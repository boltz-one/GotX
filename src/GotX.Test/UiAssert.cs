namespace GotX.Test;

public enum Edge { Left, Right, Top, Bottom, CenterX, CenterY, Baseline }

/// <summary>
/// Assertion trên giao diện. Mỗi hàm trả về true/false và khi sai thì ghi một <see cref="Failure"/> (đường dẫn phần tử,
/// hộp, ảnh khoanh vùng, nhật ký) rồi để test chạy tiếp – một lượt chạy thu được mọi sai lệch chứ không chỉ cái đầu tiên.
/// </summary>
public sealed partial class UiAssert(UiTest test)
{
    readonly UiTest _t = test;

    UiNode? Node(string key, string what)
    {
        var node = _t.Tree.Find(key);
        if (node is null) _t.Fail($"{what}: không có phần tử #{key}");
        return node;
    }

    /// <summary>Điều kiện bất kỳ. <paramref name="message"/> mô tả điều mong đợi.</summary>
    public bool True(bool condition, string message)
    {
        if (!condition) _t.Fail(message);
        return condition;
    }

    public bool Equal<T>(T actual, T expected, string what)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        if (!ok) _t.Fail($"{what}: mong đợi {expected}, thực tế {actual}");
        return ok;
    }

    public bool Near(float actual, float expected, float tolerance, string what)
    {
        bool ok = MathF.Abs(actual - expected) <= tolerance;
        if (!ok) _t.Fail($"{what}: mong đợi {Fmt.N(expected)} ±{Fmt.N(tolerance)}, thực tế {Fmt.N(actual)}");
        return ok;
    }

    public bool Exists(string key) => Node(key, "Exists") is not null;

    public bool NotExists(string key)
    {
        if (_t.Tree.Find(key) is not { } node) return true;
        _t.Fail($"NotExists: phần tử #{key} vẫn còn trong cây", node);
        return false;
    }

    /// <summary>Phần tử tồn tại và còn ít nhất một phần nằm trong vùng cắt + cửa sổ.</summary>
    public bool Visible(string key)
    {
        if (Node(key, "Visible") is not { } node) return false;
        var seen = node.Visible.Intersect(_t.Tree.ViewportBounds);
        if (!seen.IsEmpty) return true;
        _t.Fail($"Visible: #{key} không nhìn thấy được (hộp {Fmt.B(node.Screen)}, vùng cắt {Fmt.B(node.Clip)})", node);
        return false;
    }

    /// <summary>Phần tử không có trong cây, hoặc có nhưng bị cắt hết.</summary>
    public bool Hidden(string key)
    {
        if (_t.Tree.Find(key) is not { } node || node.Visible.Intersect(_t.Tree.ViewportBounds).IsEmpty) return true;
        _t.Fail($"Hidden: #{key} vẫn đang hiện", node);
        return false;
    }

    /// <summary>Chữ của phần tử (gộp mọi node chữ bên trong) bằng đúng <paramref name="expected"/>.</summary>
    public bool Text(string key, string expected)
    {
        if (Node(key, "Text") is not { } node) return false;
        string actual = _t.Tree.TextOf(node);
        if (actual == expected) return true;
        _t.Fail($"Text: #{key} mong đợi \"{expected}\", thực tế \"{actual}\"", node);
        return false;
    }

    public bool TextContains(string key, string part)
    {
        if (Node(key, "TextContains") is not { } node) return false;
        string actual = _t.Tree.TextOf(node);
        if (actual.Contains(part, StringComparison.Ordinal)) return true;
        _t.Fail($"TextContains: #{key} không chứa \"{part}\" (thực tế \"{Fmt.Cut(actual, 80)}\")", node);
        return false;
    }

    /// <summary>Có node chữ nào trên màn hình chứa <paramref name="text"/>.</summary>
    public bool TextVisible(string text)
    {
        if (_t.Tree.First(n => n.Kind == "text" && n.Text?.Contains(text, StringComparison.Ordinal) == true && !n.Visible.IsEmpty) is not null) return true;
        _t.Fail($"TextVisible: không thấy chữ \"{text}\" trên màn hình");
        return false;
    }

    /// <summary>Kích thước trên màn hình; truyền <c>float.NaN</c> để bỏ qua một chiều.</summary>
    public bool Size(string key, float width, float height, float tolerance = 0.5f)
    {
        if (Node(key, "Size") is not { } node) return false;
        var s = node.Screen;
        bool ok = (float.IsNaN(width) || MathF.Abs(s.Width - width) <= tolerance) && (float.IsNaN(height) || MathF.Abs(s.Height - height) <= tolerance);
        if (!ok) _t.Fail($"Size: #{key} mong đợi {Dim(width)}×{Dim(height)} ±{Fmt.N(tolerance)}, thực tế {Fmt.N(s.Width)}×{Fmt.N(s.Height)}", node);
        return ok;
    }

    /// <summary>Vị trí góc trái-trên trên màn hình; <c>float.NaN</c> để bỏ qua một trục.</summary>
    public bool Position(string key, float x, float y, float tolerance = 0.5f)
    {
        if (Node(key, "Position") is not { } node) return false;
        var s = node.Screen;
        bool ok = (float.IsNaN(x) || MathF.Abs(s.X - x) <= tolerance) && (float.IsNaN(y) || MathF.Abs(s.Y - y) <= tolerance);
        if (!ok) _t.Fail($"Position: #{key} mong đợi ({Dim(x)},{Dim(y)}) ±{Fmt.N(tolerance)}, thực tế ({Fmt.N(s.X)},{Fmt.N(s.Y)})", node);
        return ok;
    }

    static string Dim(float v) => float.IsNaN(v) ? "*" : Fmt.N(v);

    /// <summary>Hộp của <paramref name="child"/> nằm trọn trong hộp của <paramref name="parent"/>.</summary>
    public bool Inside(string child, string parent, float tolerance = 0.5f)
    {
        if (Node(child, "Inside") is not { } c || Node(parent, "Inside") is not { } p) return false;
        var a = c.Screen; var b = p.Screen;
        float over = MathF.Max(MathF.Max(b.X - a.X, a.Right - b.Right), MathF.Max(b.Y - a.Y, a.Bottom - b.Bottom));
        if (over <= tolerance) return true;
        _t.Fail($"Inside: #{child} {Fmt.B(a)} thò ra ngoài #{parent} {Fmt.B(b)} tới {Fmt.N(over)}px", c, p);
        return false;
    }

    public bool NoOverlap(string a, string b, float tolerance = 0.5f)
    {
        if (Node(a, "NoOverlap") is not { } na || Node(b, "NoOverlap") is not { } nb) return false;
        var i = na.Screen.Intersect(nb.Screen);
        if (i.Width <= tolerance || i.Height <= tolerance) return true;
        _t.Fail($"NoOverlap: #{a} {Fmt.B(na.Screen)} và #{b} {Fmt.B(nb.Screen)} chồng nhau {Fmt.N(i.Width)}×{Fmt.N(i.Height)}px", na, nb);
        return false;
    }

    /// <summary>Màu pixel tại điểm logical; <paramref name="tolerance"/> là chênh lệch tối đa mỗi kênh (0..255).</summary>
    public bool Pixel(Point point, Rgba expected, int tolerance = 6)
    {
        var actual = _t.PixelAt(point);
        int delta = Pixels.Delta(actual, expected);
        if (delta <= tolerance) return true;
        var f = _t.FailAt($"Pixel: tại {Fmt.P(point)} mong đợi {Fmt.C(expected)} ±{tolerance}, thực tế {Fmt.C(actual)} (lệch {delta})",
            new Bounds(point.X - 3, point.Y - 3, 6, 6), _t.Tree.TopAt(point) is { } top ? _t.Tree.Path(top) : null);
        foreach (string line in _t.Explain(point).ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)) f.Details.Add(line);
        return false;
    }

    public bool Focused(string key)
    {
        if (Node(key, "Focused") is not { } node) return false;
        if (node.Focused || _t.Window.IsFocused(key)) return true;
        var holder = _t.Tree.First(n => n.Focused);
        _t.Fail($"Focused: #{key} không giữ focus (đang focus: {(holder is null ? "không có gì" : _t.Tree.Path(holder))})", node);
        return false;
    }

    public bool NotFocused(string key)
    {
        if (_t.Tree.Find(key) is not { } node || !(node.Focused || _t.Window.IsFocused(key))) return true;
        _t.Fail($"NotFocused: #{key} đang giữ focus", node);
        return false;
    }
}
