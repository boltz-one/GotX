# GotX – Tổng quan sản phẩm và PDR

> Cập nhật: 2026-11-15 · Phạm vi: repo hiện tại (.NET 11). Windows là nền tảng duy nhất hiện có.
> Liên quan: [codebase-summary.md](codebase-summary.md) · [system-architecture.md](system-architecture.md) · [code-standards.md](code-standards.md) · [components.md](components.md) · [tailwind.md](tailwind.md) · [testing.md](testing.md) · [../README.md](../README.md) · [../THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md)

## 1. Tổng quan

GotX là thư viện UI vẽ bằng GPU cho .NET. Giao diện viết bằng chuỗi class Tailwind v4 (`w.Div().Class("flex gap-2 p-4")`) hoặc API có kiểu tương đương. Chỉ dùng BCL và API hệ điều hành qua P/Invoke; không có PackageReference.

**Đối tượng:**
- Lập trình viên .NET dựng ứng dụng desktop cần giao diện nhẹ, không phụ thuộc bên thứ ba, có thể publish Native AOT.
- Agent và CI cần kiểm tra giao diện headless: cây phần tử, lint bố cục, so ảnh, số lượt render.

## 2. Mục tiêu và ngoài phạm vi

**Mục tiêu**
- G1. Vẽ bằng GPU (D3D11); renderer CPU dùng cùng công thức SDF làm dự phòng.
- G2. Cú pháp Tailwind v4 cho giao diện. Class không hỗ trợ phải lộ ra (lint/log), không bị bỏ qua im lặng.
- G3. Bộ component kiểu shadcn/ui (62 tên trong bảng `docs/components.md` §4) với bàn phím và overlay.
- G4. Soi giao diện headless; mọi thông báo lỗi có đường dẫn phần tử và file bằng chứng.
- G5. Không package bên ngoài; tương thích Native AOT.

**Ngoài phạm vi (hiện tại)**
- Không phải bản sao đầy đủ Tailwind CSS: gradient, 3D, `peer-*`, `z-*`, `fixed`/`sticky`, `m*-auto` chưa có (`docs/tailwind.md` §3).
- Không phải engine HTML/CSS: không DOM, không cascade ngoài utility.
- Không shaping phức tạp, không font fallback (xem §8).
- Không có backend nền tảng nào ngoài Windows trong repo.

## 3. Bề mặt sản phẩm

- **GotX.Core**: engine gồm entity/effect, `App`/`Window`, flexbox và grid, chữ TrueType, biên dịch Tailwind, overlay, hình vector, PNG. Không chứa code OS. Chi tiết: [codebase-summary.md](codebase-summary.md).
- **GotX.Raster**: renderer CPU (`SoftwareRenderer`) và `PngWriter`. Dùng cho headless và làm dự phòng khi không có GPU.
- **GotX.Platform.Windows**: Win32 (DPI, chuột, phím, IME, clipboard) và backend D3D11 (flip-discard, vsync). Không tạo được thiết bị thì thử WARP rồi lùi về CPU. `GOTX_RENDERER=software` ép dùng CPU.
- **GotX.Components**: bộ component shadcn/ui. Không state là hàm tĩnh `Ui.*`; có state là lớp `View` phát event có kiểu. Quy ước: [components.md](components.md).
- **GotX.Test**: khung test/debug headless (harness, truy vấn cây, 13 luật lint, so ảnh, `Explain` pixel, đo hiệu năng). Chi tiết: [testing.md](testing.md).
- **Samples**: `Hello` (demo D3D11, AOT), `Headless` (không cửa sổ, in thống kê cache, xuất PNG), `Components` (gallery Windows, AOT), `ComponentsCheck` (kiểm tra hành vi và ảnh component, sáng/tối).
- **tests/GotX.Tests**: bộ test của repo: tự kiểm khung, tính năng Core, lint và hiệu năng của gallery.
- **tools/StyleGen**: sinh `Styled.g.cs` và `Tw.g.cs` trong `src/GotX.Core/Styling`.

## 4. Yêu cầu chức năng

Mỗi yêu cầu là một điều kiện kiểm được. Nơi kiểm chứng ghi trong ngoặc.

**4.1 Rendering**
- FR-R1. Mọi primitive (nền, viền, bóng, glyph, ảnh, mask vector) đi qua cùng một Scene; renderer GPU và CPU nhận cùng danh sách primitive.
- FR-R2. Bo góc, viền, bóng và outline mịn cả khi phần tử bị xoay/skew. (`docs/tailwind.md` §4)
- FR-R3. `overflow-hidden` cắt theo bo góc và theo hình đã biến đổi. (`docs/tailwind.md` §4)
- FR-R4. Không tạo được thiết bị D3D11 thì ứng dụng chạy bằng CPU, không đổi mã. (`WindowsPlatform`, `GOTX_RENDERER`)
- FR-R5. View không đổi và nhận đúng bounds thì không gọi `Render()` lại. (`tests/GotX.Tests/Self/PerfTests.cs`, `ExpectIdle`)

**4.2 Layout và styling**
- FR-L1. Chuỗi class được biên dịch một lần rồi cache. Token không hỗ trợ tạo `ClassList.Unknown`, gọi `Tailwind.OnUnknownClass` và lint `unknown-class`.
- FR-L2. Thứ tự áp: `!important` > variant trạng thái > class thường; variant `dark:` và breakpoint `sm`…`2xl`, `max-*` được đánh giá đúng. (`samples/Headless/Program.cs`)
- FR-L3. Variant trạng thái (`hover:`, `focus:`, `active:`, `disabled:`, `group-hover:`) chỉ đổi phần vẽ, không đổi layout.
- FR-L4. Flexbox (wrap, gap, justify, items), grid (`grid-cols-N`, span, template `[200px_1fr_auto]`), `absolute`/`relative`, `inset-*`.
- FR-L5. `Theme.Colors` có 22 họ màu × 11 sắc độ. `Theme.SetColor` đặt token ngữ nghĩa có giá trị sáng/tối. (`src/GotX.Core/Styling/Theme.cs`)

**4.3 Input và focus**
- FR-I1. Click trái/phải/giữa, hover vào/ra, kéo với `Phase` Start/Move/End và `FractionX/Y`.
- FR-I2. Tab đi theo thứ tự vẽ. Enter/Space gọi `OnClick`. Vòng `focus-visible` chỉ hiện khi focus do bàn phím. (`tests/GotX.Tests/Core/FocusTests.cs`)
- FR-I3. Phím nổi từ phần tử con lên cha khi `OnKey` trả `false`. Ô nhập được xử lý trước. (`docs/components.md` §3)
- FR-I4. Focus bằng Tab hoặc `FocusKey` tự cuộn phần tử vào tầm nhìn. Vùng cuộn lăn chuột và cuộn mượt.
- FR-I5. Hitbox của phần tử biến đổi nhận chuột qua phép biến đổi nghịch. (`docs/tailwind.md` §4)

**4.4 Text và IME**
- FR-T1. Chữ ngắt theo khoảng trắng; từ dài hơn dòng ngắt theo ký tự; `\n` xuống dòng cứng. (`tests/GotX.Tests/Core/TextStyleTests.cs`)
- FR-T2. `TextInput` nhiều dòng chọn bằng Shift+mũi tên, kéo chuột, Ctrl+A; con trỏ đi theo grapheme. (`docs/components.md` §3)
- FR-T3. Unikey/EVKey hoạt động qua WM_KEYDOWN và WM_CHAR. Chuỗi IME đang soạn (WM_IME_COMPOSITION) được gạch chân trong ô, và cửa sổ gợi ý đặt dưới con trỏ.
- FR-T4. Dấu tổ hợp rời được ghép thành ký tự dựng sẵn bởi `Vietnamese.Compose`.
- FR-T5. Ctrl+C/X/V dùng `app.Clipboard` (CF_UNICODETEXT).

**4.5 Overlay**
- FR-O1. `OpenOverlay` đưa view lên lớp trên cùng; cha không cắt, cuộn hay biến đổi được nó.
- FR-O2. Modal chặn chuột/hover bên dưới và giữ Tab bên trong. Khi đóng, focus trở về nơi lúc mở. (`tests/GotX.Tests/Core/OverlayFocusTests.cs`)
- FR-O3. Overlay neo theo anchor tự lật khi thiếu chỗ và trượt để nằm trong cửa sổ (`Floating.Place`). `AnchorRect` neo vào một điểm.
- FR-O4. Escape và click ra ngoài đóng overlay trên cùng (`DismissOnEscape`, `DismissOnOutsideClick`).

**4.6 Components**
- FR-C1. Component không state là hàm `Ui.<Tên>(w, …)`. Component có state là `View` phát `Notify` và `Emit`.
- FR-C2. Nhóm một điểm dừng Tab (RadioGroup, ToggleGroup, Tabs, Menubar, Calendar) dùng mũi tên. Menu hỗ trợ ↑ ↓ Home End Enter, sub-menu → ←. (`docs/components.md` §4)
- FR-C3. Mỗi nhóm có trang gallery `samples/Components/Pages/<Nhóm>Page.cs` và kiểm tra hành vi `samples/ComponentsCheck/<Nhóm>Checks.cs`.

**4.7 Testing và chẩn đoán**
- FR-D1. `TestSuite` chạy headless với đồng hồ ảo. Lỗi kèm đường dẫn phần tử, hộp `[x,y w×h]`, ảnh khoanh đỏ và 30 sự kiện trace cuối.
- FR-D2. Lint chạy ở scale 1 / 1.25 / 1.5 / 2. Vấn đề mới so với `tests/GotX.Tests/lint-baseline.txt` làm hỏng test; vấn đề cũ thì không.
- FR-D3. So ảnh báo phần tử nằm trong vùng khác. `--update-snapshots` chỉ ghi đè khi chạy có chủ đích.
- FR-D4. `Explain(point)` liệt kê mọi primitive phủ pixel theo thứ tự vẽ và node sở hữu.
- FR-D5. `ExpectRenders`, `ExpectIdle`, `ExpectAllocations`, `ExpectReplayMatchesFresh` là assertion hiệu năng và tính đúng đắn của cache.

## 5. Yêu cầu phi chức năng

| Mã | Yêu cầu | Số liệu và bằng chứng |
|---|---|---|
| NFR-1 | Không package bên ngoài | 0 `PackageReference` thực trong `*.csproj`; ba dòng khớp chỉ nằm trong comment |
| NFR-2 | Native AOT | `IsAotCompatible=true` ở `Directory.Build.props`; `PublishAot=true` ở `samples/Hello` và `samples/Components` |
| NFR-3 | Không cấp phát khi frame không đổi | Test `perf/budgets` đặt `ExpectAllocations(0, frames: 20)`; README ghi "cấp phát: 0 B" cho frame không đổi |
| NFR-4 | Ngân sách thời gian frame | Test dùng 50 ms (trung vị). Ngưỡng rộng, chỉ bắt thoái hoá lớn |
| NFR-5 | Giới hạn cache và bộ nhớ | Class cache 4096 (`Tailwind.MaxCachedClassLists`); glyph atlas 32 MiB (`GlyphAtlas.MaxBytes`); atlas GPU R8 2048×2048, xếp lại khi đầy; mask vector tối đa 2048×2048; 100 000 effect mỗi lượt Flush (`App.MaxEffectsPerFlush`) |
| NFR-6 | Nhịp animation | Khi còn chuyển động, platform hẹn frame khoảng 16 ms (WM_TIMER) |
| NFR-7 | GPU và CPU cho kết quả khớp | Mục tiêu. Shader dùng cùng công thức SDF/erf (`D3D11Renderer.cs`). **Chưa có test tự động so sánh hai renderer** |
| NFR-8 | File dưới 200 dòng | Quy định trong `docs/components.md` §1 (phạm vi `src/GotX.Components`). Ngoại lệ ở Core, Platform, samples: xem §11 |
| NFR-9 | Không phụ thuộc văn hoá | `InvariantGlobalization=true` |
| NFR-10 | Chú thích tiếng Việt, nêu *vì sao* | `docs/components.md` §1 |
| NFR-11 | Chạy headless trên mọi OS | README ghi "mọi OS" cho `Headless` và `ComponentsCheck`. **Chưa kiểm chứng trên Linux/macOS** |

## 6. Nền tảng

- **Hiện có:** Windows (Win32, D3D11, IMM32 qua P/Invoke). Chỉ `src/GotX.Platform.Windows` và các sample dùng nó là code OS.
- **Không có DllImport/LibraryImport** trong `GotX.Core`, `GotX.Raster`, `GotX.Components`, `GotX.Test`.
- **Dự kiến:** tên "X" nghĩa là đa nền tảng. Chưa có backend nào khác, chưa có lộ trình có ngày. Thêm backend cần triển khai `IPlatform` và `IPlatformWindow` (`src/GotX.Core/App/Platform.cs`).
- Phiên bản Windows tối thiểu: chưa xác định.

## 7. Trạng thái hiện tại (tại 2026-11-15)

| Bộ | Kết quả chạy | Ghi chú |
|---|---|---|
| `dotnet build GotX.slnx` | 0 cảnh báo, 0 lỗi | |
| `samples/Headless` (Release) | 109/109 check | cuộn trang 0 B cấp phát mỗi frame; sample không dùng class chưa hỗ trợ |
| `samples/ComponentsCheck` | 464/464 check | không có class Tailwind lạ |
| `tests/GotX.Tests` | 96/96 test | 1 lần trong 14 lượt chạy ra 95/96 mà không lặp lại; chưa xác định được test nào chập chờn |

Số liệu là kết quả chạy thật trên Windows với .NET SDK 11.0.100. Số check lớn hơn số lời gọi `Check(` / `suite.Add(` đếm trong mã
vì một số check nằm trong vòng lặp và helper. Gallery component đã chạy trên cửa sổ D3D11 thật (ảnh chụp khớp renderer CPU)
trước lần đổi tên sang GotX; chưa chạy lại sau đó. Native AOT mới được thử với một harness nhỏ dùng Core + Platform.Windows.
## 8. Giới hạn đã biết

- **Chữ:** không kerning, không font fallback, không shaping (Ả Rập, Ấn Độ). Không đọc TTC/CFF. Chỉ Segoe UI (thường, nghiêng) và Consolas; font khác không nạp theo tên. Kiểu nghiêng không có font thật thì trượt giả.
- **Ô nhập:** không undo, không con trỏ nhấp nháy, không cuộn trong ô, không double-click chọn từ.
- **Focus:** Tab theo thứ tự vẽ, không có `tabindex`; phần tử thường không nhận typeahead.
- **Hình vector và ảnh:** một màu, nét đầu tròn; ảnh chỉ PNG không interlace.
- **Layout:** không `m*-auto`, `align-content`, `col-start`, `border-double`, biến đổi 3D. Thanh cuộn không kéo được. Không có damage region.
- **Component:** không typeahead trong Select/menu, không phím tắt toàn cục, slider một núm, calendar chọn một ngày hoặc một tháng, chart không tooltip, code block không tô màu, avatar không lùi về chữ khi ảnh lỗi, accordion và drawer không có hiệu ứng, drawer không vuốt.
- **Hành vi lệch:** Enter bật/tắt checkbox, switch, radio. Tab rời Popover không modal mà không đóng nó. `OnHover` không phát "vào" khi phần tử hiện ngay dưới con trỏ đứng yên.
- **Bố cục bảng:** mỗi hàng phải lặp cùng `grid-cols-[...]`. RTL (`direction`) không áp dụng.

## 9. Tiêu chí chấp nhận release

- [ ] `dotnet build GotX.slnx` không lỗi.
- [ ] `dotnet run --project tests/GotX.Tests` thoát 0. Số test chạy khớp §7.
- [ ] `samples/Headless` và `samples/ComponentsCheck` thoát 0, không check nào đỏ.
- [ ] Lint không có vấn đề mới so với baseline. Snapshot không lệch ngoài ý muốn.
- [ ] `dotnet publish samples/Hello -r win-x64 -c Release` và `samples/Components` thành công.
- [ ] Không có `PackageReference` mới trong `*.csproj`.
- [ ] Mọi file `src/GotX.Components` dưới 200 dòng.
- [ ] §5 và §7 được cập nhật theo kết quả chạy.

## 10. Quyết định kiến trúc đã áp dụng

Chi tiết trong [system-architecture.md](system-architecture.md).
- COM gọi qua vtable bằng con trỏ hàm, không dùng thư viện binding (`D3D11Interop.cs`).
- Tailwind biên dịch một lần rồi cache. Variant trạng thái áp lúc vẽ trên bản sao `Style` (`docs/tailwind.md` §2).
- Layout không chạy lại khi hover hay cuộn. Cache view lưu kết quả của frame trước.
- Entity và effect: `Notify` xếp hàng và đánh dấu view cùng các cha.
- Lớp lớn tách thành partial theo trách nhiệm (`Window.*.cs`, `LayoutEngine.*.cs`, `Tailwind.*.cs`).

## 11. Câu hỏi mở

1. Test nào trong `tests/GotX.Tests` chập chờn (xem §7)? Nghi là test ngân sách thời gian frame khi máy đang tải nặng; chưa có bằng chứng.
2. Có cần test tự động so sánh D3D11 với CPU không? Ngưỡng sai khác chấp nhận được là bao nhiêu?
3. Quy định dưới 200 dòng áp dụng cho cả repo hay chỉ `GotX.Components`? Hiện vượt ngưỡng: `samples/Headless/Program.cs` 510, `Window.cs` 236, `D3D11Renderer.cs` 234, `TextInput.cs` 221, `Window.Focus.cs` 220, `Win32Window.Messages.cs` 205.
4. Phiên bản Windows tối thiểu là gì?
5. Có backend nền tảng thứ hai trong phạm vi không? Nếu có, ưu tiên nào?
6. Ngưỡng 50 ms trong test hiệu năng có siết lại không?

## 12. Lịch sử thay đổi yêu cầu

- 2026-11-15: tạo tài liệu từ README, `docs/*.md` và mã nguồn hiện có.
