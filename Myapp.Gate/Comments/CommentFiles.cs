namespace Myapp.Gate.Comments;

internal static class CommentFiles
{
    public const string Fix =
        "delete the comment; rename or split the code so it explains itself; put lasting knowledge in a test, the spec or AGENTS.md "
        + "(the only comments allowed are the tool directives listed in AGENTS.md, \"No comments\")";

    public const string CriterionId = @"AC-[a-z0-9]+(?:-[a-z0-9]+)*-[0-9]+";

    private static readonly string[] Generated =
    [
        "packages.lock.json",
        "pnpm-lock.yaml",
        "Myapp.Web/src/api/openapi.d.ts",
        "contract/openapi.json",
    ];

    private static readonly string[] SkippedFolders = ["node_modules/", "bin/", "obj/", ".artifacts/", "docs/"];

    public static bool IsExcluded(string path) =>
        Generated.Any(generated => path == generated || path.EndsWith("/" + generated, StringComparison.Ordinal))
        || SkippedFolders.Any(folder =>
            path.StartsWith(folder, StringComparison.Ordinal) || path.Contains("/" + folder, StringComparison.Ordinal)
        );

    public static IEnumerable<string> Matching(Func<string, bool> belongs) =>
        RepositoryFiles
            .Paths()
            .Where(path => !IsExcluded(path))
            .Where(belongs)
            .Where(path => File.Exists(Path.Combine(RepositoryFiles.Root, path)));

    public static void AssertNone(IEnumerable<string> paths, Func<string, string, IEnumerable<FoundComment>> commentsIn)
    {
        var found = paths
            .SelectMany(path => commentsIn(path, RepositoryFiles.Read(path)).Select(comment => $"{path}:{comment.Line}: {comment.Shown}"))
            .ToList();

        Assert.True(found.Count == 0, $"{Fix}; {found.Count} found:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }
}
