using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Myapp.Gate.Code;

// Why: xunit makes a test class anew for every test, so a server started in a field starts once per test, which in a sibling project made its server tests five times slower (2026-11-10).
public sealed class ClassServerTests
{
    [Fact]
    public void No_test_class_starts_the_server_in_a_field()
    {
        var found = SourceFile
            .All.Where(file => file.Path.StartsWith("Myapp.Server.Tests/", StringComparison.Ordinal))
            .SelectMany(file =>
                file.Root.DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Where(field => field.Declaration.Type.ToString() == "TestApp")
                    .Select(field => $"{file.Path}:{file.LineOf(field) + 1}")
            )
            .ToList();

        Assert.True(
            found.Count == 0,
            $"take the server as a class fixture: (TestDatabase database, ClassServer server) : IClassFixture<ClassServer>, then private TestApp App => server.App; a test that needs other settings makes its own TestApp in a using inside the test: {string.Join(", ", found)}"
        );
    }
}
