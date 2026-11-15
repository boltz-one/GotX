using System.Collections.Concurrent;
using System.Globalization;

namespace GotX;

public static partial class Tailwind
{
    static bool Done(Action a) { a(); return true; }

    static bool Prefix(string u, string prefix, out string value)
    {
        if (u.Length > prefix.Length && u.StartsWith(prefix, StringComparison.Ordinal)) { value = u[prefix.Length..]; return true; }
        value = ""; return false;
    }

    static bool Enum<T>(string v, string table, Action<byte> emit) where T : struct, System.Enum
    {
        foreach (var pair in table.Split(' '))
        {
            int c = pair.IndexOf(':');
            if (pair.AsSpan(0, c).SequenceEqual(v)) { emit(Convert.ToByte(System.Enum.Parse<T>(pair[(c + 1)..]))); return true; }
        }
        return false;
    }

    static bool SpaceOp(string v, bool neg, Action<Length> emit)
    {
        if (v == "auto") return false; // margin auto chưa hỗ trợ trong layout
        if (!TrySpacing(v, neg, out var l)) return false;
        emit(l); return true;
    }

    static bool InsetOp(string v, bool neg, byte mask, Action<Prop, byte, float, Length, Rgba> emit)
    {
        Length l;
        if (v == "auto") l = Length.Auto;
        else if (!TrySize(v, neg, out l)) return false;
        emit(Prop.Inset, mask, 0, l, default);
        return true;
    }

    static bool SizeOp(string v, bool neg, Prop p, byte screen, Action<Prop, byte, float, Length, Rgba> emit)
    {
        if (v == "screen" && screen != 0) { emit(p, screen, 0, default, default); return true; }
        if (Theme.Containers.TryGetValue(v, out float rem) && p is Prop.Width or Prop.MaxW or Prop.MinW or Prop.Basis)
        { emit(p, 0, 0, Length.Rems(rem), default); return true; }
        if (!TrySize(v, neg, out var l)) return !neg && ViewportSizeOp(v, p, emit);
        emit(p, 0, 0, l, default);
        return true;
    }
}
