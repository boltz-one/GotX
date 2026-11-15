using GotX.Raster;
using GotX.Test;

namespace ShadcnShots;

/// <summary>Pixel metrics of one capture. Diff fractions are over the union of both image sizes (missing area counts as different).</summary>
public sealed record PixelStats(int RefWidth, int RefHeight, int GotXWidth, int GotXHeight, double Diff0, double Diff24, int MaxDelta)
{
    public bool SizeMismatch => RefWidth != GotXWidth || RefHeight != GotXHeight;
}

/// <summary>Per-pixel comparison, the highlighted diff image and the side-by-side compare sheet.</summary>
public static class PixelCompare
{
    /// <summary>Per-channel tolerance treated as "perceptually the same" (anti-aliasing, sub-pixel glyph placement).</summary>
    public const int Perceptual = 24;
    const uint Strong = 0xFFFF00C8, Weak = 0xFFFFB000, Missing = 0xFF2F6BFF, Gap = 0xFF808080;

    /// <summary>
    /// Compares the reference (RGBA bytes from <see cref="GotX.PngDecoder"/>) with GotX's canvas. <paramref name="diff"/>: dimmed
    /// reference, magenta = channel delta &gt; 24, amber = 1..24, blue = area only one image has.
    /// </summary>
    public static PixelStats Compare(byte[] reference, int rw, int rh, Canvas gotx, out Canvas diff)
    {
        var mine = Pixels.ToRgba(gotx);
        int gw = gotx.Width, gh = gotx.Height, w = Math.Max(rw, gw), h = Math.Max(rh, gh);
        diff = new Canvas();
        diff.Resize(w, h);
        long d0 = 0, d24 = 0;
        int maxDelta = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool inR = x < rw && y < rh, inG = x < gw && y < gh;
                if (!inR || !inG) { d0++; d24++; diff.Pixels[y * w + x] = Missing; continue; }
                int i = (y * rw + x) * 4, j = (y * gw + x) * 4;
                int d = Math.Max(Math.Max(Math.Abs(reference[i] - mine[j]), Math.Abs(reference[i + 1] - mine[j + 1])),
                    Math.Abs(reference[i + 2] - mine[j + 2]));
                if (d > 0) d0++;
                if (d > Perceptual) d24++;
                maxDelta = Math.Max(maxDelta, d);
                uint px;
                if (d > Perceptual) px = Strong;
                else if (d > 0) px = Weak;
                else
                {
                    int gray = (reference[i] * 54 + reference[i + 1] * 183 + reference[i + 2] * 19) >> 8;
                    byte v = (byte)(170 + gray / 3);
                    px = Pixels.Pack(v, v, v);
                }
                diff.Pixels[y * w + x] = px;
            }
        double total = (double)w * h;
        return new PixelStats(rw, rh, gw, gh, d0 / total, d24 / total, maxDelta);
    }

    /// <summary>Reference left, GotX right, diff below; halved when wide so the sheet stays viewable.</summary>
    public static Canvas Sheet(byte[] reference, int rw, int rh, Canvas gotx, Canvas diff)
    {
        const int pad = 16;
        int cw = Math.Max(rw, gotx.Width), ch = Math.Max(rh, gotx.Height);
        var sheet = new Canvas();
        sheet.Resize(cw * 2 + pad * 3, ch + diff.Height + pad * 3);
        sheet.Pixels.AsSpan(0, sheet.Width * sheet.Height).Fill(Gap);
        for (int y = 0; y < rh; y++)
            for (int x = 0; x < rw; x++)
            {
                int i = (y * rw + x) * 4;
                sheet.Pixels[(y + pad) * sheet.Width + x + pad] = Pixels.Pack(reference[i], reference[i + 1], reference[i + 2]);
            }
        Blit(gotx, sheet, cw + pad * 2, pad);
        Blit(diff, sheet, (sheet.Width - diff.Width) / 2, ch + pad * 2);
        return sheet.Width > 1600 ? Half(sheet) : sheet;
    }

    static void Blit(Canvas src, Canvas dst, int ox, int oy)
    {
        for (int y = 0; y < src.Height; y++)
            src.Pixels.AsSpan(y * src.Width, src.Width).CopyTo(dst.Pixels.AsSpan((y + oy) * dst.Width + ox, src.Width));
    }

    /// <summary>2×2 box downscale (opaque canvases).</summary>
    static Canvas Half(Canvas src)
    {
        var dst = new Canvas();
        dst.Resize(src.Width / 2, src.Height / 2);
        for (int y = 0; y < dst.Height; y++)
            for (int x = 0; x < dst.Width; x++)
            {
                uint a = src.Pixels[2 * y * src.Width + 2 * x], b = src.Pixels[2 * y * src.Width + 2 * x + 1];
                uint c = src.Pixels[(2 * y + 1) * src.Width + 2 * x], d = src.Pixels[(2 * y + 1) * src.Width + 2 * x + 1];
                static uint Ch(uint p, int s) => (p >> s) & 255;
                uint r = (Ch(a, 16) + Ch(b, 16) + Ch(c, 16) + Ch(d, 16) + 2) / 4;
                uint g = (Ch(a, 8) + Ch(b, 8) + Ch(c, 8) + Ch(d, 8) + 2) / 4;
                uint bl = (Ch(a, 0) + Ch(b, 0) + Ch(c, 0) + Ch(d, 0) + 2) / 4;
                dst.Pixels[y * dst.Width + x] = 0xFF000000u | (r << 16) | (g << 8) | bl;
            }
        return dst;
    }
}
