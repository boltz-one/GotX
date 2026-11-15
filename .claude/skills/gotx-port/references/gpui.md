# GPUI (Rust) → GotX

GPUI = bộ UI của Zed (Rust). Tên method style của nó sinh từ tên Tailwind, nên **phần style dịch gần máy móc**; phần khó là mô hình entity/action/focus/list. Công cụ `gpui` dịch chuỗi style và liệt kê các điểm phải viết tay. (Các bảng ngay dưới viết theo hiểu biết về GPUI; **mục cuối "Sự thật đã kiểm chứng" mới là phần đối chiếu với mã nguồn** và thắng khi hai nơi mâu thuẫn. Gặp API lạ: đọc https://github.com/zed-industries/zed/tree/main/crates/gpui rồi mới ánh xạ.)

## Mô hình

| GPUI | GotX |
|---|---|
| `struct V` + `impl Render for V { fn render(&mut self, window, cx) -> impl IntoElement }` | `sealed class V : View` + `override Div Render(Window w)` |
| `cx.new(\|cx\| V::new())` → `Entity<V>` | `App.New(new V())` (trong `OnCreated()` của view cha); nhúng bằng `w.View(child)` |
| `cx.notify()` | `Notify()` (xếp hàng, vẽ ở frame sau) |
| `cx.emit(Event)` + `impl EventEmitter<Event> for V` | `Emit(new Event(..))` với `readonly record struct` |
| `cx.subscribe(&entity, \|this, _, ev, cx\| …)` | `App.Subscribe<TEvent>(entity, e => …)` (giữ `Subscription`, Dispose khi bỏ) |
| `cx.observe(&entity, …)` | `App.Observe(entity, () => …)` |
| `entity.update(cx, \|v, cx\| …)` / `read(cx)` | gọi method trực tiếp rồi `Notify()` (một luồng chính) |
| `cx.spawn(async …)` / `background_executor` | thread nền + `App.Post(() => …)`; hẹn giờ: `App.SetTimeout/SetInterval` |
| `Context<Self>`, `Window`, `App` | `View` (có `App`), `Window w` trong `Render` |
| `SharedString` | `string` |
| Global/`cx.global::<T>()` | field tĩnh hoặc entity gốc truyền xuống |
| Model (entity không render) | lớp kế thừa `Entity` (`App.New`), `Notify/Emit` như view |

## Element và style

`div()` → `w.Div()`; `h_flex()/v_flex()` (Zed ui) → `flex` / `flex flex-col`; `.child(x)`/`.children(iter)` → `.Child(x)` (lặp bằng vòng `foreach`).
Style: tên method đổi `_` → `-` (`flex_col`→`flex-col`, `gap_2`→`gap-2`, `px_4`→`px-4`, `size_full`→`size-full`, `rounded_md`→`rounded-md`, nhưng px khác theme GotX: xem mục c); `0p5`→`0.5`, `1_2`→`1/2`; có tham số → giá trị tuỳ ý:

| GPUI | Class |
|---|---|
| `.w(px(240.))`, `.h(rems(2.))` | `w-[240px]`, `h-[2rem]` |
| `.border_1()` / `.border_color(rgb(0x3b82f6))` | `border` / `border-[#3b82f6]` |
| `.bg(rgb(0x1e1e2e))`, `.bg(hsla(h,s,l,a))` | `bg-[#1e1e2e]`; hsla → đổi sang hex có alpha (`bg-[#rrggbbaa]` hoặc `/α`) |
| `.bg(cx.theme().colors().x)` | token ngữ nghĩa gần nhất (`bg-background`, `bg-card`, `bg-accent`…) hoặc `Theme.SetColor`; ghi vào báo cáo |
| `.text_color(…)`, `.text_sm()`, `.font_weight(FontWeight::BOLD)` | `text-[#…]`, `text-sm`, `font-bold` |
| `.opacity(0.5)`, `.shadow_sm()`, `.cursor_pointer()` | `opacity-50`, `shadow-sm`, `cursor-pointer` |
| `.overflow_y_scroll()` | `overflow-y-auto` + `.Key("…")` (bắt buộc để giữ vị trí cuộn) |
| `.absolute().top_0().left_0()` | `absolute top-0 left-0` (cần cha `relative`) |
| `.hover(\|s\| s.bg(…))`, `.active(…)`, `.focus(…)`, `.group_hover("g", …)` | `hover:bg-…`, `active:…`, `focus:…`, `group-hover:…` (+ `group` trên cha) |
| `.when(cond, \|this\| this.bg(..))`, `.when_some` | chọn chuỗi class bằng điều kiện C# |
| `.id("name")` | `.Key("name")` |
| `.occlude()`, `.block_mouse_except_scroll()` | `Modal`/lớp overlay chặn chuột, hoặc bỏ qua (xem b2) |
| `deferred(...)`, `anchored()` (GPUI vẽ nổi lên trên) | không có `z-*`; dùng `Window.OpenOverlay` (neo bằng `.Anchor` + `OverlayOptions`) |

GPUI không có cascade và không có kế thừa kiểu CSS ngoài text style; style đặt trên chính element → ánh xạ 1-1 sang class của element đó.

## Sự kiện, focus, phím

| GPUI | GotX |
|---|---|
| `.on_click(cx.listener(\|this, ev, window, cx\| …))` | `.OnClick(this, static (v, e) => v.Method())` (state là `this`; không capture) |
| `.on_mouse_down/up(MouseButton::Right, …)` | `.OnAuxClick(…)` (phải/giữa, down+up); trái: `OnClick`/`OnDrag` |
| `.on_mouse_move`, `.on_hover(\|hovered, window, cx\|…)` | `.OnHover(state, static (s, inside) => …)`; di chuột chi tiết: `OnDrag` |
| `.on_scroll_wheel` | cuộn tự động; theo dõi bằng `.OnScroll` |
| `FocusHandle`, `.track_focus(&h)`, `window.focus(&h)` | `.Focusable()` + `.Key("k")`; `window.FocusKey("k")`; `window.IsFocused("k")` |
| `.key_context("X")`, `actions!`, `.on_action(…)`, `KeyBinding::new("cmd-s", Save, …)` | **không có hệ action/keymap**: `.OnKey(state, static (s, e) => bool)` ở phần tử cha, so `e.Key`/`e.Control`; ghi lại bảng phím gốc trong báo cáo |
| `.on_key_down` | `.OnKey(…)` (trả `true` nếu đã xử lý, `false` để nổi lên cha) |
| `.tooltip(\|window, cx\| Tooltip::text(…))` | `Tooltip` (component) |
| `.drag_over`, `.on_drag`, `.on_drop` | `OnDrag` cho kéo slider/thanh chia; kéo-thả giữa phần tử chưa có |
| Text input (`Editor`, `TextInput` ví dụ) | `GotX.Widgets.TextInput` / `Input` |

## Danh sách, ảnh, vẽ tay

- `uniform_list` / `list` (ảo hoá): GotX **chưa có**. Danh sách ngắn → `foreach` trong `overflow-y-auto`; danh sách lớn (>~500 dòng) → báo cáo (cần phân trang, hoặc yêu cầu tính năng ảo hoá).
- `img(path)` → `w.Image(ImageSource…)` (PNG); `svg().path("icons/x.svg")` → `VectorPath` (chép `d` từ file SVG).
- `canvas(prepaint, paint)`, `paint_quad`, `window.paint_*`: không có API vẽ tay; dựng bằng `Div` (hộp bo góc, gradient, viền) hoặc `VectorPath.Polyline` cho đường; không thể thì báo cáo.
- Animation: `with_animation(…)` → `transition-*`/`animate-*` (4 keyframe dựng sẵn) hoặc `App.SetInterval` đổi state.
- Đơn vị: `px(n)` = px, `rems(n)` = n×`rem_size` của cửa sổ (coi 16px, chưa kiểm chứng); `relative(0.5)` = `w-1/2`; `DefiniteLength`/`AbsoluteLength` → class tương ứng.
- Theme của Zed/`ui::prelude`: nhãn, nút, icon của crate `ui` → component GotX tương ứng (`Ui.Button`, `Icon.*`, `Label`…) nếu có; nếu không, dựng từ `Div` + class.

## Sự thật đã kiểm chứng với mã nguồn Zed (nhánh main, đọc qua raw.githubusercontent.com)

Mỗi dòng ghi file nguồn. Dòng "chưa kiểm chứng" là điều chưa đọc được trong các file đã mở.

| # | Sự thật | Nguồn | Hệ quả khi port |
|---|---|---|---|
| a1 | `TextStyle::default()`: `font_size: rems(1.)`, **`line_height: phi()`**, `font_weight` mặc định. `phi()` = `relative(GOLDEN_RATIO)` = **1.618 × cỡ chữ**; `line_height_in_pixels` = `(line_height × font_size).round()` | `style.rs` (Default for TextStyle, `line_height_in_pixels`), `geometry.rs` (`phi`, `DefiniteLength::to_pixels`) | Dòng chữ GPUI cao hơn Tailwind: chữ 14px thành khoảng 23px (Tailwind `text-sm` = 20px). Muốn khớp bố cục thì thêm `leading-[1.618]` (hoặc px đã làm tròn) |
| a2 | `text_xs/sm/base/lg/xl/2xl/3xl` chỉ đặt `font_size` = `rems(0.75 / 0.875 / 1.0 / 1.125 / 1.25 / 1.5 / 1.875)`; **không đụng `line_height`** | `styled.rs` | `text_sm()` thành `text-sm`, nhưng Tailwind kèm leading 20px còn GPUI thì giữ 1.618: thêm leading riêng (a1) |
| b1 | Click listener chỉ chạy khi mouse-down ở pha Bubble và `hitbox.is_hovered`, rồi mouse-up (pha Capture kiểm lại hovered, pha Bubble gọi listener). **Sau khi gọi listener không có `stop_propagation()`** | `elements/div.rs` (`paint_mouse_listeners`) | Con và cha cùng có `on_click` thì **cả hai chạy** cho một cú click (mọi hitbox chứa con trỏ và không bị chặn). Muốn chỉ con chạy thì GPUI cần `cx.stop_propagation()` trong listener con. Với GotX: kiểm hành vi thật bằng test trước khi giả định |
| b2 | `hitbox.is_hovered` đúng với **mọi** hitbox chứa con trỏ mà không bị hitbox phía trước chặn; chặn bằng `occlude()` (`HitboxBehavior::BlockMouse`) hoặc `block_mouse_except_scroll()` | `window.rs` (`Hitbox::is_hovered`, `HitboxBehavior`, `Frame::hit_test`), `div.rs` (doc `occlude`) | `.occlude()` / `.block_mouse_except_scroll()` tương ứng lớp overlay Modal; không có `block_mouse_down()` (tên đó trong bản cũ của bảng là sai) |
| b3 | `.id(..)` trả về `Stateful<Self>`; `on_click` thuộc `StatefulInteractiveElement`: phần tử có `on_click` phải có `.id()`. Enter/Space trên phần tử đang focus cũng gọi `click_listeners` (`ClickEvent::Keyboard`) | `elements/div.rs` | GotX: `.Focusable()` + `OnClick` đã chạy Enter/Space (docs/components.md §3) |
| c | Bo góc: `rounded_xs` 0.125rem = **2px**, `sm` 0.25rem = **4px**, `md` 0.375rem = **6px**, `lg` 0.5rem = **8px**, `xl` 0.75rem = **12px**, `2xl` 16px, `3xl` 24px, `none` 0, `full` = **`px(9999.)`** | `gpui_macros/src/styles.rs` (`corner_suffixes`) | Theme shadcn của GotX khác (`rounded-sm` 6, `md` 8, `lg` 10, `xl` 14): giữ đúng pixel thì dùng `rounded-[6px]`; công cụ `gpui` in nhắc nhở này |
| d1 | Thang spacing (p/m/gap/w/h/size...): `0`=0, `0p5`=0.125rem, `1`=0.25rem (4px), `2`=0.5rem, `3`=0.75rem, **`4`=1rem (16px)**, `5`=1.25rem, `6`=1.5rem, `8`=2rem, `10`=2.5rem, `12`=3rem, `16`=4rem ... `96`=24rem, `128`=32rem, `px`=1px, `full`=100%. **Trùng thang Tailwind** (n x 0.25rem). Padding không có `auto` | `gpui_macros/src/styles.rs` (`box_style_suffixes`) | `p_4` = `p-4`, `gap_2` = `gap-2`: dịch nguyên tên. Độ rộng rem phụ thuộc `rem_size` của cửa sổ (coi 16px, **chưa kiểm chứng**) |
| d2 | Độ dày viền tính bằng **px**: `border_0..12`, `16`, `20`, `24`, `32`; `border_1` = 1px | `styles.rs` (`border_suffixes`) | `border_1` thành `border`, `border_2` thành `border-2`, `border_3` thành `border-[3px]` |
| d3 | Bóng: `shadow_sm` = 2 lớp (0 1px 3px, 0 1px 2px spread -1; alpha 0.1), `md`, `lg`, `xl`, `2xl` theo Tailwind; có thêm `shadow_2xs`, `shadow_xs` | `styles.rs` (`box_shadow_style_methods`) | `shadow-*` trùng tên |
| d4 | Mặc định `Style`: `display: Block`, `position: Relative`, `flex_direction: Row`, `flex_shrink: 1.0`, `overflow: Visible`, `size: auto` (enum `Display` có `#[default] Flex` nhưng `Style::default()` đặt `Block`) | `style.rs` (Default for Style) | `div()` không `.flex()` là block, như GotX. GPUI mặc định `position: Relative` nên con `absolute()` bám cha mà không cần gì thêm; ở GotX hãy đặt `relative` ở cha cho chắc |
| d5 | `flex_1` = grow 1, shrink 1, basis 0%; `flex_auto` = 1, 1, auto; `flex_none` = 0, 0, auto (khớp Tailwind) | `styled.rs` | Dịch nguyên tên |
| d6 | `truncate` = `overflow_hidden` + `whitespace_nowrap` + `text_ellipsis`; `line_clamp(n)` = `line_clamp` + `overflow_hidden` | `styled.rs` | `truncate`, `line-clamp-N` |
| e | `items_baseline` = `align_items: Baseline`, `items_start` = `FlexStart`, `items_center` = `Center`, `items_stretch` = `Stretch`; `self_baseline` = `AlignSelf::Baseline` (layout kiểu CSS flexbox nên baseline thật) | `styled.rs` | **GotX không có căn baseline**: `items-baseline` được parser nhận nhưng chạy như `items-start`; `self-baseline` bị từ chối (`Tailwind.LayoutUtilities.cs`). Chữ cỡ khác nhau đặt cạnh nhau: chỉnh tay bằng `pt-*` / `items-end` |
| f1 | `actions!(ns, [A, B])` sinh struct đơn vị (`Clone, PartialEq, Default, Debug, Action`), tên `ns::A`. Phím gắn vào action trong **keymap**, action được nghe bằng **`on_action` trong cây phần tử** | `action.rs` | GotX không có hệ action: mỗi cặp (phím, action) thành một nhánh trong `.OnKey` |
| f2 | Gửi phím: từ nút đang focus lên gốc; `key_context` ghép với `KeyBinding` ở nút đó; binding gần nhất thắng (Editor, rồi Pane, rồi Workspace); listener `on_action` đăng ký ở nút trên đường đi | `key_dispatch.rs` (module doc, `bindings_for_input`, `dispatch_key`) | Cần `.Focusable()` ở phần tử nhận phím hoặc ở cha của nó |
| f3 | Không có `KeyBinding` cho một action thì phím **không** sinh action (chỉ `window.dispatch_action` bằng mã mới chạy `on_action`); chuỗi phím không khớp trả danh sách binding rỗng. Phím chưa khớp có tới `on_key_down` hay ô nhập hay không: `key_dispatch.rs` không nói (**chưa kiểm chứng**) | `key_dispatch.rs`, `action.rs` | Port: liệt kê mọi `KeyBinding::new("..", Action, ctx)` trong báo cáo; action không có binding thì handler `on_action` chỉ được gọi từ mã |
| g | `overflow_scroll/x/y_scroll`, `hover/active/focus/group_hover`, `when/when_some`, `h_flex/v_flex`, `Button/Label/Icon` của crate `ui`: **chưa kiểm chứng** (macro `overflow_style_methods!` và trait `InteractiveElement` không nằm trong các file đã đọc) | | Giữ các bảng trên như giả định; gặp trường hợp lạ thì đọc nguồn |
