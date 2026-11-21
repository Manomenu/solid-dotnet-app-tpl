using Microsoft.AspNetCore.Http.HttpResults;
using Suwbackkit.Server;

namespace Myapp.Server.Notes;

internal static class NoteEndpoints
{
    public const int MaxTextLength = 500;

    public static void MapNotes(this IEndpointRouteBuilder routes)
    {
        var notes = routes.MapGroup("/notes");
        notes.MapGet("/", List);
        notes.MapPost("/", Create).ProducesRefusal<NoteRefusal>(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IReadOnlyList<Note>> List(NoteStore store, CancellationToken cancellationToken) =>
        await store.ListAsync(cancellationToken);

    private static async Task<Results<Created<Note>, ProblemHttpResult>> Create(
        NewNote request,
        NoteStore store,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > MaxTextLength)
        {
            return Refusals.Refuse(
                StatusCodes.Status422UnprocessableEntity,
                "The note cannot be saved",
                NoteRefusal.TextInvalid,
                $"Send a JSON body with a non-empty \"text\" of at most {MaxTextLength} characters."
            );
        }
        var note = await store.CreateAsync(request.Text, cancellationToken);
        return TypedResults.Created($"/api/notes/{note.Id}", note);
    }
}
