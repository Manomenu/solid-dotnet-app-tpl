using System.Net;
using System.Net.Http.Json;
using Myapp.Server.Notes;

namespace Myapp.Server.Tests.Notes;

public sealed class NoteEndpointsTests(TestDatabase database, ClassServer server) : IClassFixture<ClassServer>
{
    private TestApp App => server.App;

    [Fact]
    public async Task A_created_note_is_listed()
    {
        _ = database.Pool;
        var cancellation = TestContext.Current.CancellationToken;
        var text = $"note {Guid.NewGuid():N}";
        using var client = App.CreateClient();

        var created = await client.PostAsJsonAsync("/api/notes", new { text }, cancellation);
        var listed = await client.GetFromJsonAsync<Note[]>("/api/notes", cancellation);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(listed);
        Assert.Contains(listed, note => note.Text == text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_note_is_refused_with_what_to_send_instead(string text)
    {
        _ = database.Pool;
        using var client = App.CreateClient();

        var response = await client.PostAsJsonAsync("/api/notes", new { text }, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("non-empty", body, StringComparison.Ordinal);
    }
}
