using GotX;
using GotX.Components;

// Kiểm tra headless cho GotX.Components: hành vi từng nhóm component + xuất ảnh các trang gallery (sáng và tối).
// dotnet run --project samples/ComponentsCheck -- [thư mục ra]
string outDir = args.Length > 0 ? args[0] : "components-out";
ComponentTheme.Install();
var h = new CheckHarness(outDir);

void Group(string name, Action<CheckHarness> run)
{
    int pass = h.Pass, fail = h.Fail;
    try { run(h); }
    catch (Exception ex) { h.Fail++; Console.WriteLine($"  ✗ {name}: ném {ex.GetType().Name}: {ex.Message}"); }
    Console.WriteLine($"{name,-22} {h.Pass - pass,3} đạt, {h.Fail - fail} lỗi");
}

Group("Nền (Core)", CoreChecks.Run);
Group("Hiển thị", DisplayChecks.Run);
Group("Biểu mẫu", FormsChecks.Run);
Group("Overlay", OverlaysChecks.Run);
Group("Điều hướng & dữ liệu", NavigationChecks.Run);
Group("Chat", ChatChecks.Run);
Group("Dữ liệu", DataChecks.Run);

// Ảnh từng trang gallery, sáng và tối. Trang cao hơn cửa sổ thì chụp ở chiều cao lớn để thấy hết.
(string Name, Func<App, View> Page)[] pages =
[
    ("display", app => app.New(new DisplayPage())), ("forms", app => app.New(new FormsPage())),
    ("overlays", app => app.New(new OverlaysPage())), ("navigation", app => app.New(new NavigationPage())), ("chat", app => app.New(new ChatPage())), ("data", app => app.New(new DataPage())),
];
h.Size = new Size(1000, 3400); // đủ cao cho trang dài nhất (Display ~3200px)
foreach (var (name, page) in pages)
    foreach (bool dark in new[] { false, true })
    {
        try { h.Save(h.Open(page, dark), $"page-{name}-{(dark ? "dark" : "light")}.png"); }
        catch (Exception ex) { h.Fail++; Console.WriteLine($"  ✗ trang {name}: ném {ex.GetType().Name}: {ex.Message}"); }
    }
h.Size = new Size(1100, 760);
h.Save(h.Open(app => app.New(new GalleryView())), "gallery.png");

var unknown = h.UnknownClasses.Distinct().ToList();
Console.WriteLine(unknown.Count == 0
    ? "Không có class Tailwind nào chưa hỗ trợ."
    : $"Class chưa hỗ trợ ({unknown.Count}):\n  " + string.Join("\n  ", unknown));
Console.WriteLine($"Tổng: {h.Pass} đạt, {h.Fail} lỗi.");
return h.Fail == 0 && unknown.Count == 0 ? 0 : 2;
