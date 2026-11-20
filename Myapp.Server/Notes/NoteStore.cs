using Dapper;
using Npgsql;

namespace Myapp.Server.Notes;

internal sealed class NoteStore(NpgsqlDataSource dataSource)
{
    public async Task<Note> CreateAsync(string text, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleAsync<NoteRow>(
            new CommandDefinition(
                "INSERT INTO notes (text) VALUES (@text) RETURNING id, text, created_at AS CreatedAt",
                new { text },
                cancellationToken: cancellationToken
            )
        );
        return row.ToNote();
    }

    public async Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<NoteRow>(
            new CommandDefinition(
                "SELECT id, text, created_at AS CreatedAt FROM notes ORDER BY created_at DESC, id",
                cancellationToken: cancellationToken
            )
        );
        return [.. rows.Select(row => row.ToNote())];
    }
}
