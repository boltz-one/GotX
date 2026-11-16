using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

public sealed partial class Window
{
    const float FauxSlant = 0.2f;

    /// <summary>Mặc định của <see cref="SubpixelText"/>: bật bằng GOTX_SUBPIXEL_TEXT=1.</summary>
    public static bool SubpixelTextDefault { get; set; } = Environment.GetEnvironmentVariable("GOTX_SUBPIXEL_TEXT") == "1";

    /// <summary>
    /// Định vị glyph theo 1/4 pixel ngang (4 biến thể mỗi glyph, kiểu GPUI; dọc vẫn làm tròn): khoảng cách giữa các chữ nhỏ đều hơn, đổi lại atlas
    /// tốn tới 4 lần bộ nhớ / công raster. Mặc định TẮT vì mọi pixel chữ đổi (ảnh chuẩn phải dựng lại). Đặt trước frame đầu; cả renderer CPU lẫn D3D11 dùng chung.
    /// </summary>
    public bool SubpixelText { get; set; } = SubpixelTextDefault;

    void PaintText(string text, Bounds b, in TextStyle ts, Bounds deviceClip, Xf xf, Bounds clip)
    {
        text = Text.ApplyCase(text, ts.Case);
        Text.WrapStyled(text, ts, ts.NoWrap ? float.NaN : b.Width + 0.01f, _lines);
        float align = ts.Align switch { TextAlign.Center => 0.5f, TextAlign.Right => 1f, _ => 0f };
        var color = Fade(ts.Color, ts.Opacity);
        int shown = ts.LineClamp > 0 ? Math.Min(_lines.Count, ts.LineClamp) : _lines.Count;
        VisibleLines(b, ts, clip, xf, shown, out int lo, out int hi);
        for (int i = lo; i < hi; i++)
        {
            var l = _lines[i];
            float y = b.Y + i * ts.LineHeight;
            if (i == shown - 1 && shown < _lines.Count) { PaintClampedLine(text, l, b, y, align, ts, color, deviceClip, xf); continue; }
            if (ts.Ellipsis && l.Width > b.Width + 0.5f)
            {
                // truncate: cắt sao cho phần còn lại + "…" vừa độ rộng
                float dots = Text.Measure("…", ts);
                int cut = Text.FitIndex(text, l.Start, l.End, MathF.Max(0, b.Width - dots), ts);
                float w = Text.MeasureRange(text, l.Start, cut, ts);
                PaintRun(text, l.Start, cut, b.X, y, ts, color, deviceClip, xf);
                PaintRun("…", 0, 1, b.X + w, y, ts, color, deviceClip, xf);
                PaintDecoration(b.X, y, w + dots, ts, color, deviceClip, xf);
                continue;
            }
            float x = b.X + (b.Width - l.Width) * align;
            PaintRun(text, l.Start, l.End, x, y, ts, color, deviceClip, xf);
            PaintDecoration(x, y, l.Width, ts, color, deviceClip, xf);
        }
    }

    /// <summary>
    /// Dòng cuối của line-clamp khi còn chữ phía sau: cắt bớt (bỏ khoảng trắng cuối) để "…" vừa bề rộng hộp,
    /// đặt "…" ngay sau chữ – như -webkit-line-clamp của trình duyệt.
    /// </summary>
    void PaintClampedLine(string text, TextLine l, Bounds b, float y, float align, in TextStyle ts, Rgba color, Bounds deviceClip, Xf xf)
    {
        float dots = Text.Measure("…", ts);
        int end = l.End;
        while (end > l.Start && text[end - 1] == ' ') end--;
        if (Text.MeasureRange(text, l.Start, end, ts) + dots > b.Width + 0.5f)
        {
            end = Text.FitIndex(text, l.Start, end, MathF.Max(0, b.Width - dots), ts);
            while (end > l.Start && text[end - 1] == ' ') end--;
        }
        float w = Text.MeasureRange(text, l.Start, end, ts);
        float x = b.X + MathF.Max(0, b.Width - (w + dots)) * align;
        PaintRun(text, l.Start, end, x, y, ts, color, deviceClip, xf);
        PaintRun("…", 0, 1, x + w, y, ts, color, deviceClip, xf);
        PaintDecoration(x, y, w + dots, ts, color, deviceClip, xf);
    }

    /// <summary>
    /// underline / line-through: một nét ngang theo đường gốc (hoặc giữa chiều cao chữ thường) của dòng.
    /// decoration-{màu} / decoration-2 đổi màu và độ dày; underline-offset-4 đặt mép trên nét cách đường gốc 4px (CSS đo từ đường gốc).
    /// </summary>
    void PaintDecoration(float x, float y, float width, in TextStyle ts, Rgba color, Bounds deviceClip, Xf xf)
    {
        if (ts.Decoration == TextDecoration.None || width <= 0) return;
        var font = Text.FontFor(ts);
        float k = ts.FontSize / font.UnitsPerEm;
        float ascent = font.Ascender * k, descent = -font.Descender * k;
        float baseline = y + (ts.LineHeight - (ascent + descent)) / 2 + ascent;
        float thick = ts.DecorationThickness > 0 ? ts.DecorationThickness : MathF.Max(1, MathF.Round(ts.FontSize / 14));
        float top = ts.Decoration != TextDecoration.Underline ? baseline - ascent * 0.3f
            : ts.UnderlineOffset is { } off ? baseline + off : baseline + thick;
        if (ts.DecorationColor is { } dc) color = Fade(dc, ts.Opacity);
        AddRect(new Bounds(x, MathF.Round(top), width, thick), color, deviceClip, xf);
    }

    /// <summary>
    /// Vẽ một đoạn chữ trên một dòng, gốc trái-trên (x, y) toạ độ layout; xf phóng cả cỡ chữ.
    /// Glyph đến từ TextSystem.Shape (thứ tự hiển thị: bidi, ligature, font dự phòng, dấu kết hợp); mỗi glyph có font riêng
    /// nên mặt chữ atlas (font, cỡ) được đổi khi font đổi. Đường gốc / chiều cao dòng theo font chính của kiểu chữ.
    /// </summary>
    void PaintRun(string text, int start, int end, float x, float y, in TextStyle ts, Rgba color, Bounds deviceClip, Xf xf)
    {
        var font = Text.FontFor(ts);
        float px = ts.FontSize * xf.S * _scale;
        float spacing = ts.LetterSpacing * xf.S * _scale;
        // Không có font nghiêng thật cho họ này → nghiêng giả bằng phép trượt ngang quanh đường gốc.
        bool fauxItalic = ts.Italic && !font.Italic;
        float k0 = px / font.UnitsPerEm, ascent = font.Ascender * k0, descent = -font.Descender * k0;
        if (!xf.IsSimple) { PaintRunRotated(text, start, end, x, y, ts, color, deviceClip, xf, font, px, ascent, descent, spacing); return; }
        var o = xf.Apply(new Point(x, y));
        float baseline = MathF.Round(o.Y * _scale + (ts.LineHeight * xf.S * _scale - (ascent + descent)) / 2 + ascent);
        float dx = o.X * _scale;
        var atlas = Text.Atlas;
        int count = Text.Shape(text, start, end, ts, true);
        var glyphs = Text.GlyphSpan;
        Font? cur = null;
        GlyphFace face = null!;
        float k = 0;
        bool subpixel = SubpixelText;

        for (int i = 0; i < count; i++)
        {
            ref readonly var s = ref glyphs[i];
            if (s.Font != cur) { cur = s.Font; face = atlas.Face(cur, px); k = px / cur.UnitsPerEm; }
            dx += s.Pre * k;
            float gx = MathF.Round(dx + s.XOff * k);
            int phase = 0;
            if (subpixel)
            {
                // Làm tròn tới 1/4 px gần nhất: phần nguyên → vị trí, phần lẻ → biến thể glyph (4 → carry sang pixel kế).
                float px0 = dx + s.XOff * k, fl = MathF.Floor(px0);
                phase = (int)((px0 - fl) * 4 + 0.5f);
                if (phase == 4) { phase = 0; fl++; }
                gx = fl;
            }
            int gi = (phase == 0 ? face : face.Phase(phase)).Get(s.Id);
            var mask = atlas[gi];
            if (mask.Width > 0)
            {
                var gb = new Bounds(gx + mask.Left, baseline - MathF.Round(s.YOff * k) + mask.Top, mask.Width, mask.Height);
                Mat2 slant = default;
                if (fauxItalic)
                {
                    // Trượt quanh tâm glyph rồi bù lại để điểm trên đường gốc đứng yên.
                    slant = new Mat2(1, -FauxSlant, 0, 1);
                    gb = gb.Offset(FauxSlant * (baseline - (gb.Y + gb.Height / 2)), 0);
                }
                Add(new Primitive { Kind = PrimitiveKind.Glyph, Bounds = gb, Transform = slant, Glyph = gi, Color = color, Clip = deviceClip });
            }
            dx += s.Advance * k + (s.IsMark ? 0 : spacing);
        }
    }

    /// <summary>Chữ bị xoay: tính vị trí từng glyph trong toạ độ layout rồi đặt theo phép biến đổi (không làm tròn pixel).</summary>
    void PaintRunRotated(string text, int start, int end, float x, float y, in TextStyle ts, Rgba color, Bounds deviceClip,
        Xf xf, Text.Font font, float px, float ascent, float descent, float spacing)
    {
        float unit = xf.S * _scale; // số pixel thiết bị trên 1 đơn vị layout
        float baseline = y + ((ts.LineHeight * unit - (ascent + descent)) / 2 + ascent) / unit;
        float pen = x;
        var atlas = Text.Atlas;
        int count = Text.Shape(text, start, end, ts, true);
        var glyphs = Text.GlyphSpan;
        Font? cur = null;
        GlyphFace face = null!;
        float k = 0;
        for (int i = 0; i < count; i++)
        {
            ref readonly var s = ref glyphs[i];
            if (s.Font != cur) { cur = s.Font; face = atlas.Face(cur, px); k = px / cur.UnitsPerEm; }
            pen += s.Pre * k / unit;
            int gi = face.Get(s.Id);
            var mask = atlas[gi];
            if (mask.Width > 0)
            {
                var local = new Bounds(pen + (s.XOff * k + mask.Left) / unit, baseline + (mask.Top - s.YOff * k) / unit, mask.Width / unit, mask.Height / unit);
                var bounds = Place(local, xf, out Mat2 rot);
                Add(new Primitive { Kind = PrimitiveKind.Glyph, Bounds = bounds, Transform = rot, Glyph = gi, Color = color, Clip = deviceClip });
            }
            pen += (s.Advance * k + (s.IsMark ? 0 : spacing)) / unit;
        }
    }
}
