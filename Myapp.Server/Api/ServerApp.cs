using Myapp.Server.Notes;
using Suwbackkit.Postgres;
using Suwbackkit.Server;

namespace Myapp.Server.Api;

internal static class ServerApp
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServerDefaults();
        builder.Services.AddDatabase(builder.Configuration, migrateOnStart: !ServerDefaults.WritingOpenApiDocument);
        builder.Services.AddSingleton<NoteStore>();

        var app = builder.Build();
        app.MapServerDefaults();

        var api = app.MapGroup("/api");
        api.MapHealthStatus();
        api.MapNotes();
        return app;
    }
}
