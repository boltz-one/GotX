using System.Runtime.InteropServices;
using static GotX.Platform.Windows.UiaElement;

namespace GotX.Platform.Windows;

/// <summary>Bảng hàm (vtable) của các interface provider UIA, trỏ tới các hàm <c>[UnmanagedCallersOnly]</c> tĩnh – không sinh mã lúc chạy.</summary>
internal static unsafe class UiaVtables
{
    static readonly Guid IUnknown = new("00000000-0000-0000-C000-000000000046"),
        Simple = new("D6DD68D1-86FD-4332-8666-9ABEDEA2D24C"), Fragment = new("F7063DA8-8359-439C-9297-BBC5299A7D87"),
        Root = new("620CE2A5-AB8F-40A9-86CB-DE3C75599B58"), Invoke = new("54FCB24B-E18E-47A2-B4D3-ECCBE77599A2"),
        Toggle = new("56D00BD0-C4F4-433C-A836-1A52A57E0892"), Value = new("C7935180-6FB3-4201-B174-7DF73ADBF64A"),
        Expand = new("D847D3A5-CAB0-4A98-8C32-ECB45C59AD24"), Selection = new("2ACAD808-B2D4-452D-A407-91FF1AD167B2"),
        Range = new("36DC7AEF-33E6-4691-AFE1-2BE7274B3D33");

    public static readonly nint[] Table = BuildAll();

    public static int SlotFor(Guid iid, bool isRoot) =>
        iid == IUnknown || iid == Simple ? SlotSimple : iid == Fragment ? SlotFragment : iid == Root ? (isRoot ? SlotRoot : -1)
        : iid == Invoke ? SlotInvoke : iid == Toggle ? SlotToggle : iid == Value ? SlotValue : iid == Expand ? SlotExpand
        : iid == Selection ? SlotSelection : iid == Range ? SlotRange : -1;

    static nint[] BuildAll() =>
    [
        V((nint)(delegate* unmanaged<nint, int*, int>)&ProviderOptions, (nint)(delegate* unmanaged<nint, int, nint*, int>)&Pattern,
          (nint)(delegate* unmanaged<nint, int, Win32.VARIANT*, int>)&Property, (nint)(delegate* unmanaged<nint, nint*, int>)&Host),
        V((nint)(delegate* unmanaged<nint, int, nint*, int>)&Navigate, (nint)(delegate* unmanaged<nint, nint*, int>)&RuntimeId,
          (nint)(delegate* unmanaged<nint, double*, int>)&Bounds, (nint)(delegate* unmanaged<nint, nint*, int>)&Embedded,
          (nint)(delegate* unmanaged<nint, int>)&SetFocus, (nint)(delegate* unmanaged<nint, nint*, int>)&FragmentRoot),
        V((nint)(delegate* unmanaged<nint, double, double, nint*, int>)&FromPoint, (nint)(delegate* unmanaged<nint, nint*, int>)&GetFocus),
        V((nint)(delegate* unmanaged<nint, int>)&DoInvoke),
        V((nint)(delegate* unmanaged<nint, int>)&DoInvoke, (nint)(delegate* unmanaged<nint, int*, int>)&ToggleState),
        V((nint)(delegate* unmanaged<nint, char*, int>)&SetValue, (nint)(delegate* unmanaged<nint, nint*, int>)&GetValue,
          (nint)(delegate* unmanaged<nint, int*, int>)&IsReadOnly),
        V((nint)(delegate* unmanaged<nint, int>)&DoExpand, (nint)(delegate* unmanaged<nint, int>)&DoCollapse, (nint)(delegate* unmanaged<nint, int*, int>)&ExpandState),
        V((nint)(delegate* unmanaged<nint, int>)&DoSelect, (nint)(delegate* unmanaged<nint, int>)&DoSelect, (nint)(delegate* unmanaged<nint, int>)&NotSupported,
          (nint)(delegate* unmanaged<nint, int*, int>)&IsSelected, (nint)(delegate* unmanaged<nint, nint*, int>)&NoContainer),
        V((nint)(delegate* unmanaged<nint, double, int>)&SetRange, (nint)(delegate* unmanaged<nint, double*, int>)&RangeNow,
          (nint)(delegate* unmanaged<nint, int*, int>)&RangeReadOnly, (nint)(delegate* unmanaged<nint, double*, int>)&RangeMax,
          (nint)(delegate* unmanaged<nint, double*, int>)&RangeMin, (nint)(delegate* unmanaged<nint, double*, int>)&RangeLarge,
          (nint)(delegate* unmanaged<nint, double*, int>)&RangeSmall),
    ];

    static nint V(params nint[] methods)
    {
        var p = (nint*)NativeMemory.Alloc((nuint)((3 + methods.Length) * sizeof(nint)));
        p[0] = (nint)(delegate* unmanaged<nint, Guid*, void**, int>)&Query;
        p[1] = (nint)(delegate* unmanaged<nint, int>)&AddRefThunk;
        p[2] = (nint)(delegate* unmanaged<nint, int>)&ReleaseThunk;
        for (int i = 0; i < methods.Length; i++) p[3 + i] = methods[i];
        return (nint)p;
    }

    static UiaElement E(nint self) => From(self)!;

    [UnmanagedCallersOnly] static int Query(nint s, Guid* iid, void** r) { try { return E(s).QueryInterface(iid, r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int AddRefThunk(nint s) { try { return E(s).AddRef(); } catch { return 1; } }
    [UnmanagedCallersOnly] static int ReleaseThunk(nint s) { try { return E(s).Release(); } catch { return 0; } }

    [UnmanagedCallersOnly] static int ProviderOptions(nint s, int* o) { *o = 1; return S_OK; } // ServerSideProvider
    [UnmanagedCallersOnly] static int Pattern(nint s, int id, nint* r) { try { return E(s).GetPattern(id, r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int Property(nint s, int id, Win32.VARIANT* v) { try { return E(s).GetProperty(id, v); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int Host(nint s, nint* r) { try { return E(s).GetHost(r); } catch { return E_FAIL; } }

    [UnmanagedCallersOnly] static int Navigate(nint s, int d, nint* r) { try { return E(s).Navigate(d, r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int RuntimeId(nint s, nint* r) { try { return E(s).RuntimeId(r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int Bounds(nint s, double* r) { try { return E(s).BoundingRect(r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int Embedded(nint s, nint* r) { *r = 0; return S_OK; }
    [UnmanagedCallersOnly] static int SetFocus(nint s) { try { return E(s).SetFocus(); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int FragmentRoot(nint s, nint* r) { try { return E(s).FragmentRoot(r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int FromPoint(nint s, double x, double y, nint* r) { try { return E(s).FromPoint(x, y, r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int GetFocus(nint s, nint* r) { try { return E(s).GetFocus(r); } catch { return E_FAIL; } }

    [UnmanagedCallersOnly] static int DoInvoke(nint s) { try { return E(s).Invoke(); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int ToggleState(nint s, int* r) { try { return E(s).GetInt(r, UiaProps.ToggleState); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int SetValue(nint s, char* v) { try { return E(s).Act(SemanticAction.SetValue, 0, new string(v)); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int GetValue(nint s, nint* r) { try { return E(s).GetBstr(r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int IsReadOnly(nint s, int* r) { try { return E(s).ReadOnly(r); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int DoExpand(nint s) { try { return E(s).Expand(); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int DoCollapse(nint s) { try { return E(s).Collapse(); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int ExpandState(nint s, int* r) { try { return E(s).GetInt(r, UiaProps.ExpandCollapseState); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int DoSelect(nint s) { try { return E(s).Select(); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int NotSupported(nint s) => E_FAIL;
    [UnmanagedCallersOnly] static int IsSelected(nint s, int* r) { try { return E(s).GetInt(r, UiaProps.IsSelected); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int NoContainer(nint s, nint* r) { *r = 0; return S_OK; }
    [UnmanagedCallersOnly] static int SetRange(nint s, double v) { try { return E(s).SetRange(v); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int RangeNow(nint s, double* r) { try { return E(s).GetRange(r, 0); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int RangeReadOnly(nint s, int* r) { try { var n = E(s).Node; *r = n is { Role: SemanticRole.Slider, Enabled: true } ? 0 : -1; return S_OK; } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int RangeMax(nint s, double* r) { try { return E(s).GetRange(r, 1); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int RangeMin(nint s, double* r) { try { return E(s).GetRange(r, 2); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int RangeLarge(nint s, double* r) { try { return E(s).GetRange(r, 3); } catch { return E_FAIL; } }
    [UnmanagedCallersOnly] static int RangeSmall(nint s, double* r) { try { return E(s).GetRange(r, 4); } catch { return E_FAIL; } }
}
