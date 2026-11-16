using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

public sealed partial class Window
{
    // ================= Input =================

    /// <summary>Xử lý một sự kiện, chạy effect; trả về true nếu cần vẽ lại.</summary>
    public bool DispatchInput(in InputEvent e)
    {
        _needsFrame = false;
        if (_log is not null) NoteInput(e);
        if (Trace is not null && e.Kind != InputKind.MouseMove) TraceInput(e);
        if (e.Kind == InputKind.KeyDown) _keyboardMode = true;
        else if (e.Kind == InputKind.MouseDown) _keyboardMode = false;
        if (_overlays.Count > 0 && HandleOverlayInput(e))
        {
            App.Flush();
            return _needsFrame;
        }
        if (e.Kind is InputKind.MouseDown or InputKind.MouseUp && e.Button != MouseButton.Left)
        {
            DispatchAuxButton(e);
            App.Flush();
            return _needsFrame;
        }
        switch (e.Kind)
        {
            case InputKind.MouseMove:
                _mouse = e.Position;
                UpdateHover();
                _dragInput?.SetCaretFromPoint(e.Position, extend: true);
                if (_dragging) RaiseDrag(DragPhase.Move, e.Position);
                break;
            case InputKind.MouseLeave:
                _mouse = new(float.NegativeInfinity, float.NegativeInfinity);
                UpdateHover();
                break;
            case InputKind.MouseDown:
            {
                _mouse = e.Position;
                _mouseDown = true;
                UpdateHover();
                StartDrag(e.Position);
                _pressed = HitTest(e.Position, clickOrInput: true);
                _pressedState = _pressed >= 0 ? _hitboxes[_pressed].State : null;
                _pressedView = _pressed >= 0 ? _hitboxes[_pressed].ViewId : 0;
                var input = _pressed >= 0 ? _hitboxes[_pressed].Input : null;
                Focus(input);
                if (input is not null)
                {
                    input.SetCaretFromPoint(e.Position, (e.Modifiers & Modifiers.Shift) != 0);
                    _dragInput = input;
                }
                else FocusFromPointer(e.Position);
                break;
            }
            case InputKind.MouseUp:
            {
                _mouseDown = false;
                UpdateHover();
                _dragInput = null;
                if (_dragging) { RaiseDrag(DragPhase.End, e.Position); _dragging = false; _drag = default; }
                int hit = HitTest(e.Position, clickOrInput: true), pressed = _pressed;
                var pressedState = _pressedState;
                int pressedView = _pressedView;
                _pressed = -1;
                _pressedState = null;
                // Cây có thể đã dựng lại giữa lúc nhấn và thả: ngoài chỉ số hitbox còn phải khớp view + state,
                // để không gọi nhầm handler của phần tử khác vừa trượt vào cùng chỉ số.
                if (hit >= 0 && hit == pressed && _hitboxes[hit] is { Handler: { } h, Invoke: { } inv, State: { } st } hb
                    && hb.ViewId == pressedView && ReferenceEquals(st, pressedState))
                {
                    if (Trace is not null) Emit(TraceKind.Click, st.GetType().Name, HitboxBounds(hit), hb.ViewId);
                    inv(h, st, new MouseEvent(e.Position, e.Button));
                }
                break;
            }
            case InputKind.Scroll:
                Scroll(e.Position, e.Delta);
                break;
            case InputKind.KeyDown:
                DispatchKey(e);
                break;
            case InputKind.TextInput:
                Focused?.Insert(e.Text ?? "");
                break;
            case InputKind.ImeCompose:
                Focused?.SetComposition(e.Text ?? "");
                break;
        }
        App.Flush();
        return _needsFrame;
    }

    /// <summary>Lăn chuột: vùng cuộn trên cùng dưới con trỏ còn cuộn được theo hướng đó (nếu hết thì chuyển lên vùng ngoài).</summary>
    void Scroll(Point p, Point delta)
    {
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = span.Length - 1; i >= 0; i--)
        {
            ref var hb = ref span[i];
            if (hb.Blocker) return; // vùng cuộn nằm dưới một overlay modal
            if (hb.ScrollKey == 0 || !hb.Contains(p) || !_scrolls.TryGetValue(hb.ScrollKey, out var st)) continue;
            bool can = (delta.Y > 0 && st.TargetY < st.MaxY) || (delta.Y < 0 && st.TargetY > 0)
                    || (delta.X > 0 && st.TargetX < st.MaxX) || (delta.X < 0 && st.TargetX > 0);
            if (!can) continue;
            st.TargetX = Math.Clamp(st.TargetX + delta.X, 0, st.MaxX);
            st.TargetY = Math.Clamp(st.TargetY + delta.Y, 0, st.MaxY);
            if (Trace is not null) Emit(TraceKind.Scroll, $"tới {st.TargetX:0.#},{st.TargetY:0.#} / tối đa {st.MaxX:0.#},{st.MaxY:0.#}", HitboxBounds(i), hb.ViewId);
            MarkViewDirty(hb.ViewId, true, DirtyCause.Scroll);
            return;
        }
    }

    /// <summary>Focus một ô nhập (null = bỏ mọi focus, kể cả focus trên phần tử thường).</summary>
    public void Focus(TextInput? input)
    {
        SetElementFocus(0, false);
        FocusInputOnly(input);
    }

    void FocusInputOnly(TextInput? input)
    {
        if (Focused == input) return;
        if (Trace is not null) Emit(TraceKind.Focus, input is null ? "(none)" : "TextInput#" + input.Id.Value);
        if (Focused is { } old) { old.FocusedIn = null; old.Notify(); DirtyInputOwners(old); MarkViewDirty(old.Id.Value, true, DirtyCause.Focus, ancestors: true); }
        Focused = input;
        if (input is not null) { input.FocusedIn = this; input.Notify(); DirtyInputOwners(input); MarkViewDirty(input.Id.Value, true, DirtyCause.Focus, ancestors: true); }
    }

    /// <summary>
    /// Phần tử bọc ô nhập (<c>.FocusInput(input)</c>) nằm trong view của CHA và đổi hình theo focus (focus:ring…): focus đổi thì view cha phải vẽ lại.
    /// Các view cha cũng được coi là bẩn trực tiếp (ancestors: true) vì component cha hay đọc <c>IsFocused</c> của con trong Render (Ui.InputGroup).
    /// </summary>
    void DirtyInputOwners(TextInput input)
    {
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = 0; i < span.Length; i++)
            if (ReferenceEquals(span[i].Input, input) && !span[i].IsKey && span[i].ViewId > 0) MarkViewDirty(span[i].ViewId, true, DirtyCause.Focus);
    }

    void UpdateHover()
    {
        var span = CollectionsMarshal.AsSpan(_hovers);
        for (int i = 0; i < span.Length; i++)
        {
            bool inside = i >= _hoverFloor && span[i].Contains(_mouse);
            bool pressed = inside && _mouseDown;
            bool was = span[i].Inside;
            bool changed = inside != was || (span[i].HasActive && pressed != span[i].Pressed);
            span[i].Inside = inside;
            span[i].Pressed = pressed;
            if (changed && span[i].Visual) MarkViewDirty(span[i].ViewId, true, DirtyCause.Hover);
            if (inside != was && span[i] is { Invoke: { } invoke, Handler: { } handler, State: { } state })
            {
                _hoverCallbackFired = true;
                invoke(handler, state, inside);
            }
        }
    }

    int HitTest(Point p, bool clickOrInput, bool aux = false)
    {
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = span.Length - 1; i >= 0; i--) // phần tử vẽ sau nằm trên
        {
            if (span[i].Blocker) return -1; // mọi thứ đứng trước lớp modal không nhận chuột
            if (span[i].Anchor is not null || span[i].DragInvoke is not null || span[i].IsKey) continue;
            if (clickOrInput && span[i].ScrollKey != 0) continue;
            if (span[i].Aux != aux) continue;
            if (span[i].Contains(p)) return i;
        }
        return -1;
    }
}
