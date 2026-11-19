namespace Myapp.Gate.Comments;

internal sealed record FoundComment(int Line, string Text)
{
    private const int ShownLength = 120;

    public string Shown
    {
        get
        {
            var firstLine = Text.Split('\n')[0].Trim();
            return firstLine.Length > ShownLength ? firstLine[..ShownLength] + "…" : firstLine;
        }
    }
}
