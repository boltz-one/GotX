using System.Runtime.InteropServices;

// Phía CLIENT của UI Automation (COM, chỉ dùng trong test): khai báo đúng thứ tự vtable của phần dùng tới; ô thừa là chỗ giữ vị trí.
[ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomation
{
    void CompareElements(); void CompareRuntimeIds(); void GetRootElement();
    [PreserveSig] int ElementFromHandle(nint hwnd, out IUIAutomationElement element);
    void ElementFromPoint();
    [PreserveSig] int GetFocusedElement(out IUIAutomationElement element);
    void GetRootElementBuildCache(); void ElementFromHandleBuildCache(); void ElementFromPointBuildCache(); void GetFocusedElementBuildCache();
    void CreateTreeWalker(); void ControlViewWalker(); void ContentViewWalker(); void RawViewWalker();
    void RawViewCondition(); void ControlViewCondition(); void ContentViewCondition(); void CreateCacheRequest();
    [PreserveSig] int CreateTrueCondition(out IUIAutomationCondition condition);
    void _22(); void _23(); void _24(); void _25(); void _26(); void _27(); void _28(); void _29(); void _30(); void _31(); void _32(); void _33();
    [PreserveSig] unsafe int AddPropertyChangedEventHandlerNativeArray(IUIAutomationElement element, int scope, nint cache, IUIAutomationPropertyChangedEventHandler handler, int* properties, int count);
    void _35(); void _36(); void _37(); void _38();
    [PreserveSig] int AddFocusChangedEventHandler(nint cache, IUIAutomationFocusChangedEventHandler handler);
    void _40();
    [PreserveSig] int RemoveAllEventHandlers();
}

[ComImport, Guid("352ffba8-0973-437c-a61f-f64cafd81df9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationCondition { }

[ComImport, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationElement
{
    [PreserveSig] int SetFocus();
    void GetRuntimeId(); void FindFirst();
    [PreserveSig] int FindAll(int scope, IUIAutomationCondition condition, out IUIAutomationElementArray found);
    void FindFirstBuildCache(); void FindAllBuildCache(); void BuildUpdatedCache();
    [PreserveSig] int GetCurrentPropertyValue(int property, [MarshalAs(UnmanagedType.Struct)] out object value);
    void GetCurrentPropertyValueEx(); void GetCachedPropertyValue(); void GetCachedPropertyValueEx();
    void GetCurrentPatternAs(); void GetCachedPatternAs();
    [PreserveSig] int GetCurrentPattern(int pattern, [MarshalAs(UnmanagedType.IUnknown)] out object? provider);
}

[ComImport, Guid("14314595-b4bc-4055-95f2-58f2e42c9855"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationElementArray
{
    [PreserveSig] int GetLength(out int length);
    [PreserveSig] int GetElement(int index, out IUIAutomationElement element);
}

[ComImport, Guid("fb377fbe-8ea6-46d5-9c73-6499642d3059"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationInvokePattern { [PreserveSig] int Invoke(); }

[ComImport, Guid("94cf8058-9b8d-4ab9-8bfd-4cd0a33c8c70"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationTogglePattern
{
    [PreserveSig] int Toggle();
    [PreserveSig] int GetCurrentToggleState(out int state);
}

[ComImport, Guid("a94cd8b1-0844-4cd6-9d2d-640537ab39e9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationValuePattern
{
    [PreserveSig] int SetValue([MarshalAs(UnmanagedType.BStr)] string value);
    [PreserveSig] int GetCurrentValue([MarshalAs(UnmanagedType.BStr)] out string value);
}

[ComImport, Guid("59213f4f-7346-49e5-b120-80555987a148"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationRangeValuePattern
{
    [PreserveSig] int SetValue(double value);
    [PreserveSig] int GetCurrentValue(out double value);
}

[ComImport, Guid("a8efa66a-0fda-421a-9194-38021f3578ea"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationSelectionItemPattern
{
    [PreserveSig] int Select();
    void AddToSelection(); void RemoveFromSelection();
    [PreserveSig] int GetCurrentIsSelected(out int selected);
}

[ComImport, Guid("619be086-1f4e-4ee4-bafa-210128738730"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationExpandCollapsePattern
{
    [PreserveSig] int Expand();
    [PreserveSig] int Collapse();
    [PreserveSig] int GetCurrentState(out int state);
}

/// <summary>Một phần tử đọc qua UIA client: các thuộc tính chính tại thời điểm hỏi.</summary>
sealed record UiaSeen(IUIAutomationElement Element, int ControlType, string Name, string AutomationId, bool Focusable, bool Enabled, double[] Rect)
{
    public const int Invoke = 10000, Value = 10002, RangeValue = 10003, ExpandCollapse = 10005, SelectionItem = 10010, Toggle = 10015;

    public static UiaSeen Read(IUIAutomationElement e)
    {
        object V(int id) { e.GetCurrentPropertyValue(id, out var v); return v; }
        return new UiaSeen(e, V(30003) as int? ?? 0, V(30005) as string ?? "", V(30011) as string ?? "", V(30009) as bool? ?? false,
            V(30010) as bool? ?? false, V(30001) as double[] ?? []);
    }

    public T? Pattern<T>(int id) where T : class
    {
        int hr = Element.GetCurrentPattern(id, out var p);
        return hr == 0 ? p as T : null;
    }
}

[ComImport, Guid("c270f6b5-5c69-4290-9745-7a7f97169468"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationFocusChangedEventHandler { [PreserveSig] int HandleFocusChangedEvent(IUIAutomationElement sender); }

[ComImport, Guid("40cd37d4-c756-4b0c-8c6f-bddfeeb13b50"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IUIAutomationPropertyChangedEventHandler
{
    [PreserveSig] int HandlePropertyChangedEvent(IUIAutomationElement sender, int propertyId, [MarshalAs(UnmanagedType.Struct)] object newValue);
}

/// <summary>Nhận sự kiện UIA (đến từ luồng UIA): ghi "focus:Tên" và "prop:Id:Tên=giá trị".</summary>
[ClassInterface(ClassInterfaceType.None), ComVisible(true)]
sealed class UiaEventSink : IUIAutomationFocusChangedEventHandler, IUIAutomationPropertyChangedEventHandler
{
    public readonly System.Collections.Concurrent.ConcurrentQueue<string> Events = new();

    public int HandleFocusChangedEvent(IUIAutomationElement sender) { Events.Enqueue("focus:" + UiaSeen.Read(sender).Name); return 0; }

    public int HandlePropertyChangedEvent(IUIAutomationElement sender, int propertyId, object newValue)
    {
        Events.Enqueue($"prop:{propertyId}:{UiaSeen.Read(sender).Name}={newValue}");
        return 0;
    }
}
