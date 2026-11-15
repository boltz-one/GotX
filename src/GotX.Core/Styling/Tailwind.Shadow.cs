namespace GotX;

public static partial class Tailwind
{
    /// <summary>Bóng tuỳ ý: <c>0_4px_10px_rgba(0,0,0,0.2)</c> (dấu _ thay khoảng trắng).</summary>
    static bool TryShadow(string s, out BoxShadow shadow)
    {
        shadow = default;
        var parts = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i <= s.Length; i++)
        {
            if (i == s.Length || (s[i] == '_' && depth == 0)) { if (i > start) parts.Add(s[start..i]); start = i + 1; continue; }
            if (s[i] == '(') depth++; else if (s[i] == ')') depth--;
        }
        var color = Rgba.Black.WithAlpha(0.1f);
        if (parts.Count > 0 && (LooksLikeColor(parts[^1]) || Theme.Colors.ContainsKey(parts[^1])))
        {
            if (!TryCssColor(parts[^1], out color) && !Theme.Colors.TryGetValue(parts[^1], out color)) return false;
            parts.RemoveAt(parts.Count - 1);
        }
        if (parts.Count is < 2 or > 4) return false;
        float[] n = new float[4];
        for (int i = 0; i < parts.Count; i++)
        {
            if (!TryLength(parts[i], out var l) || l.Resolve(float.NaN, 16) is not { } px) return false;
            n[i] = px;
        }
        shadow = new BoxShadow(color, n[0], n[1], n[2], n[3]);
        return true;
    }
}
