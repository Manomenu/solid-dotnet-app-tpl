using System.Text.RegularExpressions;

namespace Myapp.Gate.Repository;

// Why: a module of the whole repository sits beside the justfile and a tool's in the tool's folder; anywhere else nobody finds it.
public sealed partial class JustModulesTests
{
    [GeneratedRegex(@"^mod\s+([\w-]+)\s*$", RegexOptions.Multiline)]
    private static partial Regex ModLine();

    private static string[] JustFiles() =>
        [
            .. RepositoryFiles
                .Paths()
                .Where(path => path.EndsWith(".just", StringComparison.Ordinal) && File.Exists(Path.Combine(RepositoryFiles.Root, path))),
        ];

    [Fact]
    public void Every_module_is_beside_the_justfile_or_in_its_tools_folder_as_mod_just()
    {
        var misplaced = JustFiles()
            .Where(path =>
                path.Contains('/', StringComparison.Ordinal) && (Path.GetFileName(path) != "mod.just" || path.Count(c => c == '/') != 1)
            )
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            misplaced.Count == 0,
            $"move it to <name>.just beside the justfile (a module of the whole repository) or to <folder>/mod.just (a tool's): {string.Join(", ", misplaced)}"
        );
    }

    [Fact]
    public void Every_mod_line_names_a_module_that_exists_and_every_module_is_named()
    {
        var named = ModLine().Matches(RepositoryFiles.Read("justfile")).Select(match => match.Groups[1].Value).ToList();
        var files = JustFiles();

        var missing = named.Where(name => !files.Contains($"{name}.just") && !files.Contains($"{name}/mod.just")).ToList();
        var orphans = files
            .Where(path =>
                !named.Contains(
                    path.EndsWith("/mod.just", StringComparison.Ordinal) ? path.Split('/')[0] : Path.GetFileNameWithoutExtension(path)
                )
            )
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"`mod <name>` finds <name>.just or <name>/mod.just only, with no path: {string.Join(", ", missing)}"
        );
        Assert.True(orphans.Count == 0, $"add `mod <name>` to the justfile, or delete the module: {string.Join(", ", orphans)}");
    }
}
