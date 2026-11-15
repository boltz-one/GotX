namespace GotX.Components;

/// <summary>Kiểu khung code: thường, hoặc đỏ cho log lỗi.</summary>
public enum CodeBlockVariant : byte { Default, Destructive }

/// <summary>
/// Khối code: header (tên tệp / ngôn ngữ + nút sao chép ghi vào <see cref="App.Clipboard"/>) và thân có số dòng,
/// chữ mono, cuộn ngang khi dòng dài. Vùng code là một điểm dừng Tab như <c>tabIndex=0</c> của bản gốc: khi focus
/// bằng phím cả khối hiện vòng focus, ←/→ cuộn ngang, Home/End về đầu/cuối. Chưa có tô màu theo token nên mỗi dòng là
/// chữ trơn; riêng dòng chú thích nguyên dòng (//, #, --) được làm mờ để code vẫn dễ đọc.
/// </summary>
public sealed class CodeBlock : View
{
    /// <summary>Đã ghi code vào clipboard.</summary>
    public readonly record struct Copied(string Code);

    /// <summary>Số giây nút sao chép giữ dấu tích trước khi trở lại icon sao chép (như bản gốc).</summary>
    public const double CopiedSeconds = 2;
    const float ScrollStep = 40; // px mỗi lần nhấn mũi tên, như bước cuộn bằng phím của trình duyệt

    string _code = "";
    string[] _lines = [""];
    bool _copied;
    IDisposable? _copiedReset;
    Window? _window;
    string? _bodyKey;

    /// <summary>Mã nguồn; dấu xuống dòng cuối bị bỏ như bản gốc để không sinh dòng trống thừa.</summary>
    public string Code
    {
        get => _code;
        set
        {
            _code = value;
            _lines = value.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            _copied = false;
            Notify();
        }
    }

    /// <summary>Tiêu đề header (thường là tên tệp); null thì dùng <see cref="Language"/>.</summary>
    public string? Title { get; set; }

    /// <summary>Tên ngôn ngữ, hiện mờ bên phải tiêu đề.</summary>
    public string? Language { get; set; }

    /// <summary>Hiện cột số dòng.</summary>
    public bool LineNumbers { get; set; } = true;

    /// <summary>Số của dòng đầu tiên (đoạn trích giữa tệp).</summary>
    public int StartLine { get; set; } = 1;

    /// <summary>Các dòng (đánh số từ 1 trong đoạn) được tô nền để nhấn mạnh.</summary>
    public IReadOnlySet<int>? Highlighted { get; set; }

    /// <summary>Kiểu khung (Destructive cho log lỗi).</summary>
    public CodeBlockVariant Variant { get; set; }

    /// <summary>Cỡ nhỏ: chữ xs, đệm hẹp hơn.</summary>
    public bool Small { get; set; }

    /// <summary>Vừa sao chép xong: nút hiện dấu tích trong <see cref="CopiedSeconds"/> giây rồi tự trở lại.</summary>
    public bool IsCopied => _copied;

    /// <summary>Các dòng sau khi tách (đã bỏ dòng trống cuối).</summary>
    public IReadOnlyList<string> Lines => _lines;

    /// <summary>Ghi toàn bộ code vào clipboard của app và phát <see cref="Copied"/>.</summary>
    public void Copy()
    {
        App?.Clipboard.SetText(_code);
        _copied = true;
        // Sao chép lại trong lúc đang hiện dấu tích: huỷ hẹn giờ cũ để 2 giây tính lại từ đầu.
        _copiedReset?.Dispose();
        _copiedReset =App?.SetTimeout(CopiedSeconds, () => { _copied = false; _copiedReset = null; Notify(); });
        Notify();
        Emit(new Copied(_code));
    }

    /// <summary>Phím khi vùng code đang focus; code không tràn ngang thì không nhận để phím nổi tiếp lên cha.</summary>
    bool OnBodyKey(KeyEvent e)
    {
        if (_window is not { } w || _bodyKey is null || w.GetScroll(_bodyKey) is not { } scroll || scroll.Max.X <= 0) return false;
        float x = e.Key switch
        {
            Key.Left => scroll.Offset.X - ScrollStep,
            Key.Right => scroll.Offset.X + ScrollStep,
            Key.Home => 0,
            Key.End => scroll.Max.X,
            _ => float.NaN,
        };
        if (float.IsNaN(x)) return false;
        w.ScrollTo(_bodyKey, Math.Clamp(x, 0, scroll.Max.X), null);
        return true;
    }

    static bool IsComment(string line)
    {
        var t = line.AsSpan().TrimStart();
        return t.StartsWith("//") || t.StartsWith("#") || t.StartsWith("--") || t.StartsWith("/*") || t.StartsWith("*");
    }

    /// <summary>Vẽ header + thân code; thân là vùng cuộn ngang có khoá riêng theo Id.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        _bodyKey ??= "gotx-code-body-" + Id.Value;
        bool bad = Variant == CodeBlockVariant.Destructive;
        var root = w.Div().Class(bad
            ? "relative flex w-full min-w-0 flex-col overflow-hidden rounded-lg border border-destructive/30 text-destructive dark:border-destructive/40"
            : "relative flex w-full min-w-0 flex-col overflow-hidden rounded-lg border border-input text-foreground dark:bg-input/30");
        // has-[>[data-slot=code-block-content]:focus-visible]: vòng focus của vùng code vẽ quanh cả khối.
        if (w.FocusVisible && w.IsFocused(_bodyKey)) root.Class(bad ? "ring-3 ring-ring/50" : "border-ring ring-3 ring-ring/50");
        root.Class(Small ? "text-xs" : "text-sm");

        var header = w.Div().Class(Small
            ? "flex min-w-0 shrink-0 items-center gap-2 px-2.5 pt-1.5 text-muted-foreground"
            : "flex min-w-0 shrink-0 items-center gap-2 pl-3 pr-1.5 pt-2 text-muted-foreground");
        if ((Title ?? Language) is { } title)
            header.Child(w.Div().Class(bad ? "min-w-0 truncate font-medium text-destructive" : "min-w-0 truncate font-medium text-foreground").Child(title));
        if (Title is not null && Language is { } lang) header.Child(w.Div().Class("shrink-0 text-xs").Child(lang));
        header.Child(w.Div().Class("flex-1"));
        header.Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconXs).Key("gotx-code-copy-" + Id.Value)
            .OnClick(this, static (c, _) => c.Copy())
            .Child((_copied ? Icon.Check(w) : Icon.Copy(w)).Class("size-3")));
        root.Child(header);

        // Cột số dòng rộng theo số chữ số của dòng cuối để code thẳng hàng.
        int last = StartLine + _lines.Length - 1;
        string numberClass = last >= 1000 ? "w-10 shrink-0 text-right text-muted-foreground"
            : last >= 100 ? "w-8 shrink-0 text-right text-muted-foreground" : "w-6 shrink-0 text-right text-muted-foreground";
        var body = w.Div().Class(Small
            ? "flex min-w-full shrink-0 flex-col px-2.5 pt-1.5 pb-2.5 font-mono leading-relaxed"
            : "flex min-w-full shrink-0 flex-col px-3 pt-2 pb-3 font-mono leading-relaxed");
        for (int i = 0; i < _lines.Length; i++)
        {
            string text = _lines[i];
            bool marked = Highlighted?.Contains(i + 1) == true;
            var row = w.Div().Class(!marked ? "flex gap-3" : Small ? "flex gap-3 -mx-2.5 px-2.5 bg-muted" : "flex gap-3 -mx-3 px-3 bg-muted");
            if (LineNumbers) row.Child(w.Div().Class(numberClass).Child((StartLine + i).ToString()));
            // Dòng trống vẫn giữ chiều cao một dòng nhờ khoảng trắng.
            row.Child(w.Div().Class(IsComment(text) ? "whitespace-nowrap text-muted-foreground" : "whitespace-nowrap")
                .Child(text.Length == 0 ? " " : text.Replace("\t", "    ")));
            body.Child(row);
        }
        root.Child(w.Div().Class("flex min-w-0 overflow-x-auto").Key(_bodyKey).Focusable()
            .OnKey(this, static (c, e) => c.OnBodyKey(e))
            .Child(body));
        return root;
    }
}
