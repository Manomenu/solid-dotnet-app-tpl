namespace Myapp.Gate.Comments;

public sealed class WebCommentsTests
{
    private const string Config = "Myapp.Web/eslint.config.js";

    [Theory]
    [InlineData("export const noComments = {")]
    [InlineData("context.sourceCode.getAllComments()")]
    [InlineData("files: [\"**/*.{ts,tsx,js,mjs,cjs}\"],")]
    [InlineData("plugins: { local: { rules: { \"no-comments\": noComments } } },")]
    [InlineData("rules: { \"local/no-comments\": \"error\" },")]
    public void Eslint_refuses_every_comment_in_TypeScript_and_JavaScript_but_a_tool_directive(string line) =>
        Assert.True(
            RepositoryFiles.Read(Config).Contains(line, StringComparison.Ordinal),
            $"{Config} must keep the local no-comments rule switched on as an error for every TypeScript and JavaScript file "
                + $"(its examples are Myapp.Web/eslint.config.test.js); missing: {line}"
        );

    [Fact]
    public void The_lint_step_of_the_gate_lints_the_whole_web_app() =>
        Assert.Contains(
            "pnpm exec eslint . --max-warnings 0",
            RepositoryFiles.Read("Myapp.Gate/scripts/check.sh"),
            StringComparison.Ordinal
        );
}
