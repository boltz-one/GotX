using System.Diagnostics;
using System.Runtime.InteropServices;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

public sealed partial class Window
{
    Hitbox _drag;
    bool _dragging, _hoverCallbackFired;

    /// <summary>Nhấn nút trái trên phần tử có OnDrag (trên cùng tại điểm đó): bắt đầu kéo, giữ hình học lúc nhấn cho cả lượt kéo.</summary>
    void StartDrag(Point p)
    {
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = span.Length - 1; i >= 0; i--)
        {
            if (span[i].Blocker) return;
            if (span[i].DragInvoke is null || !span[i].Contains(p)) continue;
            _drag = span[i];
            _dragging = true;
            RaiseDrag(DragPhase.Start, p);
            return;
        }
    }

    void RaiseDrag(DragPhase phase, Point p)
    {
        if (_drag is not { DragInvoke: { } invoke, Handler: { } handler, State: { } state }) return;
        var local = _drag.Xf.Inverse(p);
        var box = _drag.Local;
        invoke(handler, state, new DragEvent(phase, p, new Point(local.X - box.X, local.Y - box.Y), new Size(box.Width, box.Height)));
    }

    int _auxPressed = -1, _auxView;
    object? _auxState;
    MouseButton _auxButton;

    /// <summary>
    /// Nút phải / nút giữa: gọi handler OnAuxClick khi nhấn và thả trên cùng một phần tử bằng cùng một nút.
    /// Không đổi focus, không bật active:, không kích hoạt OnClick.
    /// </summary>
    void DispatchAuxButton(in InputEvent e)
    {
        _mouse = e.Position;
        UpdateHover();
        int hit = HitTest(e.Position, clickOrInput: true, aux: true);
        if (e.Kind == InputKind.MouseDown)
        {
            _auxPressed = hit;
            _auxButton = e.Button;
            _auxState = hit >= 0 ? _hitboxes[hit].State : null;
            _auxView = hit >= 0 ? _hitboxes[hit].ViewId : 0;
            return;
        }
        int pressed = _auxPressed;
        var state = _auxState;
        _auxPressed = -1;
        _auxState = null;
        if (hit >= 0 && hit == pressed && e.Button == _auxButton
            && _hitboxes[hit] is { Handler: { } h, Invoke: { } inv, State: { } st } hb
            && hb.ViewId == _auxView && ReferenceEquals(st, state))
            inv(h, st, new MouseEvent(e.Position, e.Button));
    }
}
