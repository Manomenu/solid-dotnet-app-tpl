using Npgsql;
using Suwbackkit.Postgres;
using Suwbackkit.Testing;

[assembly: AssemblyFixture(typeof(Myapp.Server.Tests.TestDatabase))]

namespace Myapp.Server.Tests;

public sealed class TestDatabase : IAsyncLifetime
{
    public static readonly string ConnectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
        ?? "Host=127.0.0.1;Port=5533;Database=myapp_test;Username=myapp;Password=myapp";

    private string? unavailable;

    public void SkipWhenUnavailable()
    {
        if (unavailable is not null)
        {
            Assert.Skip($"PostgreSQL unavailable — start it with just db up ({unavailable})");
        }
    }

    public async ValueTask InitializeAsync()
    {
        LinkedWorktree.RefuseSharedDatabase();
        try
        {
            await TestDatabases.RecreateAsync(ConnectionString);
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException && Environment.GetEnvironmentVariable("CI") is null)
        {
            unavailable = error.Message;
            return;
        }
        await Migrator.MigrateAsync(ConnectionString, Migrator.DefaultDirectory);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
