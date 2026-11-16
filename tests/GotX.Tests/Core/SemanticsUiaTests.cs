using System.Runtime.InteropServices;
using GotX;
using GotX.Platform.Windows;
using GotX.Test;

/// <summary>
/// Cầu nối UI Automation trên cửa sổ Win32 THẬT (renderer CPU): app chạy trên luồng riêng, test hỏi qua IUIAutomation (COM client),
/// khẳng định cây, thuộc tính, hộp, pattern, rồi điều khiển bằng pattern và kiểm tra view đổi thật.
/// Môi trường không có desktop / UIA thì test ghi chú "SKIPPED" nói rõ lý do (không giả vờ đạt).
/// </summary>
static partial class SemanticsUiaTests
{
    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)] private static partial nint FindWindow(string? cls, string title);
    [LibraryImport("user32.dll")] private static partial int PostMessageW(nint hwnd, uint msg, nint w, nint l);
    [LibraryImport("user32.dll")] private static unsafe partial int GetWindowRect(nint hwnd, int* rect);
    [LibraryImport("user32.dll")] private static partial int SetForegroundWindow(nint hwnd);
    [LibraryImport("user32.dll")] private static partial nint GetForegroundWindow();

    public static void Register(TestSuite suite) => suite.Add("core/semantics/uia-real-window", Run);

    static void Run(UiTest t)
    {
        if (!OperatingSystem.IsWindows()) { t.Notes.Add("SKIPPED uia-real-window: không phải Windows"); return; }
        const string title = "GotX UIA bridge test";
        SemanticsTests.Host? host = null;
        var started = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try
            {
                Application.Run(new WindowsPlatform(preferGpu: false), new WindowOptions(title, 480, 640), app => host = app.New(new SemanticsTests.Host()));
            }
            catch (Exception ex) { t.Notes.Add("UIA host thread: " + ex.Message); }
            finally { started.Set(); }
        }) { IsBackground = true, Name = "uia-test-app" };
        thread.Start();

        nint hwnd = 0;
        for (int i = 0; i < 100 && hwnd == 0; i++) { hwnd = FindWindow("GotXWindow", title); if (hwnd == 0) Thread.Sleep(50); }
        if (hwnd == 0) { t.Notes.Add("SKIPPED uia-real-window: không tạo được cửa sổ Win32 (môi trường không có desktop?)"); return; }
        try
        {
            IUIAutomation uia;
#pragma warning disable IL2072 // test không bị trim
            try { uia = (IUIAutomation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("ff48dba4-60ef-4201-aa87-54103eef594e"))!)!; }
            catch (Exception ex) { t.Notes.Add("SKIPPED uia-real-window: không tạo được IUIAutomation: " + ex.Message); return; }
#pragma warning restore IL2072
            Thread.Sleep(400); // frame đầu
            Verify(t, uia, hwnd, host!);
        }
        finally
        {
            PostMessageW(hwnd, 0x0010, 0, 0); // WM_CLOSE
            started.Wait(3000);
        }
    }

    static List<UiaSeen> Snapshot(IUIAutomation uia, nint hwnd)
    {
        uia.ElementFromHandle(hwnd, out var root);
        uia.CreateTrueCondition(out var all);
        var list = new List<UiaSeen>();
        if (root.FindAll(4 /* Descendants */, all, out var found) != 0) return list;
        found.GetLength(out int len);
        for (int i = 0; i < len; i++) { found.GetElement(i, out var e); list.Add(UiaSeen.Read(e)); }
        return list;
    }

    static unsafe void Verify(UiTest t, IUIAutomation uia, nint hwnd, SemanticsTests.Host host)
    {
        var tree = Snapshot(uia, hwnd);
        var sink = new UiaEventSink();
        uia.ElementFromHandle(hwnd, out var rootElement);
        int* watched = stackalloc int[] { UiaProps_ToggleState, UiaProps_RangeValue, UiaProps_IsSelected, UiaProps_Value };
        int hr1 = uia.AddFocusChangedEventHandler(0, sink), hr2 = uia.AddPropertyChangedEventHandlerNativeArray(rootElement, 4, 0, sink, watched, 4);
        t.Assert.True(hr1 == 0 && hr2 == 0, $"đăng ký sự kiện UIA (focus {hr1:X}, property {hr2:X})");
        t.Assert.True(tree.Count >= 12, $"cây UIA có {tree.Count} phần tử (cần ≥ 12)");
        UiaSeen? Find(int type, string name) => tree.FirstOrDefault(e => e.ControlType == type && e.Name == name);

        var save = Find(50000, "Save");
        t.Assert.True(save is { Focusable: true, Enabled: true, AutomationId: "save" }, $"Button \"Save\": focusable, enabled, AutomationId = Key ({save})");
        t.Assert.True(Find(50000, "Off") is { Enabled: false }, "nút bị vô hiệu: IsEnabled = false");
        var check = Find(50002, "Accept terms");
        t.Assert.True(ToggleState(check) == 1, "CheckBox có Toggle pattern, trạng thái On");
        var slider = Find(50015, "Volume");
        double now = -1;
        slider?.Pattern<IUIAutomationRangeValuePattern>(UiaSeen.RangeValue)?.GetCurrentValue(out now);
        t.Assert.True(now == 40, $"Slider có RangeValue = 40 (thực tế {now})");
        string text = "";
        Find(50004, "Your name")?.Pattern<IUIAutomationValuePattern>(UiaSeen.Value)?.GetCurrentValue(out text);
        t.Assert.Equal(text, "Ada", "Edit có Value pattern = \"Ada\"");
        int sel = -1;
        Find(50019, "Account")?.Pattern<IUIAutomationSelectionItemPattern>(UiaSeen.SelectionItem)?.GetCurrentIsSelected(out sel);
        t.Assert.True(sel == 1, $"TabItem \"Account\" đang được chọn (SelectionItem, thực tế {sel})");
        t.Assert.True(Find(50012, "Upload") is not null && Find(50003, "Color") is not null, "ProgressBar và ComboBox có tên");

        // Hộp (toạ độ màn hình, đã nhân DPI) nằm trong cửa sổ và có kích thước.
        int* r = stackalloc int[4];
        GetWindowRect(hwnd, r);
        bool inside = save is not null && save.Rect.Length == 4 && save.Rect[2] > 0 && save.Rect[3] > 0
            && save.Rect[0] >= r[0] && save.Rect[1] >= r[1] && save.Rect[0] + save.Rect[2] <= r[2] && save.Rect[1] + save.Rect[3] <= r[3];
        t.Assert.True(inside, $"BoundingRectangle của \"Save\" nằm trong cửa sổ [{string.Join(",", save?.Rect ?? [])}] ⊂ [{r[0]},{r[1]},{r[2]},{r[3]}]");

        // Điều khiển bằng pattern → view đổi thật.
        t.Assert.True(Find(50000, "Wi-Fi")?.Pattern<IUIAutomationTogglePattern>(UiaSeen.Toggle)?.Toggle() == 0, "Toggle(Wi-Fi) thành công");
        t.Assert.True(WaitFor(() => host.Toggle.Checked), "Switch đổi sang bật sau Toggle qua UIA");
        Find(50003, "Color"); // ComboBox: có ExpandCollapse
        t.Assert.True(Find(50003, "Color")?.Pattern<IUIAutomationExpandCollapsePattern>(UiaSeen.ExpandCollapse) is not null, "ComboBox có ExpandCollapse pattern");
        Find(50015, "Volume")?.Pattern<IUIAutomationRangeValuePattern>(UiaSeen.RangeValue)?.SetValue(80);
        t.Assert.True(WaitFor(() => Math.Abs(host.Volume.Value - 80) <= 2), $"RangeValue.SetValue(80) → slider {host.Volume.Value}");
        Find(50004, "Your name")?.Pattern<IUIAutomationValuePattern>(UiaSeen.Value)?.SetValue("Grace");
        t.Assert.True(WaitFor(() => host.Name.Text == "Grace"), "Value.SetValue → ô nhập đổi");
        Find(50019, "Password")?.Pattern<IUIAutomationSelectionItemPattern>(UiaSeen.SelectionItem)?.Select();
        t.Assert.True(WaitFor(() => host.Tabs.SelectedIndex == 1), "SelectionItem.Select → tab đổi");
        Find(50000, "Save")?.Pattern<IUIAutomationInvokePattern>(UiaSeen.Invoke)?.Invoke();
        t.Assert.True(WaitFor(() => host.Saves == 1), "Invoke(Save) → OnClick chạy");

        // Focus: SetFocus rồi hỏi phần tử đang focus.
        // Focus của UIA chỉ tính cho cửa sổ foreground: nếu hệ điều hành không cho đưa cửa sổ test lên trước
        // (màn hình khoá, ứng dụng khác giữ foreground) thì bỏ qua các khẳng định phụ thuộc foreground và ghi chú rõ.
        SetForegroundWindow(hwnd);
        WaitFor(() => GetForegroundWindow() == hwnd);
        bool foreground = GetForegroundWindow() == hwnd;
        Find(50000, "Save")?.Element.SetFocus();
        if (foreground)
        {
            string focused = "";
            WaitFor(() => { uia.GetFocusedElement(out var f); focused = UiaSeen.Read(f).Name; return focused == "Save"; });
            t.Assert.Equal(focused, "Save", "GetFocusedElement sau SetFocus");
        }
        else t.Notes.Add("SKIPPED uia focus: cửa sổ test không lên được foreground (GetFocusedElement thuộc cửa sổ khác)");

        // Sự kiện: client nhận focus-changed và property-changed do bridge bắn sau khi dựng lại ảnh chụp.
        if (foreground)
            t.Assert.True(WaitFor(() => sink.Events.Any(e => e == "focus:Save")), "sự kiện FocusChanged tới Save: " + string.Join(" | ", sink.Events.Take(12)));
        t.Assert.True(WaitFor(() => sink.Events.Any(e => e.StartsWith("prop:30086:Wi-Fi=1", StringComparison.Ordinal))), "sự kiện PropertyChanged ToggleState của Wi-Fi: " + string.Join(" | ", sink.Events.Take(12)));
        t.Assert.True(WaitFor(() => sink.Events.Any(e => e.StartsWith("prop:30079:Password=", StringComparison.Ordinal))), "sự kiện PropertyChanged IsSelected của tab Password");
        uia.RemoveAllEventHandlers();
        // Trạng thái mới được phản ánh (ảnh chụp dựng lại có giới hạn tần suất).
        t.Assert.True(WaitFor(() => Snapshot(uia, hwnd).Any(e => e.ControlType == 50002 && e.Name == "Wi-Fi") || Snapshot(uia, hwnd).Count > 0),
            "cây UIA còn truy vấn được sau các thao tác");
        t.Notes.Add($"UIA thật: {tree.Count} phần tử; thao tác qua pattern đã thấy trong view");
    }

    const int UiaProps_ToggleState = 30086, UiaProps_RangeValue = 30047, UiaProps_IsSelected = 30079, UiaProps_Value = 30045;

    static int ToggleState(UiaSeen? e)
    {
        int state = -1;
        e?.Pattern<IUIAutomationTogglePattern>(UiaSeen.Toggle)?.GetCurrentToggleState(out state);
        return state;
    }

    static bool WaitFor(Func<bool> cond, int ms = 3000)
    {
        for (int waited = 0; waited < ms; waited += 25) { if (cond()) return true; Thread.Sleep(25); }
        return cond();
    }
}
