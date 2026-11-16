using System.Runtime.InteropServices;
using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

/// <summary>
/// Một phần tử UIA (id 0 = gốc cửa sổ). Là đối tượng COM viết tay: mỗi interface là một "ô" 16 byte trong khối native
/// [con trỏ vtable, GCHandle tới đối tượng]; QueryInterface trả về địa chỉ ô tương ứng. Không dùng reflection / COM interop sinh mã.
/// </summary>
internal sealed unsafe partial class UiaElement
{
    public const int E_FAIL = unchecked((int)0x80004005), E_NOINTERFACE = unchecked((int)0x80004002),
        UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201), S_OK = 0;
    public const int SlotSimple = 0, SlotFragment = 1, SlotRoot = 2, SlotInvoke = 3, SlotToggle = 4, SlotValue = 5,
        SlotExpand = 6, SlotSelection = 7, SlotRange = 8, Slots = 9;

    readonly UiaBridge _bridge;
    public readonly long Id;
    readonly nint _block;
    nint _handle;
    int _refs;

    public UiaElement(UiaBridge bridge, long id)
    {
        _bridge = bridge;
        Id = id;
        _block = (nint)NativeMemory.Alloc(Slots * 16);
        for (int i = 0; i < Slots; i++) { ((nint*)_block)[i * 2] = UiaVtables.Table[i]; ((nint*)_block)[i * 2 + 1] = 0; }
    }

    public bool IsRoot => Id == 0;
    public UiaSnapshot Snapshot => _bridge.Snapshot;
    public UiaNode? Node => IsRoot ? null : Snapshot.Get(Id);

    /// <summary>Con trỏ interface (đã AddRef) của ô <paramref name="slot"/>.</summary>
    public nint Acquire(int slot = SlotSimple)
    {
        AddRef();
        return _block + slot * 16;
    }

    public int AddRef()
    {
        int n = Interlocked.Increment(ref _refs);
        if (n == 1)
        {
            _handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
            for (int i = 0; i < Slots; i++) ((nint*)_block)[i * 2 + 1] = _handle;
        }
        return n;
    }

    public int Release()
    {
        int n = Interlocked.Decrement(ref _refs);
        if (n == 0)
        {
            var h = _handle;
            _handle = 0;
            NativeMemory.Free((void*)_block); // ô cuối cùng đã được trả: không ai còn giữ con trỏ vào khối
            GCHandle.FromIntPtr(h).Free();
        }
        return n;
    }

    public int QueryInterface(Guid* iid, void** result)
    {
        int slot = UiaVtables.SlotFor(*iid, IsRoot);
        if (slot < 0) { *result = null; return E_NOINTERFACE; }
        *result = (void*)Acquire(slot);
        return S_OK;
    }

    public static UiaElement? From(nint self) => self == 0 ? null : GCHandle.FromIntPtr(((nint*)self)[1]).Target as UiaElement;

    // ---- Simple ----
    public int GetPattern(int pattern, nint* result)
    {
        *result = 0;
        var n = Node;
        if (n is null) return IsRoot ? S_OK : UIA_E_ELEMENTNOTAVAILABLE;
        int slot = pattern switch
        {
            UiaProps.PatternInvoke when UiaProps.HasInvoke(n) => SlotInvoke,
            UiaProps.PatternToggle when UiaProps.HasToggle(n) => SlotToggle,
            UiaProps.PatternValue when UiaProps.HasValue(n) => SlotValue,
            UiaProps.PatternExpandCollapse when UiaProps.HasExpand(n) => SlotExpand,
            UiaProps.PatternSelectionItem when UiaProps.HasSelection(n) => SlotSelection,
            UiaProps.PatternRangeValue when n.HasRange => SlotRange,
            _ => -1,
        };
        if (slot >= 0) *result = Acquire(slot);
        return S_OK;
    }

    public int GetProperty(int property, VARIANT* value)
    {
        *value = default;
        var n = Node;
        if (n is null) return IsRoot ? S_OK : UIA_E_ELEMENTNOTAVAILABLE;
        *value = UiaProps.Get(n, property);
        return S_OK;
    }

    public int GetHost(nint* result)
    {
        *result = 0;
        if (!IsRoot) return S_OK;
        nint host;
        int hr = UiaHostProviderFromHwnd(_bridge.Hwnd, &host);
        if (hr >= 0) *result = host;
        return hr;
    }
}
