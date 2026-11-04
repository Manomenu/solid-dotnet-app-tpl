using System.Diagnostics;
using System.Text.Json;

namespace Myapp.Gate.Repository;

public sealed class HookGuardEditTests
{
    private static (int ExitCode, string Errors) Run(string path, bool allowGateEdit = false)
    {
        var gitBash = @"C:\Program Files\Git\bin\bash.exe";
        var start = new ProcessStartInfo(File.Exists(gitBash) ? gitBash : "bash")
        {
            RedirectStandardInput = true,
            RedirectStandardError = true,
            WorkingDirectory = RepositoryFiles.Root,
        };
        start.ArgumentList.Add("Myapp.Gate/scripts/hook-guard-edit.sh");
        start.Environment.Remove("ALLOW_GATE_EDIT");
        if (allowGateEdit)
        {
            start.Environment["ALLOW_GATE_EDIT"] = "1";
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException("bash did not start");
        process.StandardInput.Write(
            JsonSerializer.Serialize(
                new
                {
                    tool_name = "Edit",
                    tool_input = new { file_path = Path.IsPathRooted(path) ? path : $"{RepositoryFiles.Root}/{path}" },
                }
            )
        );
        process.StandardInput.Close();
        var errors = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, errors);
    }

    [Theory]
    [InlineData("Myapp.Gate/scripts/check.sh")]
    [InlineData("Myapp.Gate/scripts/criteria.sh")]
    [InlineData("Myapp.Gate/scripts/hook-guard-edit.sh")]
    [InlineData("Myapp.Gate/criteria.json")]
    [InlineData("Myapp.Gate/Design/ContractsTests.cs")]
    [InlineData("Myapp.Gate/Myapp.Gate.csproj")]
    [InlineData(".claude/settings.json")]
    [InlineData(".github/workflows/ci.yml")]
    [InlineData(".editorconfig")]
    [InlineData("Myapp.Web/eslint.config.js")]
    [InlineData("Myapp.Web/tsconfig.app.json")]
    [InlineData("BannedSymbols.txt")]
    [InlineData("Directory.Build.props")]
    public void An_agent_cannot_edit_the_gate_and_is_told_to_ask_the_owner(string path)
    {
        var (exitCode, errors) = Run(path);

        Assert.Equal(2, exitCode);
        Assert.Contains("ALLOW_GATE_EDIT=1 claude", errors, StringComparison.Ordinal);
        Assert.Contains("ask the user", errors, StringComparison.Ordinal);
    }

    [Fact]
    public void A_recording_git_tracks_is_refused_and_a_new_one_is_not()
    {
        var repository = Path.Combine(Path.GetTempPath(), "myapp-hook-" + Guid.NewGuid().ToString("N"));
        var folder = Path.Combine(repository, "Myapp.Server.Tests", "Characterization");
        Directory.CreateDirectory(folder);
        try
        {
            var tracked = Path.Combine(folder, "Tracked.cs");
            File.WriteAllText(tracked, "");
            Git(repository, "init", "-q");
            Git(repository, "add", "-A");

            var (exitCode, errors) = Run(tracked);

            Assert.Equal(2, exitCode);
            Assert.Contains("ALLOW_GATE_EDIT=1 claude", errors, StringComparison.Ordinal);
            Assert.Equal(0, Run(Path.Combine(folder, "New.cs")).ExitCode);
        }
        finally
        {
            Directory.Delete(repository, recursive: true);
        }
    }

    private static void Git(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = directory };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException("git did not start");
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void The_owner_lets_a_session_edit_the_gate() =>
        Assert.Equal(0, Run("Myapp.Gate/scripts/check.sh", allowGateEdit: true).ExitCode);

    [Theory]
    [InlineData("Myapp.Web/src/api/openapi.d.ts")]
    [InlineData("Myapp.Server/openapi.json")]
    public void Generated_api_files_are_refused_with_the_command_that_regenerates_them(string path)
    {
        var (exitCode, errors) = Run(path);

        Assert.Equal(2, exitCode);
        Assert.Contains("api-types.sh", errors, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Myapp.Server/Notes/NoteStore.cs")]
    [InlineData("Myapp.Server.Tests/Characterization/NotesRecorded.cs")]
    [InlineData("Myapp.Web/src/notes/notes.test.ts")]
    [InlineData("Myapp.Gate/layers.json")]
    [InlineData("AGENTS.md")]
    public void Everything_else_stays_editable(string path) => Assert.Equal(0, Run(path).ExitCode);
}
