# GotX – Bản đồ codebase

> Cập nhật: 2026-11-15 · Đo từ mã nguồn hiện tại (không tính `bin/`, `obj/`). Không phải git repo.
> Tài liệu liên quan: [README.md](../README.md) · [components.md](components.md) · [tailwind.md](tailwind.md) · [testing.md](testing.md) · project-overview-pdr.md · code-standards.md · system-architecture.md

## 1. Bố cục solution (`GotX.slnx`)

Không có `PackageReference` nào; tất cả chỉ dùng BCL. Đường dẫn tương đối từ gốc repo.

| Project | Vai trò | Tham chiếu | File .cs | Dòng |
|---|---|---|---|---|
| `src/GotX.Core` | Engine UI: App/Window, layout, Tailwind, text, media, scene. Không có code OS | — | 62 | 7 202 |
| `src/GotX.Raster` | Renderer CPU (SDF), PNG writer | Core | 7 | 522 |
| `src/GotX.Platform.Windows` | Win32 (DPI, chuột, phím, IME), backend D3D11, clipboard | Core, Raster | 9 | 1 420 |
| `src/GotX.Components` | Bộ component kiểu shadcn/ui | Core | 88 | 6 890 |
| `src/GotX.Test` | Khung test/debug headless (harness, query, lint, so ảnh, perf) | Core, Raster | 31 | 3 050 |
| `samples/Hello` | Demo cửa sổ Windows, Native AOT | Platform.Windows | 8 | 413 |
| `samples/Headless` | Chạy demo không cửa sổ, xuất PNG; link `../Hello/*View.cs` | Raster | 1 | 510 |
| `samples/Components` | Gallery component (Windows), Native AOT; trang dùng chung với ComponentsCheck | Platform.Windows, Components | 17 | 1 173 |
| `samples/ComponentsCheck` | Kiểm tra component headless (hành vi + ảnh) | Raster, Components | 27 | 3 125 |
| `tests/GotX.Tests` | Bộ test của repo | GotX.Test, Components | 19 | 2 056 |
| `tools/StyleGen` | Sinh `Styled.g.cs`, `Tw.g.cs` | — | 1 | 239 |
| **Tổng (không tính generated)** | | | **270** | **26 600** |

**File sinh tự động** (không sửa tay, xem mục 5):

| File | Dòng | Byte | Sinh bởi |
|---|---|---|---|
| `src/GotX.Core/Styling/Styled.g.cs` | 5 951 | 438 382 | `tools/StyleGen` |
| `src/GotX.Core/Styling/Tw.g.cs` | 252 | 17 532 | `tools/StyleGen` |

## 2. Cấu trúc theo project

### 2.1 `src/GotX.Core` (partial class theo trách nhiệm)

| Thư mục / file | Dòng | Trách nhiệm |
|---|---|---|
| `App/App.cs` | 194 | `App`: `New<T>`, `Notify`/`Emit`, `Observe`/`Subscribe`, hàng đợi effect |
| `App/App.Timers.cs` | 88 | Hẹn giờ (`TimerEntry`) |
| `App/Entity.cs` | 58 | `Entity`, `View`, `Subscription` |
| `App/Platform.cs` | 91 | `IPlatform`, `IPlatformWindow`, `IClipboard`, `MemoryClipboard`, `WindowOptions`, `Application.Run` |
| `App/Animation.cs` | 152 | `Ease`, `TransitionState`, `Keyframes` |
| `App/Window.cs` | 236 | Lớp `Window`: trạng thái frame (cây, layout, dirty, lines) |
| `App/Window.Paint.cs` | 176 | Vòng paint, view stack, `ElementKey` |
| `App/Window.PaintParts.cs` | 150 | Mouse state, backdrop, ring, clip |
| `App/Window.Effects.cs` | 168 | Shadow (`AddShadow*`), `Place`, `SnapToPixels` |
| `App/Window.Cache.cs` | 136 | Replay nội dung view đã cache |
| `App/Window.Views.cs` | 60 | `RenderView`, `MarkViewDirty`, `TryCachedMeasure` |
| `App/Window.Text.cs` | 118 | Vẽ chữ, gạch chân/gạch ngang, run xoay |
| `App/Window.TextInput.cs` | 63 | Vẽ `TextInput` |
| `App/Window.Media.cs` | 78 | Vẽ vector/ảnh, `ContentBox` |
| `App/Window.Input.cs` | 168 | `DispatchInput`, `Scroll`, `Focus(TextInput)` |
| `App/Window.Pointer.cs` | 66 | Kéo thả, nút phụ chuột |
| `App/Window.Focus.cs` | 220 | Focus, phạm vi phím, focus-visible |
| `App/Window.Overlays.cs` | 145 | Stack overlay, hover floor, chặn hover |
| `App/Window.Scroll.cs` | 109 | Trạng thái cuộn, `ScrollInvoker` |
| `App/Window.State.cs` | 51 | Dọn state theo key |
| `App/Window.Diagnostics.cs` | 117 | `Trace`, `LastFrame`, `Inspect` |
| `App/WindowTypes.cs` | 82 | `Hitbox`, `HoverRegion`, `ViewCache`, `ElementState`, `FocusMark`, `ScrollState` |
| `Elements/Div.cs` | 141 | `Div`: handle struct tới node trong cây |
| `Elements/ElementTree.cs` | 118 | Arena theo chunk cố định |
| `Elements/Node.cs` | 87 | `Node`, `NodeKind`, `MeasureMemo` |
| `Elements/InputTypes.cs` | 71 | `InputKind`, `Key`, `Modifiers`, `MouseButton`, `DragPhase`, invoker generic |
| `Layout/LayoutEngine.cs` | 192 | Lõi layout: measure, arrange |
| `Layout/LayoutEngine.Flow.cs` | 161 | Flexbox |
| `Layout/LayoutEngine.Grid.cs` | 172 | Grid (`GridTemplate`, span) |
| `Layout/LayoutEngine.Absolute.cs` | 53 | `absolute`, extent cuộn |
| `Styling/Tailwind.cs` | 119 | Điểm vào: `Tailwind.Get(classes)`, cache `ClassList`, `OnUnknownClass` |
| `Styling/Tailwind.Utilities.cs` | 142 | Dispatch theo tiền tố, utility tĩnh, text |
| `Styling/Tailwind.LayoutUtilities.cs` | 131 | Utility layout (display, flex, grid, position…) |
| `Styling/Tailwind.VisualUtilities.cs` | 135 | Utility visual (màu, border, radius, shadow, transform) |
| `Styling/Tailwind.Values.cs` | 197 | Parse giá trị: spacing, size, width, radius |
| `Styling/Tailwind.Apply.cs` | 136 | Áp `UtilityOp` vào `Style` |
| `Styling/Tailwind.UtilityHelpers.cs` | 51 | Helper: prefix, enum table, space |
| `Styling/ClassList.cs` | 99 | `ClassList`, `UtilityOp`, `Prop`, `StateVariant` |
| `Styling/Style.cs` | 178 | `Style`, `Length`, `IStyled`, enum thuộc tính |
| `Styling/TextStyle.cs` | 51 | `TextStyle` (kế thừa khi layout) |
| `Styling/Theme.cs` | 141 | Token thiết kế (`Theme.Colors`, tương đương `@theme`) |
| `Styling/GridTemplate.cs` | 41 | Track grid |
| `Styling/Styled.g.cs`, `Tw.g.cs` | 5 951 / 252 | Generated (mục 5) |
| `Text/TextSystem.cs` | 137 | Quản lý font, `TryAddFont`, `Atlas` |
| `Text/TextSystem.Wrap.cs` | 102 | Xuống dòng, cache wrap |
| `Text/Font.cs` | 159 | `Font` (đọc bảng glyph), `IOutlineSink` |
| `Text/Font.Outline.cs` | 132 | Outline glyph, composite |
| `Text/GlyphAtlas.cs` | 82 | Atlas glyph theo (font, glyph, size) |
| `Text/GlyphRasterizer.cs` | 110 | `GlyphMask`, rasterize outline |
| `Text/Vietnamese.cs` | 69 | Xử lý dấu tiếng Việt |
| `Media/VectorPath.cs` | 105 | `VectorPath`, `VectorPath.Icon` |
| `Media/PathParser.cs` | 181 | Parse SVG path, `PathFlattener` |
| `Media/PathRasterizer.cs` | 75 | Raster path thành `GlyphMask` |
| `Media/ImageSource.cs` | 64 | `ImageSource` (id, weak registry) |
| `Media/PngDecoder.cs` | 114 | Giải mã PNG |
| `Scene/Scene.cs` | 111 | `Scene`, `Primitive`, `PrimitiveKind` (Quad, Shadow, Glyph, Image) |
| `Overlay/Overlay.cs` | 76 | `OverlayOptions`, `Overlay` |
| `Overlay/Floating.cs` | 67 | `Placement`, `Floating.Place` |
| `Widgets/TextInput.cs` (+`.Geometry`) | 221 + 47 | `TextInput : View`, nhập nhiều dòng |
| `Diagnostics/Diagnostics.cs` | 64 | `TraceKind`, `UiNode`, `UiSnapshot`, `FrameStats` |

### 2.2 `src/GotX.Components`

| Thư mục | File | Ghi chú |
|---|---|---|
| `Core/` | `ComponentTheme.cs`, `Icon.cs`, `IconPaths.cs`, `IconPaths.More.cs`, `Indexed.cs`, `RovingFocus.cs` | Theme, icon 16px, `Indexed<T>` cho handler lặp |
| `Display/` (13) | `Ui.Alert`, `Ui.AspectRatio`, `Ui.Avatar`, `Ui.Badge`, `Ui.Button`, `Ui.ButtonGroup`, `Ui.Card`, `Ui.Empty`, `Ui.Item`, `Ui.Kbd`, `Ui.Separator`, `Ui.Skeleton`, `Ui.Spinner` | Stateless, static `Ui` |
| `Forms/` (17) | `Calendar`, `Checkbox`, `InputOtp`, `NativeSelect`, `RadioGroup`, `Slider`, `Switch`, `TextEntry` (abstract), `Toggle`, `ToggleGroup`, `ToggleStyles`; `Input`, `Textarea`; `Ui.Field`, `Ui.InputGroup`, `Ui.Label`, `Ui.Progress` | Stateful `View` + `Ui.*` |
| `Navigation/` (13) | `Accordion`, `Carousel`, `Chart`, `Collapsible`, `Pagination`, `Resizable`, `Sidebar` (+`Parts`), `Tabs`; `Ui.Breadcrumb`, `Ui.Chart`, `Ui.ScrollArea`, `Ui.Table` | |
| `Overlays/` (27) | `AlertDialog`, `Combobox`, `Command` (+`Render`), `ContextMenu`, `Dialog`, `Drawer`, `DropdownMenu`, `HoverCard`, `MenuItem`, `MenuOwner`, `MenuPanel` (+`Sub`), `Menubar`, `NavigationMenu` (+`Render`), `Popover`, `PopupSlot`, `Roving`, `Select`, `Sheet`, `SidePanel`, `Toast`, `Toaster`, `Tooltip`; `OverlayIcons`, `OverlayParts` | Popup, menu, dialog, toast |
| `Chat/` (12) | `Attachment`, `CodeBlock`, `MessageScroller`, `Questionnaire` (+`Render`), `Trace`; `ChatIcons`; `Ui.Attachment`, `Ui.Bubble`, `Ui.Marker`, `Ui.Message`, `Ui.Trace` | Thành phần khung chat |

### 2.3 `src/GotX.Raster`, `src/GotX.Platform.Windows`

| File | Trách nhiệm |
|---|---|
| `Raster/SoftwareRenderer.cs` (133) | Vòng vẽ CPU, `Rect` nội bộ |
| `Raster/SoftwareRenderer.{Quad,Shadow,Glyph,Image}.cs` | Vẽ từng loại primitive |
| `Raster/Canvas.cs`, `Raster/PngWriter.cs` | `Canvas` (sealed class); ghi PNG |
| `Platform.Windows/WindowsPlatform.cs` | `IPlatform`; biến `GOTX_RENDERER=software` ép CPU |
| `Platform.Windows/Win32Window.cs`, `Win32Window.Messages.cs` | Cửa sổ Win32, xử lý WM_* |
| `Platform.Windows/Win32.cs` | Khai báo Win32 (137 dòng) |
| `Platform.Windows/D3D11Interop.cs` | Gọi COM qua vtable con trỏ hàm |
| `Platform.Windows/D3D11Renderer.cs`, `.Resources.cs`, `.Shader.cs` | Renderer D3D11, tài nguyên, HLSL |

### 2.4 `src/GotX.Test` (khung test)

| Khu vực | File | Trách nhiệm |
|---|---|---|
| Harness | `UiTest.cs`, `UiTest.Input.cs`, `UiTest.Visual.cs`, `UiTest.Failures.cs` | `UiTest`: dựng/chạy view headless, input, ảnh, báo lỗi |
| Cây + truy vấn | `UiTree.cs`, `UiTree.Path.cs`, `TreeDump.cs` | `UiTree`: query, đường dẫn node |
| Assertion | `UiAssert.cs`, `UiAssert.Layout.cs` | `UiAssert` |
| Lint | `Lint/UiLint.cs`, `Rules.{Geometry,Overflow,Siblings,Hit,Contrast}.cs`, `LintIssue.cs`, `LintBaseline.cs` | Luật bố cục/độ tương phản; so baseline |
| Visual | `Visual/ImageDiff.cs`, `PixelExplainer.cs`, `Annotator.cs`, `Pixels.cs` | So ảnh, giải thích pixel, khoanh vùng |
| Perf | `UiPerf.cs`, `UiPerf.Cache.cs` | Đo hiệu năng, thống kê cache |
| Runner | `Runner/TestSuite.cs`, `Runner/TestReport.cs` | `TestSuite.Run(args)`, báo cáo |
| Doctor | `UiDoctor.cs` | `UiDoctor` (chưa đọc chi tiết) |
| Khác | `Failure.cs`, `Fmt.cs`, `JsonWriter.cs`, `TestFonts.cs`, `TraceLog.cs` | Tiện ích |

### 2.5 `tests/GotX.Tests`, `samples/*`, `tools/StyleGen`

| Đường dẫn | Nội dung |
|---|---|
| `tests/GotX.Tests/Program.cs` | Điểm vào; đăng ký nhóm theo thứ tự: Self → Core → Gallery |
| `tests/GotX.Tests/Self/` (8) | Tự kiểm khung: `AssertTests`, `HarnessTests`, `LintContentTests`, `LintGeometryTests`, `PerfTests`, `RunnerTests`, `VisualTests`, `SelfTestKit` (`Kit.Probe`) |
| `tests/GotX.Tests/Core/` (8) | Tính năng Core: `Diagnostics`, `Focus`, `Image`, `OverlayFocus`, `StateAndScroll`, `TextStyle`, `Timer`, `Vector` |
| `tests/GotX.Tests/Gallery/GalleryTests.cs`, `Fixtures/Fixtures.cs` | Lint + perf gallery; dữ liệu mẫu |
| `tests/GotX.Tests/lint-baseline.txt`, `__snapshots__/boxes.png` | Baseline lint; ảnh snapshot |
| `samples/Hello/` | `Program.cs` + `CounterView`, `DemoView`, `EffectsView`, `GalleryView`, `InboxView`, `ShowcaseView`, `WidgetsView` |
| `samples/Headless/Program.cs` | Args: `[outDir] [scale]`; mô phỏng input, in thống kê cache, xuất PNG |
| `samples/Components/` | `Program.cs`, `GalleryView.cs`, `Demo.cs` (`Demo.Page/Section/Row`), `Pages/{Chat,Display,Forms,Navigation,Overlays}` + `*Page.cs` |
| `samples/ComponentsCheck/` | `Program.cs`, `Harness.cs` (`CheckHarness`), `*Checks.cs` (hành vi), `*Probe*.cs` |
| `tools/StyleGen/Program.cs` | Sinh `Styled.g.cs` và `Tw.g.cs` |

## 3. Type chính và vị trí

| Type | File | Ý nghĩa |
|---|---|---|
| `App` | `src/GotX.Core/App/App.cs` | Trạng thái ứng dụng, tạo entity, hàng đợi effect, timer |
| `Entity` / `View` | `src/GotX.Core/App/Entity.cs` | Base có Id + Notify/Emit; `View` là component render được |
| `Window` | `src/GotX.Core/App/Window.cs` (+ partial) | Dựng frame, paint, hit-test, focus, input |
| `Div` | `src/GotX.Core/Elements/Div.cs` | Handle struct tới node; `.Class(...)`, `.Child(...)`, `.OnClick(...)` |
| `Style` / `TextStyle` | `src/GotX.Core/Styling/Style.cs`, `TextStyle.cs` | Thuộc tính đã resolve; `TextStyle` là kế thừa chữ |
| `ClassList` | `src/GotX.Core/Styling/ClassList.cs` | Chuỗi class đã biên dịch thành danh sách `UtilityOp` |
| `Tailwind` | `src/GotX.Core/Styling/Tailwind.cs` | Biên dịch class string, cache, callback class lạ |
| `Theme` | `src/GotX.Core/Styling/Theme.cs` | Token màu/kích thước |
| `Styled` / `Tw` | `Styling/Styled.g.cs`, `Tw.g.cs` | Extension utility (generated); bảng màu `Tw.*` |
| `Scene` / `Primitive` | `src/GotX.Core/Scene/Scene.cs` | Danh sách primitive gửi cho renderer |
| `TextSystem` | `src/GotX.Core/Text/TextSystem.cs` | Font registry, wrap, atlas |
| `GlyphAtlas` | `src/GotX.Core/Text/GlyphAtlas.cs` | Cache mask glyph |
| `Font` | `src/GotX.Core/Text/Font.cs` | Font TrueType/outline |
| `VectorPath` | `src/GotX.Core/Media/VectorPath.cs` | Path SVG-like, icon |
| `ImageSource` | `src/GotX.Core/Media/ImageSource.cs` | Ảnh PNG giải mã, id ổn định |
| `OverlayOptions` / `Overlay` | `src/GotX.Core/Overlay/Overlay.cs` | Overlay: modal, backdrop, dismiss |
| `Floating` | `src/GotX.Core/Overlay/Floating.cs` | Đặt popup cạnh anchor (`Placement`) |
| `TextInput` | `src/GotX.Core/Widgets/TextInput.cs` | Ô nhập liệu |
| `IPlatform` / `IPlatformWindow` | `src/GotX.Core/App/Platform.cs` | Hợp đồng backend OS; `Application.Run` |
| `WindowsPlatform` | `src/GotX.Platform.Windows/WindowsPlatform.cs` | Backend Windows (D3D11 hoặc CPU) |
| `ComponentTheme` | `src/GotX.Components/Core/ComponentTheme.cs` | Theme component; `Install()` |
| `Ui` | `src/GotX.Components/*/Ui.*.cs` | Factory static cho component không state |
| `Icon` | `src/GotX.Components/Core/Icon.cs` | Icon 16px, đường vẽ từ `IconPaths*.cs` |
| `UiTest` | `src/GotX.Test/UiTest.cs` | Harness test headless |
| `TestSuite` | `src/GotX.Test/Runner/TestSuite.cs` | Chạy/lọc test, `--list`, `--out` |
| `UiLint` | `src/GotX.Test/Lint/UiLint.cs` | Chạy luật lint |
| `UiDoctor` | `src/GotX.Test/UiDoctor.cs` | Chẩn đoán |

## 4. Tìm nhanh: "muốn đổi X thì sửa file nào?"

| Việc | Sửa ở |
|---|---|
| Thêm utility Tailwind (vd `ring-2`) | `Styling/Tailwind.{Utilities,LayoutUtilities,VisualUtilities,Values}.cs`, rồi `Tailwind.Apply.cs`; chạy StyleGen (mục 5) nếu cần extension |
| Thêm thuộc tính style mới | `Styling/Style.cs` (field) → `ClassList.cs` (`Prop`) → `Tailwind.Apply.cs` |
| Thêm màu/token | `Styling/Theme.cs`, `Tw` (generated) |
| Layout flex / grid / absolute | `Layout/LayoutEngine.Flow.cs` / `.Grid.cs` / `.Absolute.cs` |
| Xuống dòng, đo chữ | `Text/TextSystem.Wrap.cs`, `Text/TextSystem.cs` |
| Font, glyph, dấu tiếng Việt | `Text/Font*.cs`, `Text/GlyphRasterizer.cs`, `Text/Vietnamese.cs` |
| Shadow / ring / clip | `App/Window.Effects.cs`, `App/Window.PaintParts.cs`; CPU: `Raster/SoftwareRenderer.Shadow.cs`; GPU: `D3D11Renderer.Shader.cs` |
| Chuột, kéo thả | `App/Window.Pointer.cs`, `Elements/InputTypes.cs` |
| Phím, focus | `App/Window.Input.cs`, `App/Window.Focus.cs` |
| Overlay: vị trí, stack | `Overlay/Floating.cs`, `App/Window.Overlays.cs` |
| Message Win32 (DPI, IME) | `Platform.Windows/Win32Window.Messages.cs` |
| Đổi backend render GPU | `Platform.Windows/D3D11Renderer*.cs` |
| Thêm component không state | `src/GotX.Components/<Nhóm>/Ui.<Tên>.cs` (xem [components.md](components.md)) |
| Thêm component có state | `src/GotX.Components/<Nhóm>/<Tên>.cs`, kế thừa `View` |
| Thêm icon | `Components/Core/IconPaths.cs` (hoặc `.More.cs`), dùng qua `Icon.cs` |
| Thêm luật lint | `src/GotX.Test/Lint/Rules.*.cs`, đăng ký trong `UiLint.cs`; baseline `tests/GotX.Tests/lint-baseline.txt` |
| Thêm test | `tests/GotX.Tests/Core/*.cs`, đăng ký trong `tests/GotX.Tests/Program.cs` |
| Thêm trang gallery | `samples/Components/Pages/<Nhóm>/` + `samples/ComponentsCheck/<Nhóm>Checks.cs` |

## 5. Code sinh tự động và build/run/test

**Generated:** `Styled.g.cs` và `Tw.g.cs` (header `auto-generated … Đừng sửa tay`). Sinh bởi `tools/StyleGen/Program.cs`:

```
dotnet run --project tools/StyleGen -- src/GotX.Core/Styling
```

Lệnh ghi đè trực tiếp hai file trên. Sau khi sửa generator, chạy lại và kiểm tra `git diff`/so sánh trước khi commit. Chưa xác minh được output có khớp byte-for-byte với file hiện tại (chưa chạy lệnh).

**Build / run / test** (khớp `README.md` mục chạy; các project và tham số đã đối chiếu với `Program.cs` và `csproj`):

| Lệnh | Ghi chú |
|---|---|
| `dotnet build GotX.slnx` | Build tất cả |
| `dotnet run --project samples/Hello` | Windows, D3D11; `GOTX_RENDERER=software` ép CPU |
| `dotnet run --project samples/Headless -- out 2` | Mọi OS; args: `outDir`, `scale` |
| `dotnet run --project samples/Components` | Gallery, Windows |
| `dotnet run --project samples/ComponentsCheck -- out` | Mã thoát `2` nếu có lỗi hoặc class Tailwind chưa hỗ trợ |
| `dotnet run --project tests/GotX.Tests -- [--filter s] [--out d] [--update-snapshots] [--update-baseline] [--list] [--verbose]` | Bộ test của repo |
| `dotnet publish samples/Hello -r win-x64 -c Release` | Native AOT (`PublishAot=true`) |

`Directory.Build.props`: net11.0, `Nullable`, `ImplicitUsings`, `AllowUnsafeBlocks`, `IsAotCompatible`, `InvariantGlobalization`. Xem [code-standards.md](code-standards.md) cho quy ước.

`repomix-output.xml` nằm ở gốc repo, bị `.gitignore` bỏ qua (dòng 34); `bin/`, `obj/` cũng bị bỏ qua.

## 6. Kiểm tra tài liệu hiện có

**Đã đối chiếu (khớp):** đường dẫn trong `README.md` (mục cấu trúc, partial list, lệnh chạy); `docs/components.md` (`Display/Ui.Button.cs`, `Forms/Checkbox.cs`, `Overlays/Select.cs`, `Overlays/Dialog.cs`, `Core/Indexed.cs`, `CheckHarness`, `Demo.Page/Section/Row`, `CoreChecks.cs`); `docs/testing.md` (các thư mục/file liệt kê, `Kit.Probe`, `lint-baseline.txt`, `Window.Trace/LastFrame/Inspect`).

**Lệch / thiếu:**
- `docs/testing.md` dòng 10: cú pháp không liệt kê `--update-snapshots`, `--update-baseline` (có trong `tests/GotX.Tests/Program.cs`).
- `docs/components.md` ghi `Core/Icon.cs`, `Core/Indexed.cs` không kèm `src/GotX.Components/`; đường dẫn thực là `src/GotX.Components/Core/…`.
- `docs/components.md` nói component không state luôn là `Ui.<Tên>.cs`; `Forms/Input.cs`, `Forms/Textarea.cs`, `Overlays/Drawer.cs`, `Overlays/Sheet.cs`, `Chat/ChatIcons.cs` không theo mẫu này (chưa đọc hết để kết luận).

**File có vẻ sai chỗ / trùng / dư (chưa xác minh tham chiếu chết):**
- `src/GotX.Components/Core/ComponentTheme.cs` chứa thêm `FragmentView : View` (dòng 59) — không khớp tên file.
- `src/GotX.Core/Media/PathParser.cs` chứa thêm `PathFlattener` (dòng 6).
- `src/GotX.Core/Text/GlyphRasterizer.cs` chứa `GlyphMask` (dùng bởi `Media/PathRasterizer.cs`).
- `src/GotX.Components/Chat/ChatIcons.cs` và `Overlays/OverlayIcons.cs` là lớp bọc quanh `Icon`; cần xem có trùng hay không.
- `src/GotX.Core/Diagnostics/` chỉ có một file; có thể gộp.
- `src/GotX.Components/Forms/Input.cs` (21 dòng), `Textarea.cs` (19 dòng): mỏng, kiểm tra có thể gộp vào `TextEntry`.
- Chưa phân tích code chết (không quét tham chiếu toàn repo).
