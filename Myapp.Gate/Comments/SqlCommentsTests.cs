namespace Myapp.Gate.Comments;

public sealed class SqlCommentsTests
{
    private static IEnumerable<FoundComment> CommentsIn(string path, string text) => CommentLexer.Scan(text, "--", "'\"");

    [Fact]
    public void No_SQL_file_holds_a_comment() =>
        CommentFiles.AssertNone(CommentFiles.Matching(path => path.EndsWith(".sql", StringComparison.Ordinal)), CommentsIn);

    [Theory]
    [InlineData("create table note (id bigint);", 0)]
    [InlineData("-- the notes\ncreate table note (id bigint);", 1)]
    [InlineData("create table note (\n  id bigint -- the key\n);", 2)]
    [InlineData("create table note (id bigint); /* later */", 1)]
    [InlineData("insert into note values ('-- no', 'it''s /* no */');", 0)]
    [InlineData("create table \"x--y\" (id bigint);", 0)]
    public void The_rules_find_a_comment_in_SQL(string text, int line) =>
        Assert.Equal(
            line,
            CommentsIn("Myapp.Server/Database/Migrations/up/0001_x.sql", text).Select(comment => comment.Line).FirstOrDefault()
        );
}
