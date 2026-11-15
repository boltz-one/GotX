namespace GotX.Test;

/// <summary>
/// Mô phỏng input. Mỗi thao tác gửi sự kiện qua <c>Window.DispatchInput</c> như platform thật, rồi vẽ hai frame
/// (frame đầu áp thay đổi, frame sau cho những thứ đến trễ một frame như AutoFocus, anchor của overlay).
/// Thao tác theo khoá nhắm vào tâm phần nhìn thấy của phần tử trên màn hình (đã tính cuộn/transform).
/// </summary>
public sealed partial class UiTest
{
    /// <summary>Phần tử có khoá <paramref name="key"/>; không có thì ghi lỗi và dừng test (không thể thao tác tiếp).</summary>
    public UiNode Get(string key)
    {
        if (Tree.Find(key) is { } node) return node;
        Fail($"không tìm thấy phần tử #{key}");
        throw new TestAbortException($"không tìm thấy phần tử #{key}");
    }

    public UiNode? Find(string key) => Tree.Find(key);

    public bool Exists(string key) => Tree.Find(key) is not null;

    /// <summary>Hộp trên màn hình (logical px) của phần tử có khoá.</summary>
    public Bounds Box(string key) => Get(key).Screen;

    /// <summary>Điểm để chuột nhắm vào phần tử: tâm phần nhìn thấy. Bị cắt hết thì ghi lỗi và dừng test.</summary>
    public Point PointOf(string key)
    {
        var node = Get(key);
        if (!node.Visible.IsEmpty) return Fmt.Center(node.Visible);
        Fail($"phần tử #{key} không nhìn thấy được (bị cắt hết hoặc kích thước 0) nên không thể trỏ chuột vào", node);
        throw new TestAbortException($"phần tử #{key} không nhìn thấy được");
    }

    void Send(in InputEvent e)
    {
        Invalidate();
        Window.DispatchInput(e);
    }

    public void Click(Point p, MouseButton button = MouseButton.Left)
    {
        Send(new InputEvent(InputKind.MouseMove, p));
        Send(new InputEvent(InputKind.MouseDown, p, button));
        Send(new InputEvent(InputKind.MouseUp, p, button));
        Frames();
    }

    public void Click(string key) => Click(PointOf(key));
    public void RightClick(Point p) => Click(p, MouseButton.Right);
    public void RightClick(string key) => Click(PointOf(key), MouseButton.Right);

    /// <summary>Bấm vào node chữ đầu tiên chứa <paramref name="text"/>.</summary>
    public void ClickText(string text)
    {
        if (Tree.FindText(text) is not { } node || node.Visible.IsEmpty)
        {
            Fail($"không thấy chữ \"{text}\" trên màn hình để bấm");
            throw new TestAbortException($"không thấy chữ \"{text}\"");
        }
        Click(Fmt.Center(node.Visible));
    }

    public void Hover(Point p)
    {
        Send(new InputEvent(InputKind.MouseMove, p));
        Frames();
    }

    public void Hover(string key) => Hover(PointOf(key));

    public void MouseLeave()
    {
        Send(new InputEvent(InputKind.MouseLeave));
        Frames();
    }

    /// <summary>Nhấn ở <paramref name="from"/>, rê qua <paramref name="steps"/> điểm tới <paramref name="to"/>, rồi thả.</summary>
    public void Drag(Point from, Point to, int steps = 4)
    {
        Send(new InputEvent(InputKind.MouseMove, from));
        Send(new InputEvent(InputKind.MouseDown, from));
        for (int i = 1; i <= steps; i++)
            Send(new InputEvent(InputKind.MouseMove, new Point(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps)));
        Send(new InputEvent(InputKind.MouseUp, to));
        Frames();
    }

    public void Press(Key key, Modifiers modifiers = Modifiers.None)
    {
        Send(new InputEvent(InputKind.KeyDown, Key: key, Modifiers: modifiers));
        Frames();
    }

    /// <summary>Gõ chuỗi đã chốt vào ô nhập đang focus (như WM_CHAR).</summary>
    public void Type(string text)
    {
        Send(new InputEvent(InputKind.TextInput, Text: text));
        Frames();
    }

    /// <summary>Lăn chuột tại <paramref name="p"/> rồi chờ cuộn mượt chạy xong (tối đa ~1 giây).</summary>
    public void Scroll(Point p, float deltaY, float deltaX = 0)
    {
        Send(new InputEvent(InputKind.Scroll, p, Delta: new Point(deltaX, deltaY)));
        Frames();
        Settle(60);
    }

    public void Scroll(string key, float deltaY, float deltaX = 0) => Scroll(PointOf(key), deltaY, deltaX);
}
