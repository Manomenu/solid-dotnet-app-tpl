using System.Text.RegularExpressions;

namespace Myapp.Gate.Comments;

public sealed partial class HashCommentsTests
{
    private const string AllCommentsAreSettings = ".env.example";

    private static readonly string[] WholeLineOnly =
    [
        ".gitignore",
        ".gitattributes",
        ".dockerignore",
        ".prettierignore",
        ".editorconfig",
        "Dockerfile",
    ];

    private static readonly string[] AlsoAtTheEndOfALine = [".sh", ".yaml", ".yml", ".rsp"];

    [GeneratedRegex(@"^# shellcheck (disable|enable|source|shell|external-sources)=\S+$")]
    private static partial Regex ShellcheckDirective();

    [GeneratedRegex(@"^# syntax=\S+$")]
    private static partial Regex DockerfileSyntax();

    [GeneratedRegex($"^# {CommentFiles.CriterionId}$")]
    private static partial Regex CriterionMarker();

    [GeneratedRegex(@"^(@?[A-Za-z_][\w-]*(\s+[^:=]*)?:(?!=)|mod\s+[\w-]+\s*$)")]
    private static partial Regex RecipeOrModule();

    private static string Name(string path) => Path.GetFileName(path);

    private static bool IsJust(string path) => Name(path) == "justfile" || path.EndsWith(".just", StringComparison.Ordinal);

    private static bool IsShell(string path) =>
        path.EndsWith(".sh", StringComparison.Ordinal) || path.StartsWith(".githooks/", StringComparison.Ordinal);

    private static bool UsesHashComments(string path) =>
        IsJust(path)
        || IsShell(path)
        || WholeLineOnly.Contains(Name(path))
        || AlsoAtTheEndOfALine.Any(extension => path.EndsWith(extension, StringComparison.Ordinal))
        || Name(path) is "nginx.conf.template" or AllCommentsAreSettings;

    private static bool AlsoAtTheEnd(string path) =>
        IsShell(path)
        || Name(path) == "nginx.conf.template"
        || AlsoAtTheEndOfALine.Any(extension => path.EndsWith(extension, StringComparison.Ordinal));

    private static List<FoundComment> CommentsIn(string path, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var found = new List<FoundComment>();
        if (Name(path) == AllCommentsAreSettings)
        {
            return found;
        }
        var lines = text.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var comment = CommentOn(path, lines[index]);
            if (comment is not null && !IsAllowed(path, lines, index, comment))
            {
                found.Add(new FoundComment(index + 1, comment));
            }
        }
        return found;
    }

    private static string? CommentOn(string path, string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith('#') || (Name(path) == ".editorconfig" && trimmed.StartsWith(';')))
        {
            return trimmed.TrimEnd();
        }
        return AlsoAtTheEnd(path) ? CommentAtTheEnd(line) : null;
    }

    private static string? CommentAtTheEnd(string line)
    {
        var quote = '\0';
        for (var at = 0; at < line.Length; at++)
        {
            var sign = line[at];
            if (quote != '\0')
            {
                if (sign == '\\' && quote == '"')
                {
                    at++;
                }
                else if (sign == quote)
                {
                    quote = '\0';
                }
            }
            else if (sign is '"' or '\'')
            {
                quote = sign;
            }
            else if (sign == '#' && at > 0 && char.IsWhiteSpace(line[at - 1]))
            {
                return line[at..].TrimEnd();
            }
        }
        return null;
    }

    private static bool IsAllowed(string path, string[] lines, int index, string comment) =>
        (index == 0 && comment.StartsWith("#!", StringComparison.Ordinal))
        || (IsShell(path) && ShellcheckDirective().IsMatch(comment))
        || (Name(path) == "Dockerfile" && index == 0 && DockerfileSyntax().IsMatch(comment))
        || (Name(path) == ".editorconfig" && CriterionMarker().IsMatch(comment))
        || (IsJust(path) && DescribesTheRecipeBelow(lines, index));

    private static bool DescribesTheRecipeBelow(string[] lines, int index)
    {
        var next = index + 1;
        while (next < lines.Length && lines[next].StartsWith('[') && !lines[next].StartsWith("[private]", StringComparison.Ordinal))
        {
            next++;
        }
        return next < lines.Length && RecipeOrModule().IsMatch(lines[next]);
    }

    [Fact]
    public void No_shell_yaml_docker_ignore_or_just_file_holds_a_comment_but_a_tool_directive() =>
        CommentFiles.AssertNone(CommentFiles.Matching(UsesHashComments), CommentsIn);

    [Theory]
    [InlineData("scripts/.internal/db.sh", "#!/usr/bin/env bash\nset -euo pipefail", 0)]
    [InlineData("scripts/.internal/db.sh", "#!/usr/bin/env bash\n# The local PostgreSQL\nset -euo pipefail", 2)]
    [InlineData("scripts/.internal/db.sh", "set -euo pipefail\n# shellcheck disable=SC2086\necho $a", 0)]
    [InlineData("scripts/.internal/db.sh", "# shellcheck source=scripts/.internal/checkout-env.sh\nsource x", 0)]
    [InlineData("scripts/.internal/db.sh", "# shellcheck disable=SC2086 because\necho $a", 1)]
    [InlineData("scripts/.internal/db.sh", "echo up # start it", 1)]
    [InlineData("scripts/.internal/db.sh", "echo \"# not a comment\" '# nor this' ${#list[@]} $#", 0)]
    [InlineData(".githooks/pre-commit", "#!/usr/bin/env bash\n# Before a commit\nset -e", 2)]
    [InlineData("compose.yaml", "services:\n  db:\n    image: postgres:17 # pinned", 3)]
    [InlineData("compose.yaml", "name: myapp\n# the stack\nservices: {}", 2)]
    [InlineData(".github/workflows/ci.yml", "run: echo \"a # b\"\nname: 'c # d'", 0)]
    [InlineData("Myapp.Server/Dockerfile", "# syntax=docker/dockerfile:1\nFROM x:1", 0)]
    [InlineData("Myapp.Server/Dockerfile", "# Build context is the repo root\nFROM x:1", 1)]
    [InlineData("Myapp.Server/Dockerfile", "FROM x:1\nRUN echo a # b", 0)]
    [InlineData(".gitignore", "**/bin/\n# generated\n**/obj/", 2)]
    [InlineData(".gitattributes", "*.png binary\n# Line endings\n* text=auto", 2)]
    [InlineData(".editorconfig", "[*.cs]\n# AC-" + "notes-1\ndotnet_diagnostic.CA1000.severity = error", 0)]
    [InlineData(".editorconfig", "[*.cs]\n# Reason: formatting is CSharpier's job\ndotnet_diagnostic.IDE0055.severity = none", 2)]
    [InlineData(".editorconfig", "[*.cs]\n; old style\nroot = true", 2)]
    [InlineData("Directory.Build.rsp", "# no idle workers\n-nodeReuse:false", 1)]
    [InlineData("Myapp.Web/nginx.conf.template", "server {\n    # liveness\n    listen 8080;\n}", 2)]
    [InlineData("Myapp.Web/nginx.conf.template", "add_header cache-control \"public, max-age=1\"; # cache", 1)]
    [InlineData(".env.example", "# Copy to .env\n# WEB_PORT=8190", 0)]
    [InlineData("justfile", "# Format the code\n[group('maintenance')]\nfmt:\n    dotnet csharpier format .", 0)]
    [InlineData("justfile", "# The whole gate\ncheck *args:\n    @./check.sh", 0)]
    [InlineData("justfile", "# The local stack\nmod tmux-dev", 0)]
    [InlineData("justfile", "# Entry point for humans\n\nset shell := [\"bash\"]", 1)]
    [InlineData("justfile", "# Entry point for humans\n# Format the code\nfmt:\n    x", 1)]
    [InlineData("justfile", "# hidden\n[private]\ndefault:\n    @just --list", 1)]
    [InlineData("justfile", "fmt:\n    # format it\n    x", 2)]
    [InlineData("tmux-dev.just", "# `just tmux-dev <recipe>`\n\nset shell := [\"bash\"]", 1)]
    public void The_rules_find_a_comment_in_a_hash_comment_file(string path, string text, int line) =>
        Assert.Equal(line, CommentsIn(path, text).Select(comment => comment.Line).FirstOrDefault());
}
