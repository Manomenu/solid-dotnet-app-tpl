using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Myapp.Gate.Code;

public sealed class EscapeHatchTests
{
    [Fact]
    public void Nothing_is_typed_dynamic()
    {
        var found = SourceFile
            .All.SelectMany(file =>
                file.Root.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(name => name.Identifier.Text == "dynamic")
                    .Select(name => $"{file.Path}:{file.LineOf(name) + 1}")
            )
            .ToList();

        Assert.True(
            found.Count == 0,
            $"give it a real type (a record, a generic): dynamic switches the compiler off: {string.Join(", ", found)}"
        );
    }

    [Fact]
    public void Nothing_uses_the_null_forgiving_operator()
    {
        var found = SourceFile
            .All.SelectMany(file =>
                file.Root.DescendantNodes()
                    .Where(node => node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
                    .Select(node => $"{file.Path}:{file.LineOf(node) + 1}")
            )
            .ToList();

        Assert.True(
            found.Count == 0,
            $"remove the `!`: check for null and say what happens then, or fix the type so it cannot be null: {string.Join(", ", found)}"
        );
    }
}
