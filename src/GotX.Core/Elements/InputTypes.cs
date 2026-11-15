using GotX.Widgets;

namespace GotX;

public enum MouseButton : byte { Left, Right, Middle }
public enum InputKind : byte { MouseMove, MouseDown, MouseUp, MouseLeave, Scroll, KeyDown, TextInput, ImeCompose }
public enum Key : byte { None, Backspace, Delete, Left, Right, Up, Down, Home, End, Enter, Tab, Escape, A, C, V, X, Space, PageUp, PageDown, Y, Z }

[Flags]
public enum Modifiers : byte { None = 0, Shift = 1, Control = 2, Alt = 4 }

public readonly record struct MouseEvent(Point Position, MouseButton Button);

/// <summary>
/// Sự kiện từ platform. Text dùng cho TextInput (chuỗi đã chốt) và ImeCompose (chuỗi đang soạn).
/// Delta dùng cho Scroll (logical px, dương = cuộn xuống/sang phải).
/// </summary>
public readonly record struct InputEvent(
    InputKind Kind, Point Position = default, MouseButton Button = MouseButton.Left,
    Key Key = Key.None, Modifiers Modifiers = Modifiers.None, string? Text = null, Point Delta = default);

internal delegate void ClickInvoker(Delegate handler, object state, in MouseEvent e);

/// <summary>Bộ gọi handler đã biết kiểu, cache tĩnh nên không cấp phát khi dùng.</summary>
internal static class ClickInvoker<T> where T : class
{
    public static readonly ClickInvoker Instance =
        static (Delegate h, object s, in MouseEvent e) => ((Action<T, MouseEvent>)h)((T)s, e);
}

internal delegate void HoverInvoker(Delegate handler, object state, bool inside);

internal static class HoverInvoker<T> where T : class
{
    public static readonly HoverInvoker Instance = static (Delegate h, object s, bool inside) => ((Action<T, bool>)h)((T)s, inside);
}

public enum DragPhase : byte { Start, Move, End }

/// <summary>
/// Một bước kéo chuột trên phần tử có OnDrag. <see cref="Local"/> là vị trí chuột tính từ góc trên-trái của phần tử
/// (theo hộp lúc bắt đầu kéo, đã bỏ cuộn/transform), có thể nằm ngoài [0, Size] khi kéo ra khỏi phần tử.
/// </summary>
public readonly record struct DragEvent(DragPhase Phase, Point Position, Point Local, Size Size)
{
    /// <summary>Vị trí theo tỉ lệ 0..1 trong phần tử (đã kẹp) – tiện cho slider, thanh chia.</summary>
    public float FractionX => Size.Width <= 0 ? 0 : Math.Clamp(Local.X / Size.Width, 0, 1);
    public float FractionY => Size.Height <= 0 ? 0 : Math.Clamp(Local.Y / Size.Height, 0, 1);
}

internal delegate void DragInvoker(Delegate handler, object state, in DragEvent e);

internal static class DragInvoker<T> where T : class
{
    public static readonly DragInvoker Instance = static (Delegate h, object s, in DragEvent e) => ((Action<T, DragEvent>)h)((T)s, e);
}

/// <summary>Phím nhấn gửi tới phần tử đang focus rồi nổi dần lên các phần tử cha có <c>OnKey</c>.</summary>
public readonly record struct KeyEvent(Key Key, Modifiers Modifiers)
{
    public bool Shift => (Modifiers & Modifiers.Shift) != 0;
    public bool Control => (Modifiers & Modifiers.Control) != 0;
    public bool Alt => (Modifiers & Modifiers.Alt) != 0;
}

internal delegate bool KeyInvoker(Delegate handler, object state, in KeyEvent e);

internal static class KeyInvoker<T> where T : class
{
    public static readonly KeyInvoker Instance = static (Delegate h, object s, in KeyEvent e) => ((Func<T, KeyEvent, bool>)h)((T)s, e);
}
