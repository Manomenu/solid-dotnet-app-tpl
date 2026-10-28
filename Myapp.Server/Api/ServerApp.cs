using System.Reflection;
using Myapp.Server.Database;
using Myapp.Server.Health;
using Myapp.Server.Notes;

namespace Myapp.Server.Api;

internal static class ServerApp
{
    // Why: the OpenAPI build step starts the app only to read its routes, so it must not need a database
    private static bool WritingOpenApiDocument => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDatabase(builder.Configuration, migrateOnStart: !WritingOpenApiDocument);
        builder.Services.AddSingleton<NoteStore>();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();

        var app = builder.Build();
        app.UseStatusCodePages();

        var api = app.MapGroup("/api");
        api.MapHealth();
        api.MapNotes();
        return app;
    }
}
