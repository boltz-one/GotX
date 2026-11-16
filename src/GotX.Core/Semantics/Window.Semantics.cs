namespace GotX;

/// <summary>
/// Cây ngữ nghĩa theo yêu cầu. <see cref="Semantics"/> chạy MỘT frame riêng (cache tắt, bật ghi ngữ nghĩa) – frame thường không biết
/// gì về ngữ nghĩa nên không tốn CPU / bộ nhớ khi không có công cụ hỗ trợ.
/// </summary>
public sealed partial class Window
{
    SemanticsTree? _sem, _lastSem;

    /// <summary>Ảnh chụp ngữ nghĩa dựng gần nhất (null nếu chưa dựng lần nào).</summary>
    public SemanticsTree? LastSemantics => _lastSem;

    /// <summary>
    /// Dựng cây ngữ nghĩa của frame hiện tại. Có tác dụng phụ như <see cref="Inspect"/> (một frame thật được vẽ, animation tiến theo đồng hồ).
    /// Bộ đệm của lần dựng trước được tái dùng; đang trong Draw thì trả lại ảnh cũ.
    /// </summary>
    public SemanticsTree Semantics(Size logicalSize, float scale, SemanticsTree? into = null)
    {
        var tree = into ?? _lastSem ?? new SemanticsTree();
        if (_drawing) return tree;
        bool cache = CacheEnabled;
        _sem = tree;
        _tree.SemanticsOn = true;
        CacheEnabled = false;
        try
        {
            tree.BeginBuild(_frame + 1, logicalSize, scale);
            Draw(logicalSize, scale);
            tree.Finish(_tree);
        }
        finally
        {
            _sem = null;
            _tree.SemanticsOn = false;
            CacheEnabled = cache;
        }
        return _lastSem = tree;
    }

    void SemBegin(ref Node n, long focusKey, Bounds lb, Bounds clip, Xf xf)
    {
        var sem = _sem!;
        bool semantic = n.Sem != 0 || n.Focusable || n.FocusInput is not null;
        long keyed = n.Key != 0 ? ((long)(uint)CurrentView << 32) | (uint)n.Key : 0;
        var node = sem.Push(semantic, n.Key, keyed);
        if (n.Kind == NodeKind.Text && n.Text is { } text) sem.Text(text);
        if (node is null) return;

        if (n.Sem != 0)
        {
            ref readonly var d = ref _tree.SemAt(n.Sem);
            node.Role = d.Role; node.States = d.States; node.Name = d.Name; node.Description = d.Description; node.Value = d.Value;
            node.DescribedBy = d.DescribedBy; node.LabelledBy = d.LabelledBy;
            node.HasRange = d.HasRange; node.Min = d.Min; node.Max = d.Max; node.Now = d.Now;
        }
        node.ExplicitRole = node.Role != SemanticRole.None;
        node.Focusable = n.Focusable && !n.Disabled;
        node.Clickable = n.Click is not null && !n.Disabled;
        if (n.Disabled) node.States |= SemanticState.Disabled;
        if ((focusKey != 0 && focusKey == _focusKey) || (n.FocusInput is { } fi && fi == Focused)) node.States |= SemanticState.Focused;
        if (n.FocusInput is { } input)
        {
            if (node.Role == SemanticRole.None) node.Role = SemanticRole.Textbox;
            node.Input = input;
            node.Focusable = !n.Disabled;
            if (input.Password) node.States |= SemanticState.Password;
            else { node.Value ??= input.Text; node.Caret = input.Caret; node.SelectionStart = Math.Min(input.Caret, input.Anchor); node.SelectionEnd = Math.Max(input.Caret, input.Anchor); }
            if (input.Multiline) node.States |= SemanticState.Multiline;
            node.Name ??= input.AriaLabel ?? (input.Placeholder.Length > 0 ? input.Placeholder : null);
        }
        else if (node.Role == SemanticRole.None && node.Clickable && node.Focusable) node.Role = SemanticRole.Button;
        node.Bounds = PaintedBounds(lb, xf);
        node.Visible = node.Bounds.Intersect(clip);
    }

    void SemEnd() => _sem!.Pop();

    sealed class Accessibility(Window w, IPlatformWindow pw) : IAccessibilityHost
    {
        public int Frame => w._frame;
        public SemanticsTree Build() => w.Semantics(pw.LogicalSize, pw.ScaleFactor);
        public bool Perform(long id, SemanticAction action, double number, string? text) => w.PerformSemanticAction(id, action, number, text);
    }

    internal static IAccessibilityHost HostFor(Window window, IPlatformWindow pw) => new Accessibility(window, pw);

    /// <summary>
    /// Thực hiện một việc do công cụ hỗ trợ yêu cầu trên node của ảnh chụp ngữ nghĩa gần nhất, bằng đúng đường input thật
    /// (nhấn chuột / focus / gán chữ), nên mọi handler chạy như khi người dùng thao tác. Trả về false nếu không làm được.
    /// </summary>
    public bool PerformSemanticAction(long id, SemanticAction action, double number = 0, string? text = null)
    {
        if (_lastSem?.ById(id) is not { } n || n.Disabled) return false;
        var seen = n.Visible;
        switch (action)
        {
            case SemanticAction.Focus:
                if (n.Input is { } input) Focus(input);
                else if (n.Key is { } key) FocusKey(key);
                else if (!seen.IsEmpty) FocusFromPointer(new Point(seen.X + seen.Width / 2, seen.Y + seen.Height / 2));
                else return false;
                return true;
            case SemanticAction.SetValue:
                if (n.Input is not { } box || text is null) return false;
                box.Text = text;
                return true;
            case SemanticAction.SetRange:
            {
                if (!n.HasRange || n.Max <= n.Min || seen.IsEmpty) return false;
                float f = (float)Math.Clamp((number - n.Min) / (n.Max - n.Min), 0, 1);
                return Click(new Point(n.Bounds.X + n.Bounds.Width * f, seen.Y + seen.Height / 2));
            }
            default:
                return !seen.IsEmpty && Click(new Point(seen.X + seen.Width / 2, seen.Y + seen.Height / 2));
        }
    }

    bool Click(Point p)
    {
        DispatchInput(new InputEvent(InputKind.MouseMove, p));
        DispatchInput(new InputEvent(InputKind.MouseDown, p));
        DispatchInput(new InputEvent(InputKind.MouseUp, p));
        RequestFrame();
        return true;
    }
}
