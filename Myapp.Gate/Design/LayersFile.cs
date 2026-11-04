using System.Text.Json;

namespace Myapp.Gate.Design;

internal static class LayersFile
{
    public const string Path = "Myapp.Gate/layers.json";

    private static readonly Lazy<JsonDocument> Document = new(() => JsonDocument.Parse(RepositoryFiles.Read(Path)));

    public static string[] ServerLayers => Strings(Document.Value.RootElement.GetProperty("serverLayers"));

    public static Dictionary<string, (string[] References, string[] InternalsVisibleTo)> Projects() =>
        Document
            .Value.RootElement.GetProperty("projects")
            .EnumerateObject()
            .ToDictionary(
                project => project.Name,
                project => (Strings(project.Value.GetProperty("references")), Strings(project.Value.GetProperty("internalsVisibleTo")))
            );

    private static string[] Strings(JsonElement array) => [.. array.EnumerateArray().Select(item => item.GetString() ?? "")];
}
