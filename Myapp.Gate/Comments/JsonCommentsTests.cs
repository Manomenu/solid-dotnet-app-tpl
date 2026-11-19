using System.Text.RegularExpressions;

namespace Myapp.Gate.Comments;

public sealed partial class JsonCommentsTests
{
    [GeneratedRegex($"^// {CommentFiles.CriterionId}$")]
    private static partial Regex CriterionMarker();

    private static bool IsJson(string path) =>
        path.EndsWith(".json", StringComparison.Ordinal) || path.EndsWith(".jsonc", StringComparison.Ordinal);

    private static bool ReadsCriterionMarkers(string path) =>
        path.StartsWith("Myapp.Web/tsconfig", StringComparison.Ordinal) && path.EndsWith(".json", StringComparison.Ordinal);

    private static IEnumerable<FoundComment> CommentsIn(string path, string text) =>
        CommentLexer.Scan(text, "//", "\"").Where(comment => !(ReadsCriterionMarkers(path) && CriterionMarker().IsMatch(comment.Text)));

    [Fact]
    public void No_JSON_file_holds_a_comment_but_a_criterion_marker_in_a_tsconfig() =>
        CommentFiles.AssertNone(CommentFiles.Matching(IsJson), CommentsIn);

    [Theory]
    [InlineData("Myapp.Web/knip.json", "{ \"a\": 1 }", 0)]
    [InlineData("Myapp.Web/knip.json", "{\n  // only points at the projects\n  \"a\": 1\n}", 2)]
    [InlineData("Myapp.Web/knip.json", "{ \"a\": 1 /* a block */ }", 1)]
    [InlineData("Myapp.Web/knip.json", "{ \"$schema\": \"https://example.com/x.json\", \"b\": \"/* no */\" }", 0)]
    [InlineData("Myapp.Web/knip.json", "{ \"a\": \"say \\\"// no\\\"\" }", 0)]
    [InlineData("Myapp.Web/tsconfig.app.json", "{\n  // AC-" + "notes-1\n  \"strict\": true\n}", 0)]
    [InlineData("Myapp.Web/tsconfig.app.json", "{\n  // AC-" + "notes-1 keeps it strict\n  \"strict\": true\n}", 2)]
    [InlineData(".vscode/settings.json", "{\n  // AC-" + "notes-1\n  \"a\": 1\n}", 2)]
    public void The_rules_find_a_comment_in_JSON(string path, string text, int line) =>
        Assert.Equal(line, CommentsIn(path, text).Select(comment => comment.Line).FirstOrDefault());
}
