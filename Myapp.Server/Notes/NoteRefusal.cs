using System.Text.Json.Serialization;

namespace Myapp.Server.Notes;

[JsonConverter(typeof(JsonStringEnumConverter<NoteRefusal>))]
internal enum NoteRefusal
{
    [JsonStringEnumMemberName("note.textInvalid")]
    TextInvalid,
}
