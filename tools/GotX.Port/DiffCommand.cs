using System.Globalization;
using GotX.Raster;
using GotX.Test;

namespace GotX.Port;

/// <summary>Numbers behind <c>diff</c>; kept public so tests can check them on generated images.</summary>
public sealed record DiffReport(int RefW, int RefH, int GotxW, int GotxH, int Common, int Diff0, int Diff24, int MaxDelta, List<DiffRegion> Regions)
{
    public bool SameSize => RefW == GotxW && RefH == GotxH;
    public double Pct0 => Common == 0 ? 0 : 100.0 * Diff0 / Common;
    public double Pct24 => Common == 0 ? 0 : 100.0 * Diff24 / Common;
}

/// <summary><c>diff &lt;ref.png&gt; &lt;gotx.png&gt; [out.png]</c>: pixel diff (tolerance 0 and 24), dimmed-reference diff image, top regions.</summary>
public static class DiffCommand
{
    const uint Magenta = 0xFFFF00C8, Amber = 0xFFFFB000, Blue = 0xFF3B82F6;

    public static int Run(string[] argv, TextWriter o)
    {
        if (argv.Length < 2) { o.WriteLine("usage: diff <ref.png> <gotx.png> [out.png]"); return 0; }
        var r = PngDecoder.Decode(File.ReadAllBytes(argv[0]), out int rw, out int rh);
        var g = PngDecoder.Decode(File.ReadAllBytes(argv[1]), out int gw, out int gh);
        var report = Compare(r, rw, rh, g, gw, gh, out var image);
        o.WriteLine($"ref {rw}x{rh}  gotx {gw}x{gh}  " + (report.SameSize ? "same size"
            : $"SIZE MISMATCH (gotx is {gw - rw:+0;-0;0} wide, {gh - rh:+0;-0;0} tall)"));
        if (!report.SameSize)
            o.WriteLine($"compared: ONLY the overlapping top-left {Math.Min(rw, gw)}x{Math.Min(rh, gh)} corner; the extra area ({Math.Max(rw, gw) * Math.Max(rh, gh) - report.Common} px, blue in the diff image) is NOT in the percentages below");
        o.WriteLine($"differing{(report.SameSize ? "" : " (overlap only)")}: {report.Pct0.ToString("0.00", CultureInfo.InvariantCulture)}% @tol0 ({report.Diff0} px), {report.Pct24.ToString("0.00", CultureInfo.InvariantCulture)}% @tol24 ({report.Diff24} px), max channel delta {report.MaxDelta}");
        o.WriteLine("top regions (tol24, x,y WxH, px):");
        if (report.Regions.Count == 0) o.WriteLine("  none");
        foreach (var (reg, i) in report.Regions.Take(5).Select((x, i) => (x, i)))
            o.WriteLine($"  {i + 1}. [{reg.Area.X:0},{reg.Area.Y:0} {reg.Area.Width:0}x{reg.Area.Height:0}] {reg.Pixels} px");
        if (argv.Length >= 3)
        {
            PngWriter.Write(argv[2], image);
            o.WriteLine($"diff image: {argv[2]} (dimmed reference; magenta channel delta > 24, amber 1-24, blue only in one image)");
        }
        return 0;
    }

    /// <summary>Compares two RGBA bitmaps on their common top-left area and builds the diff image (size of the larger of each axis).</summary>
    public static DiffReport Compare(byte[] r, int rw, int rh, byte[] g, int gw, int gh, out Canvas image)
    {
        int w = Math.Min(rw, gw), h = Math.Min(rh, gh);
        var rc = Crop(r, rw, w, h);
        var gc = Crop(g, gw, w, h);
        var gotx = ToCanvas(gc, w, h);
        var d24 = ImageDiff.Compare(gotx, rc, 24);
        int d0 = 0, max = 0;
        image = new Canvas();
        image.Resize(Math.Max(rw, gw), Math.Max(rh, gh));
        image.Pixels.AsSpan(0, image.Width * image.Height).Fill(Blue);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (y * w + x) * 4;
                int d = Math.Max(Math.Max(Math.Abs(rc[i] - gc[i]), Math.Abs(rc[i + 1] - gc[i + 1])), Math.Max(Math.Abs(rc[i + 2] - gc[i + 2]), Math.Abs(rc[i + 3] - gc[i + 3])));
                uint px;
                if (d > 24) px = Magenta;
                else if (d > 0) px = Amber;
                else { int gray = (rc[i] * 54 + rc[i + 1] * 183 + rc[i + 2] * 19) >> 8; byte v = (byte)(170 + gray / 3); px = Pixels.Pack(v, v, v); }
                if (d > 0) d0++;
                max = Math.Max(max, d);
                image.Pixels[y * image.Width + x] = px;
            }
        return new DiffReport(rw, rh, gw, gh, w * h, d0, d24.DiffPixels, max, d24.Regions);
    }

    static byte[] Crop(byte[] src, int srcW, int w, int h)
    {
        if (srcW == w && src.Length == w * h * 4) return src;
        var dst = new byte[w * h * 4];
        for (int y = 0; y < h; y++) Buffer.BlockCopy(src, y * srcW * 4, dst, y * w * 4, w * 4);
        return dst;
    }

    static Canvas ToCanvas(byte[] rgba, int w, int h)
    {
        var c = new Canvas();
        c.Resize(w, h);
        for (int i = 0; i < w * h; i++)
        {
            int a = rgba[i * 4 + 3];
            uint r = (uint)(rgba[i * 4] * a / 255), gr = (uint)(rgba[i * 4 + 1] * a / 255), b = (uint)(rgba[i * 4 + 2] * a / 255);
            c.Pixels[i] = ((uint)a << 24) | (r << 16) | (gr << 8) | b;
        }
        return c;
    }
}
