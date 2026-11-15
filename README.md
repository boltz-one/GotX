# GotX

**GotX** — thư viện UI vẽ bằng GPU cho .NET, viết giao diện bằng class Tailwind.

Không có PackageReference nào. Chỉ dùng BCL và API hệ điều hành (Win32, D3D11, IMM32 qua P/Invoke), tương thích Native AOT. Hiện chỉ chạy trên Windows.

**Về tên gọi:** *gọt* như trong "gọt giũa" — làm cho tinh và mượt; **X** là đa nền tảng.

## Ví dụ nhanh

```csharp
using GotX;
using GotX.Platform.Windows;

Application.Run(new WindowsPlatform(), new WindowOptions("Hello", 480, 240), app => app.New(new HelloView()));

public sealed class HelloView : View
{
    int _count;

    public override Div Render(Window w) =>
        w.Div().Class("flex flex-col items-center justify-center gap-4 size-full bg-slate-100")
            .Child(w.Div().Class("text-2xl font-bold text-slate-900").Child($"Đã bấm {_count} lần"))
            .Child(w.Div().Class("rounded-lg bg-indigo-600 px-4 py-2 text-white hover:bg-indigo-700 active:bg-indigo-800")
                .Child("Bấm tôi")
                .OnClick(this, static (v, _) => v.Add(1)));

    void Add(int d) { _count += d; Notify(); }
}
```

Project cần `ProjectReference` tới `src/GotX.Platform.Windows/GotX.Platform.Windows.csproj` (xem `samples/Hello/Hello.csproj`). Demo đầy đủ: `samples/Hello`.

## Build, chạy, test

```bash
dotnet build GotX.slnx                                        # build tất cả project
dotnet run --project samples/Hello                            # demo cửa sổ, Windows, D3D11
set GOTX_RENDERER=software                                    # ép renderer CPU (cmd; PowerShell: $env:GOTX_RENDERER = "software")
dotnet run --project samples/Headless -- out 2                # không cửa sổ, in thống kê, xuất PNG vào out, scale 2
dotnet run --project samples/Components                       # gallery component (Windows)
dotnet run --project samples/ComponentsCheck -- out           # kiểm tra component headless; mã thoát 2 nếu lỗi
dotnet run --project tests/GotX.Tests                         # bộ test; thêm --filter, --list, --update-snapshots
dotnet run --project tools/StyleGen -- src/GotX.Core/Styling  # sinh lại Styled.g.cs, Tw.g.cs (ghi đè, không sửa tay)
dotnet publish samples/Hello -r win-x64 -c Release            # Native AOT
```

- Tại thời điểm viết: Headless 109 check, ComponentsCheck 464 check, GotX.Tests 96 test. Các số này chưa chạy lại; trạng thái và tiêu chí release xem `docs/project-overview-pdr.md` §7 và §9.
- Headless và ComponentsCheck không cần cửa sổ, nhưng chưa kiểm chứng ngoài Windows (`docs/project-overview-pdr.md` NFR-11).

## Tour nhanh

### Styling

```csharp
w.Div().Class("flex items-center gap-2 px-4 py-2 rounded-lg bg-indigo-600 text-white " +
              "hover:bg-indigo-700 active:bg-indigo-800 md:px-6 dark:bg-indigo-500 shadow-lg shadow-indigo-500/30")

w.Div().Flex().ItemsCenter().Gap2().Px4().Py2().RoundedLg().Bg(Tw.Indigo600).TextColor(Tw.White)

w.Div().Class("flex-1 overflow-y-auto")                            // vùng cuộn: lăn chuột, cuộn mượt, thanh cuộn
w.Div().Class("grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4")
w.Div().Class("transition duration-200 hover:-translate-y-1 hover:shadow-lg active:scale-95")
```

- Hai cách trên ghi vào cùng `Style`. Chuỗi class (Tailwind v4) được biên dịch một lần rồi cache; API có kiểu cho IDE gợi ý, sai tên thì lỗi lúc biên dịch.
- `Theme.SetColor("primary", Tw.Indigo600, Tw.Indigo400)`: token ngữ nghĩa tự đổi theo dark mode, không cần `dark:` đi kèm. Token khác có thể sửa lúc khởi động qua `Theme`.
- Variant `hover:`, `focus:`, `active:`, `disabled:` chỉ đổi phần vẽ, không đổi layout.
- Bảng class được hỗ trợ: `docs/tailwind.md` §3. Class chưa hỗ trợ không bị bỏ qua im lặng (`Tailwind.OnUnknownClass`).

### Sự kiện và overlay

```csharp
w.Div().OnClick(this, static (v, e) => ...)        // nút trái
       .OnAuxClick(this, static (v, e) => ...)     // nút phải / giữa (e.Button)
       .OnHover(this, static (v, inside) => ...)   // chuột vào / ra
       .OnDrag(this, static (v, e) => ...)         // kéo: e.Phase, e.FractionX (0..1, slider, thanh chia)
       .Anchor("nút-mở")                           // tên để overlay neo vào

// Lớp trên cùng cửa sổ: không bị cha cắt, cuộn hay biến đổi
var dialog = window.OpenOverlay(view, new OverlayOptions { Modal = true });   // có backdrop, chặn bên dưới
var menu = window.OpenOverlay(list, new OverlayOptions { Anchor = "nút-mở", Placement = Placement.BottomStart });
menu.Close();                                       // Escape và bấm ra ngoài cũng đóng overlay trên cùng

app.OpenWindow(new WindowOptions("Cửa sổ 2", 420, 260), app.New(new OtherView()));   // thêm cửa sổ
app.Clipboard.SetText("...");                       // Ctrl+C / X / V trong ô nhập dùng clipboard này
```

Overlay neo theo anchor tự lật sang cạnh đối diện khi thiếu chỗ và trượt để nằm trong cửa sổ. Chi tiết: `docs/system-architecture.md` §7.

### Bàn phím, hẹn giờ, hình vector, ảnh

```csharp
w.Div().Key("save").Focusable()                     // điểm dừng của Tab; Enter/Space gọi OnClick
       .Class("focus-visible:ring-2 focus-visible:ring-indigo-500")   // vòng focus chỉ khi focus bằng bàn phím
       .OnClick(this, static (v, _) => v.Save());
w.Div().OnKey(this, static (v, e) => e.Key == Key.Down && v.Next());  // nhận phím của phần tử con đang focus; false = nổi lên cha
w.Div().Focusable().AutoFocus()                     // tự nhận focus khi xuất hiện (menu, hộp thoại)
window.FocusKey("save"); window.MoveFocus(backward: false); window.Blur();
window.ScrollIntoView("tên-anchor");                // cuộn các vùng cuộn bao ngoài tới phần tử

var timer = app.SetTimeout(0.7, () => Open());      // giây; callback chạy trên main thread; timer.Dispose() để huỷ
app.SetInterval(1, Tick);

static readonly VectorPath Check = VectorPath.Icon("M20 6 9 17l-5-5");   // cú pháp d của SVG
w.Path(Check).Class("size-4 text-emerald-600");     // hình vector tô bằng màu chữ
w.Image(ImageSource.FromPngFile("avatar.png")).Class("size-10 rounded-full object-cover");
```

- Thứ tự xử lý phím, focus và Tab: `docs/system-architecture.md` §6.5.
- Tạo `VectorPath` một lần (`static readonly`), không tạo trong `Render()`.
- Ảnh: giải mã PNG bằng BCL (`ZLibStream`); định dạng khác tự giải mã rồi dùng `ImageSource.FromRgba`.

### Entity và effect

```csharp
public sealed class CounterView : View
{
    int _count;
    void Add(int d) { _count += d; Notify(); }      // xếp hàng effect, chưa vẽ ngay
    public override Div Render(Window w) => w.Div()...;
}

var counter = app.New(new CounterView());           // tạo qua app để được cấp Id
app.Observe(counter, () => ...);                    // chạy khi counter Notify
app.Subscribe<TextInput.TextChanged>(input, e => ...);   // nhận sự kiện có kiểu
w.View(counter).W72();                              // nhúng view con, có cache riêng
```

Effect được xử lý một lượt sau mỗi sự kiện input (`App.Flush`); effect sinh thêm trong lượt đó chạy tiếp trong cùng lượt. `app.Release(entity)` gỡ entity cùng observer và subscriber của nó.

### Cache view

Mỗi view nhớ kết quả đo, bounds, clip và đoạn primitive của frame trước. Nếu view không bị đánh dấu bẩn và nhận đúng bounds đó, GotX chép lại kết quả thay vì gọi `Render()`. `Notify()` đánh dấu view và mọi view cha; hover chỉ làm bẩn view chứa phần tử có hover. Chi tiết: `docs/system-architecture.md` §4.

## Dự án và cấu trúc

| Project | Vai trò |
|---|---|
| `src/GotX.Core` | Engine UI thuần BCL: App, Window, layout, Tailwind, chữ, media, overlay, scene. Không có code OS |
| `src/GotX.Raster` | Renderer CPU (SDF) và PNG writer; dùng cho headless, test và dự phòng |
| `src/GotX.Platform.Windows` | Win32 (DPI, chuột, phím, IME, clipboard) và backend D3D11 |
| `src/GotX.Components` | Bộ component kiểu shadcn/ui |
| `src/GotX.Test` | Khung test và soi lỗi giao diện headless |
| `tests/GotX.Tests` | Bộ test của repo |
| `samples/Hello` | Demo cửa sổ Windows, Native AOT |
| `samples/Headless` | Demo không cửa sổ: thống kê cache, xuất PNG |
| `samples/Components` | Gallery component (Windows) |
| `samples/ComponentsCheck` | Kiểm tra hành vi và ảnh của component, headless |
| `tools/StyleGen` | Sinh `Styled.g.cs` và `Tw.g.cs` |

Muốn đổi một việc thì sửa file nào: `docs/codebase-summary.md` §4. Bản đồ đầy đủ: `docs/codebase-summary.md` §2.

## Tài liệu

| File | Nội dung |
|---|---|
| [docs/project-overview-pdr.md](docs/project-overview-pdr.md) | Mục tiêu, yêu cầu chức năng và phi chức năng, trạng thái, giới hạn, tiêu chí release |
| [docs/codebase-summary.md](docs/codebase-summary.md) | Bản đồ project, file và type; "muốn đổi X thì sửa ở đâu" |
| [docs/system-architecture.md](docs/system-architecture.md) | Pipeline frame, cache view, input, overlay, chữ, renderer CPU và GPU |
| [docs/code-standards.md](docs/code-standards.md) | Ràng buộc (không package, AOT), quy ước code, checklist trước merge |
| [docs/tailwind.md](docs/tailwind.md) | Cơ chế utility của Tailwind CSS và bảng class được hỗ trợ |
| [docs/components.md](docs/components.md) | Quy ước component, API sự kiện và overlay, danh sách component |
| [docs/testing.md](docs/testing.md) | GotX.Test: harness, assertion, lint, snapshot, đo hiệu năng |
| [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) | Ghi công và giấy phép: font-rs, Tailwind CSS, shadcn/ui, Lucide |

## Giới hạn hiện tại

- Chỉ Windows. `IPlatform` đã tách nhưng chưa có backend thứ hai.
- Chữ: không kerning, shaping, font fallback; không đọc TTC/CFF; chỉ Segoe UI và Consolas.
- Tailwind: chưa có gradient, `z-*`, `fixed`/`sticky`, `m*-auto`, biến đổi 3D, `peer-*`.
- Layout: chưa có `align-content`, `col-start`; thanh cuộn không kéo được bằng chuột.
- Focus: Tab đi theo thứ tự vẽ, không có `tabindex`; phần tử thường không nhận typeahead.
- Ô nhập: không undo, không con trỏ nhấp nháy, không cuộn trong ô.
- Hình vector một màu; ảnh chỉ đọc PNG không interlace.
- Không có damage region: mọi frame vẽ lại toàn bộ Scene.
- Chưa có test tự động so sánh D3D11 với CPU; chưa kiểm chứng trên Linux hoặc macOS.
- App, Window và Entity chạy trên một luồng UI, không có khoá; thread khác đưa việc vào bằng `app.Post`.

Danh sách đầy đủ và lý do: `docs/project-overview-pdr.md` §8.

## Giấy phép bên thứ ba

GotX dẫn xuất thuật toán, bảng số và path icon từ font-rs, Tailwind CSS, shadcn/ui và Lucide. Ghi công và giấy phép: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
