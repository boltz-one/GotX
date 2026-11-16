using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class UiaElement
{
    /// <summary>Navigate: 0 cha, 1 anh kế, 2 anh trước, 3 con đầu, 4 con cuối. Kết quả là con trỏ IRawElementProviderFragment (hoặc null).</summary>
    public int Navigate(int direction, nint* result)
    {
        *result = 0;
        var snap = Snapshot;
        long target = -1;
        if (IsRoot)
        {
            if (snap.Roots.Length > 0 && direction == 3) target = snap.Nodes[snap.Roots[0]].Id;
            else if (snap.Roots.Length > 0 && direction == 4) target = snap.Nodes[snap.Roots[^1]].Id;
        }
        else
        {
            var n = Node;
            if (n is null) return UIA_E_ELEMENTNOTAVAILABLE;
            int[] siblings = n.Parent >= 0 ? snap.Nodes[n.Parent].Children : snap.Roots;
            int self = Array.IndexOf(siblings, snap.ById[Id]);
            switch (direction)
            {
                case 0:
                    *result = _bridge.Element(n.Parent >= 0 ? snap.Nodes[n.Parent].Id : 0).Acquire(SlotFragment);
                    return S_OK;
                case 1 when self >= 0 && self + 1 < siblings.Length: target = snap.Nodes[siblings[self + 1]].Id; break;
                case 2 when self > 0: target = snap.Nodes[siblings[self - 1]].Id; break;
                case 3 when n.Children.Length > 0: target = snap.Nodes[n.Children[0]].Id; break;
                case 4 when n.Children.Length > 0: target = snap.Nodes[n.Children[^1]].Id; break;
            }
        }
        if (target != -1) *result = _bridge.Element(target).Acquire(SlotFragment);
        return S_OK;
    }

    public int RuntimeId(nint* result)
    {
        *result = 0;
        if (IsRoot) return S_OK; // gốc: HWND host cung cấp
        nint sa = SafeArrayCreateVector(3 /* VT_I4 */, 0, 3);
        int* p;
        SafeArrayAccessData(sa, (void**)&p);
        p[0] = 0x2A; // UiaAppendRuntimeId
        p[1] = (int)Id;
        p[2] = (int)(Id >> 32);
        SafeArrayUnaccessData(sa);
        *result = sa;
        return S_OK;
    }

    /// <summary>UiaRect (left, top, width, height) theo pixel màn hình.</summary>
    public int BoundingRect(double* rect)
    {
        rect[0] = rect[1] = rect[2] = rect[3] = 0;
        POINT origin = default;
        ClientToScreen(_bridge.Hwnd, &origin);
        if (IsRoot)
        {
            RECT c;
            GetClientRect(_bridge.Hwnd, &c);
            rect[0] = origin.x; rect[1] = origin.y; rect[2] = c.right; rect[3] = c.bottom;
            return S_OK;
        }
        var n = Node;
        if (n is null) return UIA_E_ELEMENTNOTAVAILABLE;
        float s = _bridge.Scale;
        rect[0] = origin.x + n.Bounds.X * s; rect[1] = origin.y + n.Bounds.Y * s;
        rect[2] = n.Bounds.Width * s; rect[3] = n.Bounds.Height * s;
        return S_OK;
    }

    public int SetFocus()
    {
        if (IsRoot) return S_OK;
        if (Node is null) return UIA_E_ELEMENTNOTAVAILABLE;
        _bridge.Perform(Id, SemanticAction.Focus);
        return S_OK;
    }

    public int FragmentRoot(nint* result)
    {
        *result = _bridge.Element(0).Acquire(SlotRoot);
        return S_OK;
    }

    /// <summary>Phần tử sâu nhất chứa điểm màn hình (x, y). Nút vẽ sau nằm trên nên duyệt từ cuối.</summary>
    public int FromPoint(double x, double y, nint* result)
    {
        *result = 0;
        POINT origin = default;
        ClientToScreen(_bridge.Hwnd, &origin);
        var snap = Snapshot;
        float px = (float)((x - origin.x) / snap.Scale), py = (float)((y - origin.y) / snap.Scale);
        for (int i = snap.Nodes.Length - 1; i >= 0; i--)
            if (!snap.Nodes[i].Visible.IsEmpty && snap.Nodes[i].Visible.Contains(new Point(px, py)))
            {
                *result = _bridge.Element(snap.Nodes[i].Id).Acquire(SlotFragment);
                return S_OK;
            }
        return S_OK;
    }

    public int GetFocus(nint* result)
    {
        *result = 0;
        long id = Snapshot.FocusedId;
        if (id != 0) *result = _bridge.Element(id).Acquire(SlotFragment);
        return S_OK;
    }
}
