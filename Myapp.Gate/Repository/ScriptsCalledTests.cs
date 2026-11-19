namespace Myapp.Gate.Repository;

public sealed class ScriptsCalledTests
{
    private static bool IsScript(string path) =>
        path.EndsWith(".sh", StringComparison.Ordinal)
        && (path.StartsWith("scripts/", StringComparison.Ordinal) || path.StartsWith("Myapp.Gate/scripts/", StringComparison.Ordinal));

    private static bool Calls(string path) =>
        path.EndsWith(".sh", StringComparison.Ordinal)
        || path.EndsWith(".just", StringComparison.Ordinal)
        || path.EndsWith(".proj", StringComparison.Ordinal)
        || path is "justfile" or ".claude/settings.json" or ".vscode/tasks.json" or ".template.config/template.json"
        || path.StartsWith(".githooks/", StringComparison.Ordinal)
        || path.StartsWith(".github/workflows/", StringComparison.Ordinal);

    [Fact]
    public void Every_script_is_called_by_a_recipe_another_script_or_a_hook()
    {
        var paths = RepositoryFiles.Paths().Where(path => File.Exists(Path.Combine(RepositoryFiles.Root, path))).ToArray();
        var callers = paths.Where(Calls).Select(path => (path, text: RepositoryFiles.Read(path))).ToList();

        var uncalled = paths
            .Where(IsScript)
            .Where(script =>
                !callers.Any(caller => caller.path != script && caller.text.Contains(Path.GetFileName(script), StringComparison.Ordinal))
            )
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(
            uncalled.Count == 0,
            $"nothing calls it: delete it, or call it from the recipe, script or hook that needs it: {string.Join(", ", uncalled)}"
        );
    }
}
