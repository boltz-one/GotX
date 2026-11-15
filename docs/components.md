# GotX.Components – quy ước viết component

`src/GotX.Components` là bộ component kiểu shadcn/ui dựng trên `GotX.Core`. Tài liệu này mô tả cách một component
được viết, những gì Core cung cấp, và những gì Core **chưa** có (để biết khi nào phải đơn giản hoá).

## 1. Hai loại component

**Không có state → hàm tĩnh trong `Ui`** (`public static partial class Ui`, mỗi component một file `Ui.<Tên>.cs`).
Hàm nhận `Window w`, trả về `Div` đã có class; người gọi nối thêm `.Child(...)`, `.OnClick(...)`, `.Class(...)`.
Mẫu: `Display/Ui.Button.cs`.

```csharp
Ui.Button(w, "Lưu", ButtonVariant.Outline, ButtonSize.Sm).OnClick(this, static (v, _) => v.Save())
```

**Có state → lớp kế thừa `View`** (một file `<Tên>.cs`). Tạo bằng `App.New(new X { ... })`, nhúng bằng `w.View(x)`,
báo thay đổi bằng event có kiểu. Mẫu: `Forms/Checkbox.cs`, `Overlays/Select.cs`, `Overlays/Dialog.cs`.

```csharp
public sealed class Checkbox : View
{
    public readonly record struct CheckedChanged(bool Checked);
    public bool Checked { get => _checked; set { if (_checked == value) return; _checked = value; Notify(); Emit(new CheckedChanged(value)); } }
    public override Div Render(Window w) => w.Div().Class(...).OnClick(this, static (c, _) => c.Toggle());
}
// nơi dùng:
_agree = App.New(new Checkbox { Label = "Đồng ý" });
App.Subscribe<Checkbox.CheckedChanged>(_agree, e => ...);
```

Quy tắc chung:
- `Notify()` sau mỗi lần đổi state (view sẽ render lại ở frame sau); `Emit(...)` để báo ra ngoài.
- Setter gọi được trước khi entity được gắn vào app (`Notify`/`Emit` khi đó không làm gì; entity con dùng `?.`).
- Entity con tạo trong `protected override void OnCreated()` bằng `App.New(...)`.
- Trạng thái (đang chọn, đang mở…) chọn class bằng điều kiện C#, vì không có variant `data-[state=...]`:
  `w.Div().Class(_on ? "bg-primary ..." : "bg-input ...")`.
- Handler dùng lambda `static` + state: `.OnClick(this, static (v, e) => v.Foo())`. Phần tử trong danh sách cần chỉ số
  thì dùng `Indexed<T>` (`Core/Indexed.cs`): `.OnClick(refs[i], static (r, _) => r.Owner.Pick(r.Index))`.
- Handler cần `Window` (mở overlay): lưu `_window = w` trong `Render`.
- Tránh nối chuỗi class mỗi frame ở phần tử lặp nhiều; dùng hằng chuỗi hoặc bảng dựng sẵn (xem `Ui.Button.cs`).
- Nội dung do người dùng cung cấp: thuộc tính `Func<Window, Div>?` (xem `Dialog.Body`).
- Mỗi file dưới 200 dòng; chú thích tiếng Việt, nói *vì sao* chứ không kể lại code. Không thêm package.

## 2. Viết giao diện

- `w.Div()` tạo phần tử; `.Class("...")` nhận class Tailwind v4 (bảng hỗ trợ: `docs/tailwind.md`); `.Child(Div)` /
  `.Child("chữ")` thêm con; `.Key("tên")` đặt khoá ổn định (bắt buộc cho vùng cuộn/transition trong danh sách động,
  và để kiểm tra headless tìm được phần tử).
- Phần tử mặc định là block: các con xếp dọc và giãn hết chiều ngang. Muốn xếp ngang dùng `flex`.
- Token màu của shadcn dùng trực tiếp và tự đổi theo dark mode: `bg-background text-foreground bg-card bg-popover
  bg-primary text-primary-foreground bg-secondary bg-muted text-muted-foreground bg-accent text-accent-foreground
  text-destructive border-border border-input ring-ring bg-sidebar ... chart-1..5`. Độ mờ: `bg-primary/80`.
- **Viền mặc định lấy màu chữ**, không phải `border-border` như shadcn: luôn viết `border border-border` (hoặc `border-input`).
- Bo góc theo shadcn: `rounded-sm` 6, `rounded-md` 8, `rounded-lg` 10, `rounded-xl` 14.
- Variant trạng thái có: `hover: active: focus: disabled: group-hover: dark: sm: md: lg:`. Chúng chỉ đổi phần vẽ
  (màu, bóng, transform), **không đổi layout**. `disabled:` bật bằng `.Disabled(true)` (đồng thời bỏ qua click).
- Icon: `Icon.Check / X / Plus / Minus / ChevronDown|Up|Left|Right / Dot / Circle / MoreHorizontal / Search / Spinner`
  (`Core/Icon.cs`), mỗi cái là một ô 16px lấy màu chữ của cha. Cần icon khác thì ghép từ viền/hộp theo cùng cách.

## 3. Sự kiện và overlay (API của Core)

| Việc | API |
|---|---|
| Click trái | `.OnClick(state, static (s, e) => ...)` |
| Click phải / giữa | `.OnAuxClick(state, static (s, e) => ...)` – `e.Button`, `e.Position` |
| Chuột vào / ra | `.OnHover(state, static (s, inside) => ...)` |
| Kéo chuột | `.OnDrag(state, static (s, e) => ...)` – `e.Phase` (Start/Move/End), `e.FractionX/Y` (0..1 trong phần tử), `e.Local`, `e.Size` |
| Ô nhập chữ | `GotX.Widgets.TextInput` (View): `Text`, `Placeholder`, `Multiline`, `BoxClass`, `MinHeight`; event `TextChanged`, `Submitted` (Enter khi `Multiline = false`); `window.Focus(input)` |
| Mở lớp trên cùng | `window.OpenOverlay(view, new OverlayOptions { ... })` → `Overlay` (`Close()`, `IsOpen`, `Closed`, `ContentBounds`) |
| Modal | `Modal = true` (backdrop `BackdropClass`, chặn chuột/hover bên dưới), bố cục nội dung bằng `LayerClass` (`"flex justify-end"` cho drawer phải, `"flex items-end"` cho sheet đáy) |
| Neo vào phần tử | đặt `.Anchor("tên-duy-nhất")` lên nút mở, rồi `Anchor = "tên"`, `Placement = Placement.BottomStart`, `Offset`, `MatchAnchorWidth` |
| Neo vào một điểm | `AnchorRect = new Bounds(x, y, 0, 0)` (context menu tại vị trí chuột) |
| Đóng | Escape và bấm ra ngoài đóng overlay trên cùng (`DismissOnEscape`, `DismissOnOutsideClick`); bấm lại chính anchor thì không tự đóng, handler của nút tự xử lý |
| Vị trí phần tử trên màn hình | `window.AnchorBounds("tên")`, `window.FocusedBounds` – gọi trong `Render` thì nhận hình học của frame trước (frame đang dựng chưa xong) |
| Cuộn bằng code | `window.ScrollTo("key", x, y)`, `window.ScrollToEnd("key")` – vùng cuộn gọi tên bằng `.Key(...)` của nó |
| Cuộn tới phần tử | `window.ScrollIntoView("tên-anchor")` – cuộn các vùng cuộn bao ngoài vừa đủ để phần tử có `.Anchor(...)` hiện ra (mục đang chọn bằng phím trong danh sách dài). Focus bằng Tab / `FocusKey` tự làm việc này |
| Hiện thanh cuộn | Lớp mở rộng của GotX trên phần tử `overflow-*-auto`: `scrollbar-hover` (chỉ hiện khi con trỏ ở trên vùng cuộn, ẩn 600ms sau khi rời – như ScrollArea `type="hover"` của shadcn), `scrollbar-scroll` (hiện khi đang cuộn), `scrollbar-none`, `scrollbar-always` (mặc định); `scrollbar-w-2.5` bề rộng rãnh, `scrollbar-thumb-border` màu con trượt. `Ui.ScrollArea` dùng sẵn bộ này |
| Theo dõi cuộn | `.OnScroll(state, static (s, info) => ...)` – `info.Offset`, `info.Max`, `info.RemainingY`; hoặc `window.GetScroll("key")` |
| Dark mode | `window.DarkMode` |
| Focus bàn phím | `.Focusable()` – điểm dừng của Tab (thứ tự vẽ), bấm chuột cũng focus; Enter/Space gọi `OnClick`. Variant `focus:` (mọi focus) và `focus-visible:` (focus do bàn phím). Trong danh sách động đặt thêm `.Key(...)` |
| Nhận phím | `.OnKey(state, static (s, e) => bool)` – `e.Key`, `e.Shift/Control/Alt`. Nhận phím khi focus ở phần tử này **hoặc phần tử con** (kể cả ô nhập không dùng phím đó, ví dụ ↑/↓ trong ô một dòng); trả `true` = đã xử lý, `false` = nổi tiếp lên cha rồi tới Tab/Escape mặc định |
| Tự focus | `.Focusable().AutoFocus()` – nhận focus ở frame đầu tiên xuất hiện (nội dung menu/hộp thoại); khi overlay đóng, focus trở về nơi cũ |
| Focus bằng code | `window.FocusKey("key")` (thêm `visible: false` để focus mà không bật `focus-visible:`, ví dụ focus đi theo chuột; gọi trong `Render` hoặc trước khi phần tử được vẽ thì Core thực hiện ở cuối frame), `window.Focus(textInput)`, `window.Blur()`, `window.MoveFocus(backward)`, `window.IsFocused("key")` |
| Hẹn giờ | `App.SetTimeout(giây, action)`, `App.SetInterval(giây, action)` → `IDisposable` để huỷ; callback chạy trên main thread. Headless/test: gán `app.Clock` rồi gọi `app.RunTimers()` |
| Icon / hình vector | `static readonly VectorPath X = VectorPath.Icon("M18 6 6 18", "m6 6 12 12")` (cú pháp `d` của SVG, hoặc dạng tắt `"circle cx cy r"`, `"line x1 y1 x2 y2"`, `"rect x y w h rx"`, `"polyline …"`); `VectorPath.FilledIcon`, `Fill`, `Stroke`, `Polyline` (đường biểu đồ). Vẽ: `w.Path(X).Class("size-4")` – lấy màu chữ; `object-fill` để kéo giãn |
| Ảnh | `ImageSource.FromPngFile(path)` / `FromPng(bytes)` / `FromRgba(w, h, bytes)`; vẽ: `w.Image(img).Class("size-10 rounded-full object-cover")` (`object-contain`, `object-fill`) |
| Kiểu chữ | `font-mono` / `font-sans`, `italic`, `underline`, `line-through`, `uppercase` / `lowercase` / `capitalize`, `tracking-tight…widest` |

Tên anchor phải duy nhất cho từng instance: `_anchor ??= "gotx-<loại>-" + Id.Value` (xem `Select.cs`).

Helper dùng chung của Components (`src/GotX.Components/Core/`):

| Việc | Helper |
|---|---|
| Nhóm một-điểm-dừng Tab (radio, toggle group, lưới ngày) | `new RovingFocus(prefix)`: `Key(i)`, `Sync(w, count, home)` trong `Render`, `Focus(w, i, visible)`, `LeaveOnTab` |
| Mục kế tiếp theo phím trong menu / danh sách / tab tự giữ key | `RovingFocus.FromKey / Step / Edge` (bỏ qua mục tắt, `NotNavigation` khi không phải phím di chuyển), `RovingFocus.Names` (bảng key/anchor dựng một lần), `FocusedIndex`, `TabOut` |
| Vòng focus | `FocusRing.Ring` (phần tử có viền), `FocusRing.RingOnly`; `FocusRing.Within(w, anchor)` = focus bàn phím đang ở trong anchor |
| Trễ mở/đóng huỷ được | `Delay` (`Start(App, giây, action)`, `Cancel`, `Pending`) |
| Chạy một việc ngay sau frame đang dựng (đo anchor, mở/đóng overlay theo focus) | `AfterFrame.Create(App, action)` trong `OnCreated`, `Request()` trong `Render` – không dùng `SetTimeout(0)` |
| Chỉ số cho handler `static` của phần tử lặp | `Indexed<T>` |
| View từ một hàm dựng | `FragmentView` |
| Icon | `Icon.*` là bộ duy nhất (path ở `IconPaths*.cs`); không tạo lớp icon riêng theo nhóm |

## 4. Danh sách component

| Nhóm (thư mục) | Component |
|---|---|
| `Display/` | Button, Badge, Alert, Avatar (+ group, badge), Card, Empty, Item, Kbd, Separator, Skeleton, Spinner, ButtonGroup, AspectRatio, Typography |
| `Forms/` | Checkbox, Input, Textarea, Label, Field, InputGroup, InputOtp, RadioGroup, Switch, Slider, Toggle, ToggleGroup, NativeSelect, Progress, Calendar, DatePicker |
| `Overlays/` | Dialog, AlertDialog, Sheet, Drawer, Popover, Tooltip, HoverCard, Select, Combobox, Command, DropdownMenu, ContextMenu, Menubar, NavigationMenu, Toast/Toaster |
| `Navigation/` | Accordion, Collapsible, Tabs, Breadcrumb, Pagination, Table, ScrollArea, Resizable, Carousel, BarChart + Chart (cột/đường/vùng), Sidebar, DataTable, SidebarNav |
| `Chat/` | Bubble, Message, MessageScroller, Attachment, Marker, CodeBlock, Trace, Questionnaire |

**Typography** (`Display/Ui.Typography.cs`) – `Ui.H1…H4, P, Lead, Large, Small, Muted, Blockquote, InlineCode, List`, đặt trong `Ui.Prose`
(khoảng cách 24px thay cho `mt-6` của từng khối). Chưa có `text-balance`, `list-disc` (chấm là hộp tròn).

**DatePicker** (`Forms/DatePicker.cs`) – nút + `Calendar` trong overlay; `Value` (`DateOnly?`), `ValueChanged`, `Format`, `Placeholder`.
Enter/Space/↓ mở, chọn ngày thì đóng và focus về nút, Escape đóng. Chọn một ngày; bấm lại ngày đang chọn không đóng lịch.

**DataTable&lt;T&gt;** (`Navigation/DataTable*.cs`, `DataColumn.cs`) – cột có kiểu (`Width` là track px/fr, `Text`/`Cell`, `Compare`), bấm tiêu đề
để sắp xếp, chọn hàng (ô tiêu đề chọn cả trang, có trạng thái một phần), lọc chữ theo `FilterColumn`, phân trang, menu ẩn/hiện cột,
`RowActions`, "No results.". Event `SelectionChanged / SortChanged / PageChanged / FilterChanged / ColumnVisibilityChanged / RowAction`.
Hàng nhận diện theo chỉ số trong `Items` nên focus và lựa chọn giữ nguyên khi sắp xếp/lọc/đổi trang. Chưa có kéo thả, ghim/đổi bề rộng cột.

**SidebarNav** (`Navigation/SidebarNav*.cs`) – phần menu của sidebar shadcn: `SidebarNavGroup` → `SidebarNavItem` (icon, `Badge`, `SubItems`
gập được hoặc luôn mở, nút phụ có `Menu`). `Collapsed` còn cột icon; khung dựng bằng `Wrapper / Frame / Header / Content / Footer / Inset /
Trigger`. Bàn phím: Tab tới từng nút, Enter/Space, →/← mở/đóng. Chưa có tooltip khi thu gọn, rail, Ctrl+B, animation.

Bàn phím (theo shadcn / Base UI): mọi nút, toggle, checkbox, switch, tab, accordion, link phân trang… là điểm dừng của Tab
và có vòng focus `focus-visible`. Nhóm một-điểm-dừng dùng mũi tên: RadioGroup, ToggleGroup, Tabs, Menubar, lưới ngày của
Calendar, câu hỏi một lựa chọn (`RovingFocus` trong `Core/`). Menu và danh sách (DropdownMenu, ContextMenu, Menubar, Select,
Combobox, Command, NavigationMenu, NativeSelect – lớp mỏng trên Select): ↑/↓/Home/End/Enter, sub-menu →/←, focus trở về nút mở khi đóng. Dialog/Sheet/Drawer
giữ Tab bên trong. Slider, Resizable, Carousel, CodeBlock nhận mũi tên khi được focus.

Còn khác bản gốc:
- **Chưa có:** typeahead trong Select/menu (phần tử thường không nhận ký tự gõ); phím tắt toàn cục (Ctrl+B của Sidebar);
  Home/End trong danh sách của Command/Combobox (ô nhập dùng mất); "grace polygon" của sub-menu (chỉ có trễ 100 ms).
- **Đơn giản hoá:** slider một núm; calendar chọn một ngày, một tháng; chart có cột/đường/vùng nhưng không có tooltip và
  điểm dữ liệu; code block không tô màu cú pháp; avatar không tự lùi về chữ viết tắt khi ảnh lỗi (truyền `null`);
  accordion/trace mở đóng không có hiệu ứng chiều cao; drawer không vuốt để đóng.
- **Khác hành vi:** Enter cũng bật/tắt checkbox, switch, radio (Core coi Enter là click); focus khi mở Dialog nằm trên
  khung hộp thoại chứ không phải phần tử focus được đầu tiên; Tab rời được Popover không modal mà không đóng nó;
  toast hiện ngay dưới con trỏ đứng yên chỉ tạm dừng sau khi chuột rời ra rồi vào lại; lịch: bấm chuột vào ngày của tháng
  bên cạnh thì lịch lật tháng và focus đi theo ngày đó (không hiện vòng focus).
- **Khác class bản gốc (có chủ ý):** Trace – mô tả bước dùng `text-muted-foreground` (gốc `/70`, tương phản 2.7:1), bước
  chờ `opacity-80` (gốc 60), tiêu đề đang chạy `text-foreground animate-pulse` thay cho shimmer; Pagination – hàng nút tự
  xuống dòng khi hẹp, chữ Trước/Sau ẩn dưới 640px (`hidden sm:block` như gốc); Tabs – tab rộng theo nhãn (`grow`) thay vì
  chia đều (`flex-1`); phần phụ một dòng của InputGroup bỏ `py-1.5` (nút 24px + đệm vượt khung 32px); nhãn bấm được và
  Switch cỡ `sm` có thêm `-my-px py-px` để vùng bấm đủ 16px.
- **Bố cục:** bảng dùng grid nên mỗi hàng phải lặp cùng `grid-cols-[...]`; `direction` (RTL) không áp dụng;
  trigger tuỳ biến của DropdownMenu/Popover nên là `Div` có class nút (`Ui.ButtonClasses`), không phải `Ui.Button`
  (nếu không sẽ thành hai điểm dừng Tab).
## 5. Core chưa có – phải đơn giản hoá

- Hình vector chỉ một màu (màu chữ), nét đầu tròn/khớp tròn; không có gradient hay nhiều màu trong một path.
- Ảnh: chỉ giải mã PNG (không interlace). JPEG/WebP phải tự giải mã rồi dùng `ImageSource.FromRgba`.
- Font: Segoe UI (thường/nghiêng) và Consolas trên Windows; không nạp font theo tên, không có `tracking-[…]` tuỳ ý.
- Tab đi theo thứ tự vẽ (không có `tabindex`); phần tử thường không nhận ký tự gõ (không làm được typeahead).
- Không có `z-*`, `fixed`, `sticky`: thứ tự vẽ là thứ tự trong cây, con `absolute` nằm trên anh em; muốn nổi lên trên cùng thì dùng overlay.
- Không có `m*-auto`: dùng `justify-*`, `items-*` hoặc một phần tử đệm `flex-1`.
- Không có gradient, blur, `backdrop-*`, keyframes tuỳ ý (chỉ `animate-spin/pulse/ping/bounce`).
- Kích thước là border-box: hộp cao/rộng 0 có viền sẽ không vẽ gì (nét icon phải có kích thước bằng độ dày nét).
- Độ rộng phần trăm của nội dung overlay tính theo View bọc ngoài chứ không theo cửa sổ: dùng độ rộng cố định (`w-96 max-w-full`).
- `OnHover` không phát "vào" khi phần tử hiện ra ngay dưới con trỏ đang đứng yên.

- `flex-1` (basis 0) trong hàng/cột tự co theo nội dung chia đều chỗ chứ không giữ bề rộng nội dung tối thiểu: dùng `grow`.
- Phần tử `animate-*` làm view chứa nó và mọi view cha render lại mỗi frame; đặt animation chạy liên tục ở nơi có ít view
  cha và view nhỏ (tách vào view con không giúp view cha đứng yên), và giữ `Render` của các view đó không cấp phát.

Class chưa hỗ trợ không bị bỏ qua im lặng: `samples/ComponentsCheck` liệt kê chúng và trả mã lỗi.

## 6. Kiểm chứng

```
dotnet run --project samples/ComponentsCheck -- <thư mục ra>   # kiểm tra hành vi + ảnh từng trang gallery (sáng/tối)
dotnet run --project samples/Components                        # gallery có cửa sổ (Windows)
```

- Mỗi nhóm có `samples/Components/Pages/<Nhóm>Page.cs` (trang gallery, dùng `Demo.Page / Demo.Section / Demo.Row`)
  và `samples/ComponentsCheck/<Nhóm>Checks.cs` (kiểm tra hành vi qua `CheckHarness`).
- `CheckHarness`: `Open(app => app.New(new V()))`, `Click(w, "key")`, `Click(w, point)`, `Move`, `Drag`, `Press`, `Type`,
  `Scroll`, `Frames`, `Wait`, `Box(w, "key")`, `Exists`, `Pixel(w, point)`, `Save(w, "tên.png")`, `Check("mô tả", điều kiện)`.
  Mẫu: `samples/ComponentsCheck/CoreChecks.cs`.
- Kiểm tra phải đo hành vi thật (state sau click, vị trí overlay, màu pixel), không dùng dữ liệu giả để cho qua.
