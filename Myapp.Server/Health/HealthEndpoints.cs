using Microsoft.AspNetCore.Http.HttpResults;
using Myapp.Server.Database;
using Npgsql;

namespace Myapp.Server.Health;

internal static class HealthEndpoints
{
    public static void MapHealth(this IEndpointRouteBuilder routes) => routes.MapGet("/health", Check);

    private static async Task<Results<Ok<HealthStatus>, ProblemHttpResult>> Check(Pool pool, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await pool.OpenAsync(cancellationToken);
            return TypedResults.Ok(new HealthStatus("ok"));
        }
        catch (NpgsqlException)
        {
            return TypedResults.Problem(
                detail: "The database does not answer: start it with `just db up`, or fix ConnectionStrings:Default.",
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
    }
}
