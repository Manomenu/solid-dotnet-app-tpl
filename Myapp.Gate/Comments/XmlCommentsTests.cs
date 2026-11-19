using System.Text.RegularExpressions;

namespace Myapp.Gate.Comments;

public sealed partial class XmlCommentsTests
{
    private static readonly string[] Extensions = [".props", ".targets", ".csproj", ".proj", ".slnx", ".config", ".html", ".svg", ".xml"];

    [GeneratedRegex($"^<!-- {CommentFiles.CriterionId} -->$")]
    private static partial Regex CriterionMarker();

    private static bool IsXml(string path) => Extensions.Any(extension => path.EndsWith(extension, StringComparison.Ordinal));

    private static IEnumerable<FoundComment> CommentsIn(string path, string text) =>
        CommentLexer
            .Scan(text, null, "", "<!--", "-->")
            .Where(comment => !(path == "Directory.Build.props" && CriterionMarker().IsMatch(comment.Text)));

    [Fact]
    public void No_XML_or_HTML_file_holds_a_comment_but_a_criterion_marker_in_Directory_Build_props() =>
        CommentFiles.AssertNone(CommentFiles.Matching(IsXml), CommentsIn);

    [Theory]
    [InlineData("Myapp.Server/Myapp.Server.csproj", "<Project>\n  <PropertyGroup />\n</Project>", 0)]
    [InlineData("nuget.config", "<?xml version=\"1.0\"?>\n<!--\n  nuget.org only\n-->\n<configuration />", 2)]
    [InlineData("scripts/.internal/init-project.proj", "<Project>\n  <!-- the post action -->\n</Project>", 2)]
    [InlineData("Myapp.Web/index.html", "<html>\n<body><!-- the app mounts here --><div id=\"root\"></div></body>\n</html>", 2)]
    [InlineData("Directory.Build.props", "<Project>\n  <!-- AC-" + "notes-1 -->\n  <PropertyGroup />\n</Project>", 0)]
    [InlineData("Directory.Build.props", "<Project>\n  <!-- AC-" + "notes-1: strict -->\n</Project>", 2)]
    [InlineData("Myapp.Server/Myapp.Server.csproj", "<Project>\n  <!-- AC-" + "notes-1 -->\n</Project>", 2)]
    public void The_rules_find_a_comment_in_XML(string path, string text, int line) =>
        Assert.Equal(line, CommentsIn(path, text).Select(comment => comment.Line).FirstOrDefault());
}
