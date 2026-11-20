using Npgsql;
using Suwbackkit.Postgres;

[assembly: AssemblyFixture(typeof(Myapp.Server.Tests.TestDatabase))]

namespace Myapp.Server.Tests;

public sealed class TestDatabase : IAsyncLifetime
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
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
        try
        {
            await RecreateAsync();
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException && Environment.GetEnvironmentVariable("CI") is null)
        {
            unavailable = error.Message;
            return;
        }
        await Migrator.MigrateAsync(ConnectionString, Migrator.DefaultDirectory);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static async Task RecreateAsync()
    {
        var database = new NpgsqlConnectionStringBuilder(ConnectionString).Database;
        var admin = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = "postgres",
            Timeout = 3,
            Pooling = false,
        };
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
        await create.ExecuteNonQueryAsync();
    }
}
