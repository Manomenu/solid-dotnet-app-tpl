namespace Myapp.Gate.Comments;

public sealed class CssCommentsTests
{
    private static IEnumerable<FoundComment> CommentsIn(string path, string text) => CommentLexer.Scan(text, null, "'\"");

    [Fact]
    public void No_CSS_file_holds_a_comment() =>
        CommentFiles.AssertNone(CommentFiles.Matching(path => path.EndsWith(".css", StringComparison.Ordinal)), CommentsIn);

    [Theory]
    [InlineData(":root { --radius: 0.5rem; }", 0)]
    [InlineData("/* the palette */\n:root { --radius: 0.5rem; }", 1)]
    [InlineData(":root {\n  --radius: 0.5rem; /* rounder */\n}", 2)]
    [InlineData("a { background: url(https://example.com/x.png); content: \"/* no */\"; }", 0)]
    public void The_rules_find_a_comment_in_CSS(string text, int line) =>
        Assert.Equal(line, CommentsIn("Myapp.Web/src/index.css", text).Select(comment => comment.Line).FirstOrDefault());
}
