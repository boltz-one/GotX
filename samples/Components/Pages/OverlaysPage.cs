using GotX;
using GotX.Components;

/// <summary>Trang gallery nhóm Overlays: mỗi component một nút mở, dòng trạng thái ghi sự kiện gần nhất.</summary>
public sealed class OverlaysPage : View
{
    Window? _window;
    Dialog _dialog = null!;
    AlertDialog _alert = null!;
    Sheet _sheet = null!;
    Drawer _drawer = null!;
    Popover _popover = null!;
    Tooltip _tooltip = null!;
    HoverCard _hover = null!;
    DropdownMenu _dropdown = null!;
    ContextMenu _context = null!;
    Menubar _menubar = null!;
    NavigationMenu _nav = null!;
    Select _select = null!;
    Combobox _combo = null!;
    Command _command = null!, _palette = null!;
    Toaster _toaster = null!;
    Indexed<OverlaysPage>[]? _sides, _toasts;
    string _log = "Chưa có sự kiện.";

    static readonly string[] SideNames = ["Phải", "Trái", "Trên", "Dưới"];
    static readonly (string Label, ToastVariant Variant)[] ToastKinds =
        [("Mặc định", ToastVariant.Default), ("Thành công", ToastVariant.Success), ("Thông tin", ToastVariant.Info),
         ("Cảnh báo", ToastVariant.Warning), ("Lỗi", ToastVariant.Error)];

    /// <summary>Tạo các component và nối sự kiện của chúng vào dòng trạng thái.</summary>
    protected override void OnCreated()
    {
        _dialog = App.New(new Dialog { Title = "Sửa hồ sơ", Description = "Thay đổi hồ sơ rồi bấm Lưu." });
        _dialog.Footer = w => Ui.Button(w, "Lưu").Focusable().OnClick(this, static (p, _) => p._dialog.Close());
        _alert = App.New(new AlertDialog
        {
            Title = "Bạn chắc chắn chứ?", ActionText = "Xoá", ActionVariant = ButtonVariant.Destructive,
            Description = "Thao tác này không hoàn tác được: tài khoản và dữ liệu sẽ bị xoá vĩnh viễn.",
        });
        _sheet = App.New(new Sheet { Title = "Sửa hồ sơ", Description = "Thay đổi rồi bấm Lưu khi xong." });
        _sheet.Footer = w => Ui.Button(w, "Lưu").Focusable().OnClick(this, static (p, _) => p._sheet.Close());
        _drawer = App.New(new Drawer { Title = "Mục tiêu hằng ngày", Description = "Đặt số calo cần đốt mỗi ngày." });
        _drawer.Body = static w => w.Div().Class("flex justify-center text-5xl font-bold").Child("350");
        _drawer.Footer = w => Ui.Button(w, "Đóng", ButtonVariant.Outline).Focusable().OnClick(this, static (p, _) => p._drawer.Close());
        _popover = App.New(new Popover
        {
            TriggerText = "Mở popover", Title = "Kích thước", Description = "Đặt kích thước cho lớp.",
            Body = static w => w.Div().Class("grid grid-cols-2 gap-2 text-sm")
                .Child(w.Div().Class("text-muted-foreground").Child("Rộng")).Child("100%")
                .Child(w.Div().Class("text-muted-foreground").Child("Cao")).Child("25px"),
        });
        _tooltip = App.New(new Tooltip { Text = "Thêm vào thư viện", Trigger = static w => Ui.Button(w, "Rê chuột", ButtonVariant.Outline).Focusable() });
        _hover = App.New(new HoverCard
        {
            Trigger = static w => Ui.Button(w, "@nextjs", ButtonVariant.Link).Class("hover:underline").Focusable(),
            Body = static w => w.Div().Class("flex flex-col gap-1")
                .Child(w.Div().Class("text-sm font-semibold").Child("@nextjs"))
                .Child(w.Div().Class("text-sm").Child("Framework React của Vercel."))
                .Child(w.Div().Class("text-xs text-muted-foreground").Child("Tham gia tháng 12/2021")),
        });
        _dropdown = App.New(new DropdownMenu { TriggerText = "Tài khoản", Items = OverlaysDemoData.AccountMenu() });
        _context = App.New(new ContextMenu { Items = OverlaysDemoData.BrowserMenu() });
        _menubar = App.New(new Menubar { Menus = OverlaysDemoData.AppMenus() });
        _nav = App.New(new NavigationMenu { Items = OverlaysDemoData.Navigation() });
        _select = App.New(new Select { Items = ["Táo", "Chuối", "Việt quất", "Nho"], Placeholder = "Chọn quả" });
        _combo = App.New(new Combobox { Items = OverlaysDemoData.Frameworks, Placeholder = "Chọn framework…" });
        _command = App.New(new Command { Groups = OverlaysDemoData.Commands(), PanelClass = "w-80 border border-border" });
        _palette = App.New(new Command { Groups = OverlaysDemoData.Commands() });
        _toaster = App.New(new Toaster());

        App.Subscribe<AlertDialog.Confirmed>(_alert, _ => Log("AlertDialog: xác nhận"));
        App.Subscribe<AlertDialog.Cancelled>(_alert, _ => Log("AlertDialog: huỷ"));
        foreach (var menu in new MenuOwner[] { _dropdown, _context, _menubar })
        {
            App.Subscribe<MenuItemSelected>(menu, e => Log($"Menu: {e.Item.Text}"));
            App.Subscribe<MenuItemToggled>(menu, e => Log($"Menu: {e.Item.Text} = {(e.Checked ? "bật" : "tắt")}"));
        }
        App.Subscribe<NavigationMenu.LinkSelected>(_nav, e => Log($"Điều hướng: {e.Title}"));
        App.Subscribe<Select.ValueChanged>(_select, e => Log($"Select: {e.Value}"));
        App.Subscribe<Combobox.ValueChanged>(_combo, e => Log($"Combobox: {e.Value}"));
        App.Subscribe<Command.ItemSelected>(_command, e => Log($"Command: {e.Item.Text}"));
        App.Subscribe<Command.ItemSelected>(_palette, e => Log($"Bảng lệnh: {e.Item.Text}"));
        App.Subscribe<Toaster.ActionClicked>(_toaster, e => Log($"Toast #{e.Id}: hoàn tác"));
    }

    void Log(string text) { _log = text; Notify(); }

    void OpenSheet(int side) { _sheet.Side = (PanelSide)side; _sheet.Open(_window!); }

    void ShowToast(int kind)
    {
        var (label, variant) = ToastKinds[kind];
        _toaster.Show(_window!, new Toast
        {
            Title = "Thông báo: " + label, Description = "Sự kiện đã được lên lịch.", Variant = variant,
            ActionText = variant == ToastVariant.Default ? "Hoàn tác" : "",
        });
    }

    /// <summary>Dựng trang: mỗi mục một nhóm nút mở.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var sides = Indexed<OverlaysPage>.Ensure(ref _sides, this, SideNames.Length);
        var toasts = Indexed<OverlaysPage>.Ensure(ref _toasts, this, ToastKinds.Length);

        var sheetRow = Demo.Row(w);
        for (int i = 0; i < SideNames.Length; i++)
            sheetRow.Child(Ui.Button(w, SideNames[i], ButtonVariant.Outline).OnClick(sides[i], static (r, _) => r.Owner.OpenSheet(r.Index)));
        var toastRow = Demo.Row(w);
        for (int i = 0; i < ToastKinds.Length; i++)
            toastRow.Child(Ui.Button(w, ToastKinds[i].Label, ButtonVariant.Outline).OnClick(toasts[i], static (r, _) => r.Owner.ShowToast(r.Index)));

        return Demo.Page(w)
            .Child(w.Div().Class("text-sm text-muted-foreground").Child(_log))
            .Child(Demo.Section(w, "Dialog & Alert Dialog", "Mở thì focus vào hộp thoại, Tab đi vòng bên trong; alert dialog không đóng khi bấm ra ngoài.").Child(Demo.Row(w)
                .Child(Ui.Button(w, "Mở dialog", ButtonVariant.Outline).OnClick(this, static (p, _) => p._dialog.Open(p._window!)))
                .Child(Ui.Button(w, "Xoá tài khoản", ButtonVariant.Destructive).OnClick(this, static (p, _) => p._alert.Open(p._window!)))))
            .Child(Demo.Section(w, "Sheet", "Tấm modal từ một cạnh cửa sổ.").Child(sheetRow))
            .Child(Demo.Section(w, "Drawer", "Ngăn kéo từ đáy hoặc cạnh phải, có thanh kéo.").Child(Demo.Row(w)
                .Child(Ui.Button(w, "Drawer đáy", ButtonVariant.Outline)
                    .OnClick(this, static (p, _) => { p._drawer.Side = PanelSide.Bottom; p._drawer.Open(p._window!); }))
                .Child(Ui.Button(w, "Drawer phải", ButtonVariant.Outline)
                    .OnClick(this, static (p, _) => { p._drawer.Side = PanelSide.Right; p._drawer.Open(p._window!); }))))
            .Child(Demo.Section(w, "Popover, Tooltip, Hover Card", "Tooltip hiện ngay (cả khi Tab tới); hover card mở sau 0,7 giây, đóng sau 0,3 giây.").Child(Demo.Row(w)
                .Child(w.View(_popover)).Child(w.View(_tooltip)).Child(w.View(_hover))))
            .Child(Demo.Section(w, "Dropdown Menu & Context Menu", "Nhãn, phím tắt, checkbox, dòng tắt, menu con; ↑/↓/Enter/→/←; chuột phải vào khung.").Child(Demo.Row(w).Class("items-start")
                .Child(w.View(_dropdown)).Child(w.View(_context))))
            .Child(Demo.Section(w, "Menubar & Navigation Menu", "←/→ chuyển menu; navigation menu mở khi rê chuột hoặc Enter, ↓ đi vào tấm.").Child(Demo.Row(w).Child(w.View(_menubar)).Child(w.View(_nav))))
            .Child(Demo.Section(w, "Select & Combobox", "↑/↓/Home/End rồi Enter; combobox lọc danh sách khi gõ.").Child(Demo.Row(w)
                .Child(w.View(_select)).Child(w.View(_combo))))
            .Child(Demo.Section(w, "Command", "Nhúng thẳng, hoặc mở thành hộp thoại; ↑/↓ đổi dòng đang chọn, Enter chạy.").Child(Demo.Row(w).Class("items-start")
                .Child(w.View(_command))
                .Child(Ui.Button(w, "Mở bảng lệnh", ButtonVariant.Outline).OnClick(this, static (p, _) => p._palette.OpenDialog(p._window!)))))
            .Child(Demo.Section(w, "Toast", "Chồng thông báo góc dưới phải, tự đóng sau 4 giây; rê chuột lên thì tạm dừng.").Child(toastRow));
    }
}
