using System.Globalization;
using GotX.Text;

namespace GotX.Widgets;

/// <summary>
/// Ô nhập liệu nhiều dòng, tự xuống dòng. Hỗ trợ:
/// – Bộ gõ kiểu Unikey/EVKey (gửi Backspace rồi ký tự mới) – xử lý tự nhiên qua KeyDown + TextInput.
/// – IME có chuỗi đang soạn (Telex của Windows, bộ gõ CJK): hiển thị gạch chân, chốt khi commit.
/// – Chọn bằng Shift+mũi tên / kéo chuột, Ctrl+A, di chuyển theo grapheme và theo dòng hiển thị.
/// Là một View nên chỉ ô này render lại khi gõ; các view khác được phát lại từ cache.
/// </summary>
public sealed partial class TextInput : View
{
    string _text = "";
    string? _displayCache;

    public readonly record struct TextChanged(string Text);
    public readonly record struct Submitted(string Text);

    public string Text
    {
        get => _text;
        set
        {
            _text = Vietnamese.Compose(value ?? "");
            Caret = Anchor = _text.Length;
            Composition = "";
            OnChanged();
        }
    }

    public string Placeholder { get; set; } = "";
    public bool Multiline { get; set; } = true;
    public int Caret { get; private set; }
    public int Anchor { get; private set; }
    public string Composition { get; private set; } = "";
    public Rgba SelectionColor { get; set; } = Rgba.FromHex(0xc7d2fe);
    public Rgba PlaceholderColor { get; set; } = Rgba.FromHex(0x94a3b8);
    /// <summary>Class Tailwind của khung ô nhập; focus: được đánh giá khi ô đang focus.</summary>
    public string BoxClass { get; set; } =
        "w-full px-3 py-2 rounded-lg border bg-white border-slate-300 text-slate-900 cursor-text " +
        "focus:border-indigo-500 focus:ring-3 focus:ring-indigo-200 " +
        "dark:bg-slate-800 dark:border-slate-600 dark:text-slate-100 dark:focus:ring-indigo-900";

    /// <summary>Chiều cao tối thiểu vùng chữ (logical px).</summary>
    public float MinHeight { get; set; } = 72;

    public bool IsFocused => FocusedIn is not null;
    public bool HasSelection => Caret != Anchor;
    int SelStart => Math.Min(Caret, Anchor);
    int SelEnd => Math.Max(Caret, Anchor);

    // ---- Ảnh chụp layout ở lần vẽ gần nhất (Window ghi vào) ----
    internal Window? FocusedIn;
    internal readonly List<TextLine> Lines = new();
    internal Bounds Content;
    internal TextStyle Style;
    internal TextSystem? TextSys;
    internal Bounds CaretRect;
    internal Xf Xf = Xf.Identity; // biến đổi lúc vẽ (cuộn, translate, scale) – để đổi toạ độ chuột

    /// <summary>Khi view chứa ô được phát lại ở vị trí mới (cuộn), dời hình học đã lưu theo.</summary>
    internal void Translate(float dx, float dy)
    {
        Xf = Xf.Translate(dx, dy);
        CaretRect = CaretRect.Offset(dx, dy);
    }

    /// <summary>Chuỗi hiển thị: văn bản + chuỗi IME đang soạn chèn tại con trỏ.</summary>
    internal string DisplayText => _displayCache ??= Mask(Composition.Length == 0 ? _text : _text.Insert(Caret, Composition));

    /// <summary>Ô mật khẩu: hiển thị dấu chấm thay chữ (mỗi đơn vị UTF-16 một dấu, nên vị trí con trỏ giữ nguyên) và không cho chép ra.</summary>
    public bool Password
    {
        get => _password;
        set { if (_password == value) return; _password = value; _displayCache = null; Notify(); }
    }
    bool _password;

    string Mask(string s) => _password && s.Length > 0 ? new string('•', s.Length) : s;
    internal int DisplayCaret => Caret + Composition.Length;

    public override Div Render(Window w)
    {
        var box = w.Div().Class(BoxClass);
        return box.FocusTarget(this).Child(w.InputArea(this).MinH(Multiline ? MinHeight : 0));
    }

    // ---------- Chỉnh sửa ----------

    public void Insert(string s)
    {
        if (s.Length == 0) return;
        ReplaceSelection(s);
    }

    public void SetComposition(string s)
    {
        if (s.Length > 0 && HasSelection) ReplaceSelection("");
        Composition = s;
        _displayCache = null;
        Notify();
    }

    public void SelectAll() { Anchor = 0; Caret = _text.Length; Moved(); }

    /// <summary>Chép vùng chọn vào clipboard của app (Ctrl+C). Không có vùng chọn thì không làm gì.</summary>
    public void Copy()
    {
        if (HasSelection && !_password) App?.Clipboard.SetText(_text[SelStart..SelEnd]);
    }

    /// <summary>Chép rồi xoá vùng chọn (Ctrl+X).</summary>
    public void Cut()
    {
        if (!HasSelection || App is null || _password) return;
        Copy();
        ReplaceSelection("");
    }

    /// <summary>Chèn văn bản từ clipboard tại con trỏ, thay vùng chọn nếu có (Ctrl+V).</summary>
    public void Paste()
    {
        if (App?.Clipboard.GetText() is { Length: > 0 } text) Insert(text);
    }

    void ReplaceSelection(string s)
    {
        s = s.Replace("\r", "");
        if (!Multiline) s = s.Replace('\n', ' ');
        int a = SelStart, b = SelEnd;
        string next = string.Concat(_text.AsSpan(0, a), s, _text.AsSpan(b));
        int caret = a + s.Length;
        // Bộ gõ gửi dấu tổ hợp rời (vd. U+0323) → ghép với ký tự đứng trước.
        string composed = Vietnamese.Compose(next);
        caret -= next.Length - composed.Length;
        _text = composed;
        Caret = Anchor = Math.Clamp(caret, 0, _text.Length);
        Composition = "";
        OnChanged();
    }

    void OnChanged()
    {
        _displayCache = null;
        Notify();
        Emit(new TextChanged(_text));
    }

    void Moved() { _displayCache = null; Notify(); }

    void MoveTo(int pos, bool extend)
    {
        Caret = Math.Clamp(pos, 0, _text.Length);
        if (!extend) Anchor = Caret;
        Moved();
    }

    int PrevBoundary(int pos)
    {
        if (pos <= 0) return 0;
        if (_text[pos - 1] == '\n') return pos >= 2 && _text[pos - 2] == '\r' ? pos - 2 : pos - 1;
        // Grapheme không vắt qua '\n' nên chỉ cần dò từ đầu dòng cứng hiện tại, không phải từ đầu văn bản.
        int p = _text.LastIndexOf('\n', pos - 1) + 1, last = p;
        while (p < pos) { last = p; p += Math.Max(1, StringInfo.GetNextTextElementLength(_text, p)); }
        return last;
    }

    int NextBoundary(int pos) =>
        pos >= _text.Length ? pos : pos + Math.Max(1, StringInfo.GetNextTextElementLength(_text, pos));
}
