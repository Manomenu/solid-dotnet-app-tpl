using Microsoft.Extensions.Options;

namespace Myapp.Server.Database;

internal sealed class DatabaseStartup(IOptions<DatabaseOptions> options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        Migrator.MigrateAsync(options.Value.ConnectionString, Migrator.DefaultDirectory);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
