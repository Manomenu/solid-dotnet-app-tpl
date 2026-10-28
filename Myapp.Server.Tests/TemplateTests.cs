using System.Text.Json;
using System.Text.RegularExpressions;

namespace Myapp.Server.Tests;

public sealed partial class TemplateTests
{
    private static readonly string[] TextExtensions =
    [
        ".cs",
        ".json",
        ".sh",
        ".ts",
        ".tsx",
        ".yaml",
        ".yml",
        ".md",
        ".props",
        ".csproj",
        ".slnx",
        ".proj",
        ".html",
        ".svg",
        ".rsp",
    ];

    private static readonly string[] MustBeExcluded =
    [
        "**/.git/**",
        "**/bin/**",
        "**/obj/**",
        "**/node_modules/**",
        ".artifacts/**",
        "**/.env",
        "CLAUDE.local.md",
        "pack/**",
        "docs/visual/**",
        "Myapp.Server.Tests/TemplateTests.cs",
        "**/packages.lock.json",
        "**/pnpm-lock.yaml",
    ];

    [GeneratedRegex("myapp", RegexOptions.IgnoreCase)]
    private static partial Regex Name();

    private static JsonElement Template() => JsonDocument.Parse(RepositoryFiles.Read(".template.config/template.json")).RootElement;

    private static IEnumerable<(string Symbol, string Base, string[] Values)> PortSymbols() =>
        Template()
            .GetProperty("symbols")
            .EnumerateObject()
            .Where(symbol => symbol.Value.TryGetProperty("generator", out var generator) && generator.GetString() == "switch")
            .Select(symbol =>
                (
                    symbol.Name,
                    symbol.Value.GetProperty("replaces").GetString() ?? "",
                    symbol
                        .Value.GetProperty("parameters")
                        .GetProperty("cases")
                        .EnumerateArray()
                        .Select(item => item.GetProperty("value").GetString() ?? "")
                        .ToArray()
                )
            );

    private static IEnumerable<(string Path, string Text)> TextFiles() =>
        RepositoryFiles
            .Paths()
            .Where(path => TextExtensions.Contains(Path.GetExtension(path)))
            .Where(path =>
                !path.EndsWith("packages.lock.json", StringComparison.Ordinal)
                && !path.StartsWith(".template.config/", StringComparison.Ordinal)
            )
            .Select(path => (path, RepositoryFiles.Read(path)));

    private static bool Shifted(string basePort, string[] values) =>
        values.Length == 10
        && values[^1] == basePort
        && Enumerable
            .Range(0, 9)
            .All(index =>
                values[index]
                == (int.Parse(basePort, System.Globalization.CultureInfo.InvariantCulture) + ((index + 1) * 10)).ToString(
                    System.Globalization.CultureInfo.InvariantCulture
                )
            );

    [Fact]
    public void The_template_names_the_placeholder_and_leaves_out_what_must_not_be_copied()
    {
        var template = Template();
        var excluded = template
            .GetProperty("sources")[0]
            .GetProperty("modifiers")[0]
            .GetProperty("exclude")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToList();

        Assert.Equal("Myapp", template.GetProperty("sourceName").GetString());
        var missing = MustBeExcluded.Where(pattern => !excluded.Contains(pattern)).ToList();
        Assert.True(
            missing.Count == 0,
            $"add to sources[0].modifiers[0].exclude in .template.config/template.json: {string.Join(", ", missing)}"
        );
    }

    [Fact]
    public void Every_port_symbol_replaces_a_base_port_and_shifts_it_by_ten_per_step()
    {
        var wrong = PortSymbols().Where(port => !Shifted(port.Base, port.Values)).Select(port => port.Symbol).ToList();

        Assert.True(PortSymbols().Any(), "the template must have port symbols");
        Assert.True(
            wrong.Count == 0,
            $"fix the cases of these symbols in .template.config/template.json (nine shifted values, then the base): {string.Join(", ", wrong)}"
        );
    }

    [Fact]
    public void A_base_port_appears_only_as_a_whole_number_and_every_one_is_used()
    {
        var files = TextFiles().ToList();
        var problems = new List<string>();
        foreach (var (symbol, port, _) in PortSymbols())
        {
            var inside = Regex.Count(string.Join('\n', files.Select(file => file.Text)), $@"\d{port}|{port}\d");
            if (inside > 0)
            {
                problems.Add(
                    $"{port} appears inside a longer number {inside} time(s): the template would corrupt it, write the number differently"
                );
            }
            if (!files.Any(file => file.Text.Contains(port, StringComparison.Ordinal)))
            {
                problems.Add($"{symbol} replaces {port}, which no file contains: delete the symbol");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void The_placeholder_name_is_spelled_only_Myapp_or_myapp()
    {
        var other = TextFiles()
            .SelectMany(file => Name().Matches(file.Text).Select(match => (file.Path, match.Value)))
            .Where(found => found.Value is not ("Myapp" or "myapp"))
            .Select(found => $"{found.Path}: {found.Value}")
            .Distinct()
            .Order()
            .ToList();

        Assert.True(
            other.Count == 0,
            $"the template replaces only Myapp and myapp, write the name in one of those spellings: {string.Join(", ", other)}"
        );
    }
}
