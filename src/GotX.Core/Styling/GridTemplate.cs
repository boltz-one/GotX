namespace GotX;

/// <summary>Một track của grid: <c>1fr</c>, <c>200px</c>, <c>auto</c>.</summary>
/// <summary>MinAuto: track fr tường minh (<c>1fr</c> = minmax(auto, 1fr)) không hẹp hơn min-content của phần tử trong nó; grid-cols-N và minmax(0,1fr) thì không.</summary>
public readonly record struct GridTrack(TrackKind Kind, float Fr, Length Size, bool MinAuto = false);

/// <summary>Mẫu cột/hàng của grid (grid-cols-3 = repeat(3, minmax(0, 1fr))). Bất biến, được cache.</summary>
public sealed class GridTemplate
{
    static readonly GridTemplate?[] s_repeat = new GridTemplate?[65];
    public GridTrack[] Tracks { get; }
    public int Count => Tracks.Length;

    /// <summary>Không có track tường minh (grid-flow-col không có grid-cols-*: mọi cột là cột ngầm auto).</summary>
    internal static readonly GridTemplate Empty = new([]);

    public GridTemplate(GridTrack[] tracks) { Tracks = tracks; }

    public static GridTemplate Repeat(int n)
    {
        n = Math.Clamp(n, 1, 64);
        return s_repeat[n] ??= new GridTemplate(Enumerable.Repeat(new GridTrack(TrackKind.Fr, 1, default), n).ToArray());
    }

    /// <summary>Phân tích "200px_1fr_auto" (dấu _ thay khoảng trắng, như giá trị tuỳ ý của Tailwind).</summary>
    public static bool TryParse(string s, out GridTemplate template)
    {
        template = null!;
        var parts = s.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is 0 or > 64) return false;
        var tracks = new GridTrack[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            string p = parts[i];
            if (p == "auto") tracks[i] = new GridTrack(TrackKind.Auto, 0, default);
            else if (p.EndsWith("fr") && float.TryParse(p[..^2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fr))
                tracks[i] = new GridTrack(TrackKind.Fr, fr, default, MinAuto: true);
            else if (p.StartsWith("minmax(0,") && p.EndsWith("fr)") && float.TryParse(p[9..^3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float mfr))
                tracks[i] = new GridTrack(TrackKind.Fr, mfr, default);
            else if (Tailwind.TryLengthValue(p, out var l) && l.Kind is LengthKind.Px or LengthKind.Rems)
                tracks[i] = new GridTrack(TrackKind.Fixed, 0, l);
            else return false;
        }
        template = new GridTemplate(tracks);
        return true;
    }
}
