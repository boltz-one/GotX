namespace GotX.Port;

public enum TokKind { Ident, Number, Str, Punct }

/// <summary>Rust-ish token: kind, text, 1-based line and [Start,End) offsets into the source.</summary>
public readonly record struct Tok(TokKind Kind, string Text, int Line, int Start, int End);

/// <summary>
/// Just enough Rust lexing for style-chain scanning: identifiers, numbers (0x.., 1.5f32), strings (escapes, r#"raw"#), chars/lifetimes,
/// line and nested block comments (dropped), punctuation (:: -> => .. and || kept together). Tolerant: never throws.
/// </summary>
public static class GpuiTokenizer
{
    public static List<Tok> Tokenize(string s)
    {
        var toks = new List<Tok>();
        int i = 0, line = 1;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '\n') { line++; i++; continue; }
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '/' && Peek(s, i + 1) == '/') { while (i < s.Length && s[i] != '\n') i++; continue; }
            if (c == '/' && Peek(s, i + 1) == '*')
            {
                int depth = 1; i += 2;
                while (i < s.Length && depth > 0)
                {
                    if (s[i] == '\n') line++;
                    if (s[i] == '/' && Peek(s, i + 1) == '*') { depth++; i += 2; }
                    else if (s[i] == '*' && Peek(s, i + 1) == '/') { depth--; i += 2; }
                    else i++;
                }
                continue;
            }
            int start = i, startLine = line;
            if (c == '"' || (c == 'r' && (Peek(s, i + 1) == '"' || (Peek(s, i + 1) == '#' && RawStart(s, i + 1)))) || (c == 'b' && Peek(s, i + 1) == '"'))
            {
                i = ReadString(s, i, ref line);
                toks.Add(new Tok(TokKind.Str, s[start..i], startLine, start, i));
                continue;
            }
            if (c == '\'')
            {
                // char literal 'x' / '\n' vs lifetime 'a
                if (Peek(s, i + 1) == '\\') { int e = s.IndexOf('\'', i + 2); i = e < 0 ? s.Length : e + 1; toks.Add(new Tok(TokKind.Str, s[start..i], line, start, i)); continue; }
                if (Peek(s, i + 2) == '\'') { i += 3; toks.Add(new Tok(TokKind.Str, s[start..i], line, start, i)); continue; }
                i++;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                continue; // lifetime: dropped
            }
            if (char.IsLetter(c) || c == '_')
            {
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                toks.Add(new Tok(TokKind.Ident, s[start..i], line, start, i));
                continue;
            }
            if (char.IsDigit(c))
            {
                if (c == '0' && Peek(s, i + 1) is 'x' or 'X') { i += 2; while (i < s.Length && (Uri.IsHexDigit(s[i]) || s[i] == '_')) i++; }
                else
                {
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '_')) i++;
                    if (i < s.Length && s[i] == '.' && Peek(s, i + 1) != '.' && !char.IsLetter(Peek(s, i + 1)) && Peek(s, i + 1) != '_')
                    { i++; while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '_')) i++; }
                }
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++; // suffix (f32, u8)
                toks.Add(new Tok(TokKind.Number, s[start..i], line, start, i));
                continue;
            }
            string two = i + 1 < s.Length ? s.Substring(i, 2) : "";
            if (two is "::" or "->" or "=>" or ".." or "||" or "&&" or "==" or "!=" or "<=" or ">=" or "+=" or "-=") { i += 2; toks.Add(new Tok(TokKind.Punct, two, line, start, i)); continue; }
            i++;
            toks.Add(new Tok(TokKind.Punct, c.ToString(), line, start, i));
        }
        return toks;
    }

    static char Peek(string s, int i) => i < s.Length ? s[i] : '\0';

    static bool RawStart(string s, int i)
    {
        while (i < s.Length && s[i] == '#') i++;
        return Peek(s, i) == '"';
    }

    static int ReadString(string s, int i, ref int line)
    {
        if (s[i] == 'b') i++;
        if (s[i] == 'r')
        {
            i++;
            int hashes = 0;
            while (Peek(s, i) == '#') { hashes++; i++; }
            i++; // opening quote
            string end = "\"" + new string('#', hashes);
            int e = s.IndexOf(end, i, StringComparison.Ordinal);
            int stop = e < 0 ? s.Length : e + end.Length;
            for (int k = i; k < stop; k++) if (s[k] == '\n') line++;
            return stop;
        }
        i++;
        while (i < s.Length && s[i] != '"')
        {
            if (s[i] == '\\') i++;
            if (i < s.Length && s[i] == '\n') line++;
            i++;
        }
        return Math.Min(s.Length, i + 1);
    }
}
