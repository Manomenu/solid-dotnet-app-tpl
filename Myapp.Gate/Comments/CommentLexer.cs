namespace Myapp.Gate.Comments;

internal static class CommentLexer
{
    public static IReadOnlyList<FoundComment> Scan(
        string text,
        string? lineComment,
        string quotes,
        string blockStart = "/*",
        string blockEnd = "*/"
    )
    {
        var found = new List<FoundComment>();
        var line = 1;
        var at = 0;
        while (at < text.Length)
        {
            var sign = text[at];
            if (sign == '\n')
            {
                line++;
                at++;
            }
            else if (quotes.Contains(sign, StringComparison.Ordinal))
            {
                var end = EndOfString(text, at, sign);
                line += LineBreaks(text, at, end);
                at = end;
            }
            else if (lineComment is not null && text.AsSpan(at).StartsWith(lineComment, StringComparison.Ordinal))
            {
                var end = text.IndexOf('\n', at);
                end = end < 0 ? text.Length : end;
                found.Add(new FoundComment(line, text[at..end].Trim()));
                at = end;
            }
            else if (text.AsSpan(at).StartsWith(blockStart, StringComparison.Ordinal))
            {
                var close = text.IndexOf(blockEnd, at + blockStart.Length, StringComparison.Ordinal);
                var end = close < 0 ? text.Length : close + blockEnd.Length;
                found.Add(new FoundComment(line, text[at..end].Trim()));
                line += LineBreaks(text, at, end);
                at = end;
            }
            else
            {
                at++;
            }
        }
        return found;
    }

    private static int EndOfString(string text, int opening, char quote)
    {
        for (var at = opening + 1; at < text.Length; at++)
        {
            if (text[at] == '\\')
            {
                at++;
            }
            else if (text[at] == quote)
            {
                return at + 1;
            }
        }
        return text.Length;
    }

    private static int LineBreaks(string text, int start, int end) => text.AsSpan(start, end - start).Count('\n');
}
