namespace GotX;

/// <summary>
/// Chữ rất dài (hàng trăm dòng) trong khung cắt: dòng nằm hẳn ngoài vùng cắt không được định hình, không sinh glyph. Pixel không đổi vì glyph
/// của chúng đều nằm ngoài vùng cắt. Phần bị bỏ được ghi như một vùng cắt "ẩn" (<see cref="NoteClipRect"/>) nên view chứa nó chỉ được phát lại
/// (chép + tịnh tiến) khi vùng ẩn vẫn nằm ngoài vùng cắt sau khi dời; ngược lại view được dựng lại và các dòng mới lộ ra được vẽ.
/// Chỉ áp dụng cho chữ không xoay và không line-clamp; chữ ngắn (nhãn, đoạn văn thường) không đổi gì.
/// </summary>
public sealed partial class Window
{
    const int TextCullMinLines = 48;

    /// <summary>Khoảng dòng [lo, hi) cần vẽ; các dòng ngoài khoảng nằm hẳn ngoài vùng cắt (có lề cho glyph tràn khỏi hộp dòng).</summary>
    void VisibleLines(Bounds b, in TextStyle ts, Bounds clip, Xf xf, int shown, out int lo, out int hi)
    {
        lo = 0; hi = shown;
        if (!CullViews || shown < TextCullMinLines || ts.LineClamp > 0 || !xf.IsSimple || clip.Width >= 1e8f || clip.Height >= 1e8f) return;
        float s = xf.A, lh = ts.LineHeight * s, top = xf.Ty + b.Y * s, m = ts.FontSize * 2 * s + 2;
        if (lh <= 0) return;
        lo = (int)Math.Clamp(MathF.Floor((clip.Y - m - top) / lh) - 1, 0, shown);
        hi = (int)Math.Clamp(MathF.Ceiling((clip.Bottom + m - top) / lh) + 1, lo, shown);
        if (lo > 0) NoteClipRect(HiddenRect(top, lo, lh, m), clip, default);
        if (hi < shown) NoteClipRect(HiddenRect(top + hi * lh, shown - hi, lh, m), clip, default);
    }

    // Rộng vô hạn theo chiều ngang: chỉ chiều dọc quyết định dòng có lộ ra hay không (chữ không ngắt dòng có thể tràn ngang).
    static Bounds HiddenRect(float y, int lines, float lh, float margin) => new(-1e7f, y - margin, 2e7f, lines * lh + 2 * margin);
}
