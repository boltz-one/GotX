# HTML + Tailwind → GotX

Nguồn đúng về class hỗ trợ: `docs/tailwind.md` (đọc khi nghi ngờ). Công cụ `classes` trả lời chính xác từng token.

## Phần tử

| HTML | GotX |
|---|---|
| `div section main header footer nav aside article form fieldset ul ol li` | `w.Div().Class("…")` (block; `ul/li` không có dấu đầu dòng) |
| `p h1–h6 span a strong em small label code` | `w.Div().Class("…").Child("chữ")`. Không có inline flow: chữ liền nhau trong một `<p>` có thẻ con inline → (a) một `Child("…")` nếu cùng kiểu, (b) `flex flex-wrap` hàng nhiều `Div`, giữ khoảng trắng bằng chữ có sẵn dấu cách; chữ đậm/nghiêng giữa dòng là điểm lệch cần ghi |
| `button` | `Ui.Button(w, "chữ", variant, size)` nếu class gốc là biến thể shadcn; nếu không, `w.Div().Key(..).Focusable().Class("… cursor-pointer").OnClick(…)` (Enter/Space tự kích hoạt) |
| `a href` | như button + `OnClick` mở đích; link trong đoạn chữ xem hàng `span` |
| `input text/email/password` | `Input` (`TextEntry.Box.Password = true` cho mật khẩu), `Textarea`, `Checkbox`, `RadioGroup`, `Switch`, `Slider`, `NativeSelect` / `Select` / `Combobox` |
| `img` | `w.Image(ImageSource.FromPngFile(..)).Class("size-10 object-cover rounded-full")` (chỉ PNG; ảnh khác cần giải mã trước → `FromRgba`) |
| `svg` | `static readonly VectorPath X = VectorPath.Icon("path d…", "circle cx cy r", …)`; `w.Path(X).Class("size-4")`; màu theo `currentColor` = màu chữ; chỉ một màu |
| `table thead tbody tr th td` | `Ui.Table*` hoặc `grid grid-cols-[…]` lặp theo hàng |
| `dialog`, popover, dropdown, tooltip | `Dialog`/`AlertDialog`/`Sheet`/`Popover`/`DropdownMenu`/`Tooltip`… (overlay lớp trên cùng, không phải phần tử trong cây) |
| `hr` | `Ui.Separator` hoặc `h-px bg-border` |

## Mô hình bố cục khác trình duyệt

- Mặc định **block** xếp dọc, giãn hết ngang; `inline*` = block. Muốn hàng: `flex`.
- Kích thước **border-box** luôn (preflight). Hộp cao/rộng 0 có viền không vẽ gì: nét kẻ phải có kích thước bằng độ dày.
- Flex đúng CSS: `min-width:auto` (không co dưới min-content), grow/shrink đóng băng ở min/max, `min-w-0` hoạt động. Grid: `grid-cols-*`, `col-span`, `col-start/end`, `grid-flow`, track `fr/px/auto`.
- Không có `margin: auto` (`mx-auto`, `ml-auto`): dùng `justify-center` / `justify-between`, `items-center`, hoặc phần tử đệm `flex-1`.
- `position`: `relative/absolute` có; **không** `fixed`/`sticky`; **không** `z-*` — thứ tự vẽ là thứ tự cây, con `absolute` vẽ sau anh em; nổi lên trên cùng → overlay (`window.OpenOverlay`).
- Cuộn: `overflow-*-auto` + `.Key("…")` bắt buộc; thanh cuộn kiểu shadcn: `scrollbar-hover scrollbar-w-2.5 scrollbar-thumb-border` hoặc `Ui.ScrollArea`.
- Đơn vị: rem = 16px; `%` theo cha; `vw/vh` trong `w-screen/h-screen` và `calc()` đơn giản.

## Variant

Có: `hover: active: focus: focus-visible: disabled: group-hover: dark: sm: md: lg: xl: 2xl: max-*: min-[…]: max-[…]:` (và `!`). Chúng **chỉ đổi phần vẽ** (màu, bóng, transform, opacity), không đổi layout.
Chưa có (chọn bằng C#): `peer-*`, `group-focus*`, `focus-within`, `aria-*`, `data-[state=…]`, `has-[…]`, `first/last/odd/even`, `placeholder:`, `file:`, `selection:`, `@container`, `[&>svg]:` arbitrary variants.
Cách thay: trạng thái (open/checked/selected/active tab) → chọn chuỗi class bằng điều kiện C# khi `Render`; `group-focus` → đặt class lên con theo `w.IsFocused(key)`; `[&>svg]:size-4` → ghi `size-4` trên từng icon.

## Màu, chữ, font

- Token ngữ nghĩa shadcn (`bg-background text-foreground bg-primary text-muted-foreground border-border ring-ring`…) có sẵn qua `ComponentTheme.Install()`, tự đổi sáng/tối; màu Tailwind (`bg-slate-900`, `text-[#0f172a]`, `/50` độ mờ) dùng được. Biến CSS `var(--x)` → token tương ứng hoặc `Theme.SetColor("x", sáng, tối)`.
- Viền mặc định lấy **màu chữ** (Tailwind v4): luôn có màu viền tường minh khi nguồn có (`border border-border`).
- Font: sample shadcn dùng Geist (`samples/Shadcn/Assets/fonts`); app khác dùng Segoe UI/Consolas (`font-mono`). Kerning có; ligature chưa; `tabular-nums` có (nếu font có `tnum`).
- Chữ: `line-clamp-*`, `text-balance`, `truncate`, `uppercase`, `tracking-*`, `underline`, `italic` có; `text-shadow`, `hyphens`, `writing-mode`, cột chữ chưa.

## Hiệu ứng

`transition[-colors|-opacity|-shadow|-transform]` + `duration-*` + `ease-*` (chỉ thuộc tính vẽ); `animate-spin|pulse|ping|bounce` (chỉ 4 cái; keyframes tuỳ ý chưa). Animation liên tục làm view chứa nó và mọi view cha render lại mỗi frame: đặt ở view nhỏ, nông.
Gradient `bg-linear-*` / `from- via- to-` có. `backdrop-*`, `blur`, `filter`, `mix-blend`, `mask` chưa.

## Hành vi (JS → C#)

| Web | GotX |
|---|---|
| `onclick` / handler | `.OnClick(state, static (s, e) => …)`; chuột phải `OnAuxClick`; kéo `OnDrag`; hover `OnHover`; cuộn `OnScroll` |
| `:focus-visible`, tab order | `.Focusable()`; Tab theo thứ tự vẽ; `.AutoFocus()`; `window.FocusKey("k")` |
| `keydown` | `.OnKey(state, static (s, e) => bool)` (nổi lên cha); Enter/Space trên phần tử focus = click |
| state (`useState`) | field của `View` + `Notify()` |
| props / events | thuộc tính C# + event `record struct` qua `Emit` / `App.Subscribe` |
| `setTimeout/Interval` | `App.SetTimeout/SetInterval` (trả `IDisposable`) |
| `fetch` / async | thread nền + `App.Post(() => …)`; không có promise |
| portal/Radix popper | `window.OpenOverlay(view, new OverlayOptions { Anchor = "…", Placement = … })` |
| `<form>` submit/validation | handler tự gom giá trị từ `TextInput.Text`; không có validation tích hợp |

## Tuỳ biến component (chỉnh được gì)

| Component | Chỉnh class thế nào |
|---|---|
| Button, Badge, Card, Item, Empty, Progress, Breadcrumb… | chọn style (`ComponentStyle`); thêm class bằng `.Class("…")` nối sau (lần sau thắng khi trùng thuộc tính) |
| Input / Textarea (`TextEntry`) | `BoxClassOverride (Func<bool,string>)` (arg = invalid), `MinBoxHeight`; `Box.Password = true`; muốn sửa nhiều → kế thừa `TextEntry` (BoxClass/DisabledClass) |
| Select / NativeSelect | `Select.TriggerClass`, `Select.ContentClass`; NativeSelect bọc Select (không có hook bề rộng riêng: dùng `TriggerClass` qua Select) |
| DropdownMenu / menu | `DropdownMenu.TriggerClass`/`TriggerOpenClass`, `MenuOwner.MenuClass`, `MenuItem.Custom(Func<Window,Div>)` hàng không tương tác |
| Switch, RadioGroup, Checkbox | class cố định theo style; muốn khác → style sheet mới hoặc dựng bằng `Div`+`Focusable` giữ class gốc |

Không thấy hook cho thứ mình cần → **không** sửa component trong lúc port: dựng bằng `Div` với class gốc, ghi vào báo cáo.

## Bố cục inline của trình duyệt (nguồn của nhiều lệch 3–6px)

- `space-y-*` giữa một `label` inline và `input`: margin dọc của phần tử inline bị bỏ qua; dòng của label cao bằng strut của cha (ví dụ 16/24 → 24px). Đừng đổi `space-y` thành `gap` mù quáng: **đo** `rect` của label và `y` của anh em kế tiếp trong bản đồ layout, rồi ghi `leading-*`/`mt-*` cho đúng số đo.
- `textarea` inline-block để dư ~6px bên dưới (baseline gap); `inline-flex` trong cha block: ở GotX dùng cha `flex flex-col` + `self-start` ở con (`w-fit` không đủ).
- `button`/`input` là inline-block: chiều cao dòng chứa chúng lấy theo dòng của cha — đo từ ảnh nguồn.
- Windows Chrome vẽ `font-medium` bằng Segoe UI **Semibold**: nạp `seguisb.ttf` ở `FontWeight.Medium` để bề rộng chữ khớp (dùng Geist thì nạp đúng trọng lượng).

## Màu

Bảng màu mặc định của GotX hiện theo Tailwind v3 (hex); trang Tailwind v4 dùng oklch nên lệch (vd. `red-600` v3 `#dc2626`, v4 `#e7000b`; `indigo-600` `#4f46e5` vs `#4f39f6`). Cho tới khi bảng màu Core chuyển sang v4: đọc màu tính toán từ JSON chụp trang nguồn và ghi đè `Theme.Colors[...]` lúc khởi động của chương trình port (một file `PortTheme.cs`); `accent-*` của input native → token `primary` qua `Theme.SetColor`.