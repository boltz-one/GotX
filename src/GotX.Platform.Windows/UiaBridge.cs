using System.Collections.Concurrent;
using System.Diagnostics;
using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

/// <summary>
/// Cầu nối UI Automation của một cửa sổ. Chỉ được tạo khi công cụ hỗ trợ gửi WM_GETOBJECT đầu tiên, nên app không có AT không tốn gì.
/// UIA gọi provider từ luồng khác: provider chỉ đọc <see cref="Snapshot"/> bất biến; hành động được chuyển về luồng giao diện bằng
/// <see cref="WM_GOTX_UIA"/>. Sau mỗi frame (chỉ khi có client đang nghe) ảnh chụp được dựng lại có giới hạn tần suất,
/// rồi so với ảnh cũ để bắn sự kiện đổi focus / đổi thuộc tính.
/// </summary>
internal sealed unsafe class UiaBridge : IDisposable
{
    const int AutomationFocusChanged = 20005, LiveRegionChanged = 20024, MinRebuildMs = 120;

    readonly IAccessibilityHost _host;
    readonly ConcurrentQueue<Action> _actions = new();
    readonly Timer _later;
    long _lastBuild;
    int _builtFrame = -1;
    volatile bool _disposed;

    public UiaBridge(nint hwnd, IAccessibilityHost host)
    {
        Hwnd = hwnd;
        _host = host;
        _later = new Timer(static s => ((UiaBridge)s!).Nudge(), this, Timeout.Infinite, Timeout.Infinite);
        Snapshot = UiaSnapshot.Copy(host.Build());
        _builtFrame = host.Frame;
        _lastBuild = Stopwatch.GetTimestamp();
    }

    public nint Hwnd { get; }
    public volatile UiaSnapshot Snapshot;
    public float Scale => Snapshot.Scale;

    public UiaElement Element(long id) => new(this, id);

    /// <summary>Trả lời WM_GETOBJECT(UiaRootObjectId): trao provider gốc cho UIA.</summary>
    public nint GetObject(nint wParam, nint lParam)
    {
        var root = Element(0);
        nint p = root.Acquire();
        nint result = UiaReturnRawElementProvider(Hwnd, wParam, lParam, p);
        root.Release();
        return result;
    }

    /// <summary>Chạy trên luồng giao diện sau mỗi frame vẽ. Không có client nghe thì không làm gì.</summary>
    public void OnFrame()
    {
        if (_disposed || UiaClientsAreListening() == 0 || _host.Frame == _builtFrame) return;
        long wait = MinRebuildMs - (Stopwatch.GetTimestamp() - _lastBuild) * 1000 / Stopwatch.Frequency;
        if (wait > 0) { _later.Change((int)wait + 1, Timeout.Infinite); return; } // frame cuối của một loạt vẫn được phản ánh
        Rebuild();
    }

    void Nudge() { if (!_disposed) PostMessageW(Hwnd, WM_GOTX_UIA, 0, 0); }

    public void Perform(long id, SemanticAction action, double number = 0, string? text = null)
    {
        _actions.Enqueue(() => _host.Perform(id, action, number, text));
        PostMessageW(Hwnd, WM_GOTX_UIA, 0, 0);
    }

    /// <summary>WM_GOTX_UIA trên luồng giao diện: chạy hành động chờ sẵn rồi dựng lại ảnh nếu cần.</summary>
    public void Drain()
    {
        while (_actions.TryDequeue(out var a)) a();
        OnFrame();
    }

    void Rebuild()
    {
        var old = Snapshot;
        var next = UiaSnapshot.Copy(_host.Build());
        _builtFrame = _host.Frame;
        _lastBuild = Stopwatch.GetTimestamp();
        Snapshot = next;
        RaiseChanges(old, next);
    }

    void RaiseChanges(UiaSnapshot old, UiaSnapshot next)
    {
        if (next.FocusedId != 0 && next.FocusedId != old.FocusedId) Raise(next.FocusedId, p => UiaRaiseAutomationEvent(p, AutomationFocusChanged));
        foreach (var n in next.Nodes)
        {
            var was = old.Get(n.Id);
            if (was is null)
            {
                if (n.Role is SemanticRole.Alert or SemanticRole.Status) Raise(n.Id, p => UiaRaiseAutomationEvent(p, LiveRegionChanged));
                continue;
            }
            Changed(n, was, UiaProps.Name, was.Name != n.Name);
            Changed(n, was, UiaProps.IsEnabled, was.Enabled != n.Enabled);
            Changed(n, was, UiaProps.ToggleState, UiaProps.HasToggle(n) && UiaProps.ToggleStateOf(was) != UiaProps.ToggleStateOf(n));
            Changed(n, was, UiaProps.ExpandCollapseState, UiaProps.HasExpand(n) && was.Has(SemanticState.Expanded) != n.Has(SemanticState.Expanded));
            Changed(n, was, UiaProps.IsSelected, UiaProps.HasSelection(n) && UiaProps.IsSelectedNow(was) != UiaProps.IsSelectedNow(n));
            Changed(n, was, UiaProps.ValueValue, UiaProps.HasValue(n) && was.Value != n.Value);
            Changed(n, was, UiaProps.RangeValueValue, n.HasRange && was.Now != n.Now);
            if (n.Role == SemanticRole.Alert && was.Name != n.Name) Raise(n.Id, p => UiaRaiseAutomationEvent(p, LiveRegionChanged));
        }
    }

    void Changed(UiaNode now, UiaNode before, int property, bool changed)
    {
        if (!changed) return;
        var o = UiaProps.Get(before, property);
        var n = UiaProps.Get(now, property);
        Raise(now.Id, p => UiaRaiseAutomationPropertyChangedEvent(p, property, o, n));
        if (o.vt == 8) SysFreeString((char*)o.data);
        if (n.vt == 8) SysFreeString((char*)n.data);
    }

    void Raise(long id, Func<nint, int> raise)
    {
        var e = Element(id);
        nint p = e.Acquire();
        try { raise(p); }
        finally { e.Release(); }
    }

    public void Dispose()
    {
        _disposed = true;
        _later.Dispose();
        UiaReturnRawElementProvider(Hwnd, 0, 0, 0); // ngắt provider gốc: UIA thả mọi tham chiếu
    }
}
