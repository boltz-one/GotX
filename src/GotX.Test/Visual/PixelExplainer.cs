using System.Text;
using GotX.Raster;

namespace GotX.Test;

/// <summary>Một primitive phủ lên pixel đang xét. <see cref="Coverage"/> là độ phủ ước lượng 0..1 (mép anti-alias, mask glyph).</summary>
public sealed record PixelLayer(int Index, PrimitiveKind Kind, Rgba Color, Bounds Bounds, Bounds Clip, float Coverage, string Note, string? Path);

/// <summary>Kết quả của <see cref="PixelExplainer.Explain"/>.</summary>
public sealed class PixelExplanation
{
    public Point Logical { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    /// <summary>Màu cuối cùng trên ảnh.</summary>
    public Rgba Final { get; init; }
    public Rgba ClearColor { get; init; }
    /// <summary>Theo thứ tự vẽ: phần tử cuối danh sách nằm trên cùng.</summary>
    public List<PixelLayer> Layers { get; } = new();

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"pixel {Fmt.P(Logical)} → thiết bị ({X},{Y}), màu cuối {Fmt.C(Final)}, nền cửa sổ {Fmt.C(ClearColor)}, {Layers.Count} lớp phủ (dưới → trên):\n");
        foreach (var l in Layers)
            sb.Append($"  #{l.Index,-4} {l.Kind,-6} {Fmt.C(l.Color),-13} phủ {Fmt.N(l.Coverage),-4} {Fmt.B(l.Bounds)}{l.Note}  ← {l.Path ?? "(không thuộc node nào)"}\n");
        return sb.ToString();
    }
}

/// <summary>
/// Trả lời "vì sao pixel này có màu này / cái gì đang vẽ đè lên": liệt kê mọi primitive của Scene phủ lên một pixel theo
/// thứ tự vẽ, kèm node sở hữu (qua <see cref="UiNode.PrimStart"/>/<see cref="UiNode.PrimEnd"/>).
/// Độ phủ được tính lại gần đúng: quad theo SDF hình chữ nhật bo góc, glyph theo mask trong atlas. Primitive bị xoay
/// và vùng cắt theo hình (bo góc) chỉ xét theo hộp bao – được ghi chú trong cột Note.
/// </summary>
public static class PixelExplainer
{
    public static PixelExplanation Explain(UiTree tree, Canvas canvas, Point logical)
    {
        var scene = tree.Snapshot.Scene;
        int x = (int)MathF.Floor(logical.X * tree.Scale), y = (int)MathF.Floor(logical.Y * tree.Scale);
        var result = new PixelExplanation { Logical = logical, X = x, Y = y, Final = Pixels.Get(canvas, x, y), ClearColor = scene.ClearColor };
        float cx = x + 0.5f, cy = y + 0.5f;
        var center = new Point(cx, cy);
        var prims = scene.Primitives;
        for (int i = 0; i < prims.Length; i++)
        {
            ref readonly var p = ref prims[i];
            if (!p.Clip.Contains(center)) continue;
            string note = "";
            var color = p.Color;
            float coverage;
            bool rotated = p.Transform.IsTransform;
            if (rotated) note += " (xoay: theo hộp bao)";
            switch (p.Kind)
            {
                case PrimitiveKind.Glyph:
                    coverage = rotated ? (p.Bounds.Contains(center) ? 1 : 0) : GlyphCoverage(scene, p, x, y);
                    break;
                case PrimitiveKind.Shadow:
                    coverage = p.Bounds.Inflate(p.Blur).Contains(center) ? 1 : 0;
                    note += $" blur {Fmt.N(p.Blur)}";
                    break;
                case PrimitiveKind.Quad:
                    coverage = QuadCoverage(p, cx, cy, rotated, ref color, ref note);
                    break;
                default:
                    coverage = p.Bounds.Contains(center) ? 1 : 0;
                    break;
            }
            if (coverage <= 0.004f) continue;
            if (p.ClipShape != 0) note += " (trong vùng cắt bo góc)";
            if (p.Clip.Width < 1e8f) note += $" clip {Fmt.B(p.Clip)}";
            var owner = tree.OwnerOfPrimitive(i);
            result.Layers.Add(new PixelLayer(i, p.Kind, color, p.Bounds, p.Clip, coverage, note, owner is null ? null : tree.Path(owner)));
        }
        return result;
    }

    static float GlyphCoverage(Scene scene, in Primitive p, int x, int y)
    {
        var mask = scene.Atlas[p.Glyph];
        int mx = x - (int)MathF.Round(p.Bounds.X), my = y - (int)MathF.Round(p.Bounds.Y);
        if ((uint)mx >= (uint)mask.Width || (uint)my >= (uint)mask.Height) return 0;
        return mask.Alpha[my * mask.Width + mx] / 255f;
    }

    /// <summary>Độ phủ của quad tại tâm pixel; nếu pixel rơi vào dải viền thì <paramref name="color"/> đổi thành màu viền.</summary>
    static float QuadCoverage(in Primitive p, float cx, float cy, bool rotated, ref Rgba color, ref string note)
    {
        var b = p.Bounds;
        if (rotated) return b.Contains(new Point(cx, cy)) ? 1 : 0;
        // Khoảng cách có dấu tới hình chữ nhật bo góc (âm = bên trong).
        float lx = cx - (b.X + b.Width / 2), ly = cy - (b.Y + b.Height / 2);
        float r = lx < 0 ? (ly < 0 ? p.Corners.TopLeft : p.Corners.BottomLeft) : (ly < 0 ? p.Corners.TopRight : p.Corners.BottomRight);
        r = MathF.Min(r, MathF.Min(b.Width, b.Height) / 2);
        float qx = MathF.Abs(lx) - b.Width / 2 + r, qy = MathF.Abs(ly) - b.Height / 2 + r;
        float d = MathF.Min(MathF.Max(qx, qy), 0) + MathF.Sqrt(MathF.Max(qx, 0) * MathF.Max(qx, 0) + MathF.Max(qy, 0) * MathF.Max(qy, 0)) - r;
        float coverage = Math.Clamp(0.5f - d, 0, 1);
        if (coverage <= 0 || !p.HasBorder) return color.A > 0 ? coverage : 0;

        // Cạnh gần nhất quyết định màu viền; nằm sâu hơn độ dày viền thì là phần nền.
        float dl = cx - b.X, dr = b.Right - cx, dt = cy - b.Y, db = b.Bottom - cy;
        float nearest = MathF.Min(MathF.Min(dl, dr), MathF.Min(dt, db));
        var (width, border) = nearest == dt ? (p.BorderWidths.Top, p.BorderTop) : nearest == db ? (p.BorderWidths.Bottom, p.BorderBottom)
            : nearest == dl ? (p.BorderWidths.Left, p.BorderLeft) : (p.BorderWidths.Right, p.BorderRight);
        if (width > 0 && -d <= width)
        {
            color = border;
            note += " (viền)";
            return border.A > 0 ? coverage : 0;
        }
        return color.A > 0 ? coverage : 0;
    }
}
