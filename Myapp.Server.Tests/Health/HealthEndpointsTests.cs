using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Myapp.Server.Database;
using Npgsql;

namespace Myapp.Server.Tests.Health;

public sealed class HealthEndpointsTests(TestDatabase database) : IDisposable
{
    private readonly TestApp app = new();

    public void Dispose() => app.Dispose();

    [Fact]
    public async Task Health_is_ok_when_the_database_answers()
    {
        _ = database.Pool;
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_says_what_to_do_when_the_database_does_not_answer()
    {
        _ = database.Pool;
        var unreachable = new NpgsqlConnectionStringBuilder(TestDatabase.ConnectionString) { Port = 1 }.ConnectionString;
        using var broken = app.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton(Pool.Create(unreachable)))
        );
        using var client = broken.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("just db up", body, StringComparison.Ordinal);
    }
}
