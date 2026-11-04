using System.Text.RegularExpressions;

namespace Myapp.Gate.Repository;

public sealed partial class DocsPathsTests
{
    private static readonly string[] NotInTheRepository = ["CLAUDE.local.md"];

    [GeneratedRegex(@"`([\w.\-/]+)`|\]\(([\w.\-/]+)(?:#[\w\-]*)?\)")]
    private static partial Regex Mention();

    [GeneratedRegex(@"\.(cs|csproj|json|md|props|sh|slnx|sql|ts|tsx|txt|yaml|yml)$")]
    private static partial Regex FileName();

    private static bool IsPath(string mention) =>
        !mention.StartsWith("http", StringComparison.Ordinal)
        && !mention.StartsWith('/')
        && (mention.Contains('/', StringComparison.Ordinal) || FileName().IsMatch(mention));

    private static bool Exists(string mention, string[] paths)
    {
        var folder = mention.TrimEnd('/') + "/";
        return paths.Any(known =>
            known == mention
            || known.StartsWith(folder, StringComparison.Ordinal)
            || known.EndsWith("/" + mention, StringComparison.Ordinal)
            || known.Contains("/" + folder, StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Every_file_and_folder_the_docs_mention_exists()
    {
        var paths = RepositoryFiles.Paths();
        var stale = paths
            .Where(path => path.EndsWith(".md", StringComparison.Ordinal) && !path.StartsWith("docs/visual/", StringComparison.Ordinal))
            .SelectMany(document =>
                Mention()
                    .Matches(RepositoryFiles.Read(document))
                    .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
                    .Where(IsPath)
                    .Where(mention => !NotInTheRepository.Contains(mention) && !Exists(mention, paths))
                    .Select(mention => $"{document}: {mention}")
            )
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            stale.Count == 0,
            $"the docs name paths that do not exist: say what the code does instead of where it is, or fix the path: {string.Join(", ", stale)}"
        );
    }
}
