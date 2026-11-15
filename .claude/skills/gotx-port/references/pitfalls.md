# Bẫy khi port (rút từ lần port shadcn)

## Layout
- **Block mặc định**: quên `flex` → con xếp dọc chiếm cả bề ngang. HTML `div` cũng block, nhưng `span/a/button/img/input` là inline trong trình duyệt → ở GotX chúng cũng thành block.
- **Chữ liền dòng** (`<p>text <b>bold</b> text</p>`) không có inline flow; xem `html-tailwind.md`.
- **min-width:auto của flex** giống CSS: nhãn không xuống dòng sẽ không co; muốn cắt thì `min-w-0 truncate`.
- `w-full max-w-sm` / `max-w-*` hoạt động đúng CSS (đo và đặt chỗ cùng giới hạn).
- `%` chiều cao cần cha có chiều cao xác định; `h-full` trong cha cao theo nội dung = auto (như CSS).
- Root của trang `FullBleed` cần `size-full` ở node view (xem `samples/Shadcn/SiteShell.cs`), nếu không cao theo nội dung.
- Hộp 0px có viền không vẽ (border-box): nét kẻ icon dựng bằng hộp phải cao/rộng bằng độ dày.
- Overlay (`Popover`, `Select`…) đo theo View bọc: dùng bề rộng cố định (`w-96 max-w-full`), không `%` cửa sổ.

## Style
- Viền mặc định **màu chữ**: luôn viết màu viền cùng `border`.
- Variant trạng thái chỉ đổi phần vẽ; đổi layout theo hover không làm được.
- Gọi `.Class()` nhiều lần: variant trạng thái được gộp, lần sau thắng khi trùng thuộc tính; class `lg:` viết **sau** `xl:` sẽ bị `xl:` ghi đè ở màn rộng — theo thứ tự viết, không theo độ ưu tiên Tailwind.
- Style component: `ComponentTheme.Install()` một lần; trang dùng style khác bản mặc định phải chọn đúng style (xem `docs/components.md`); sai style → lệch 1–4px khắp nơi (nút 32 vs 36px, bo góc, bóng).
- Bóng đổ ngoài + nền trong suốt: nền bị lộ bóng (lỗi Core đang được sửa); tạm dùng `bg-card`.
- `bg-clip-padding` (nút shadcn viền trong suốt) cần Core hỗ trợ — kiểm bằng `classes`.

## State và hiệu năng
- Quên `Notify()` → UI không đổi. Handler dùng lambda `static` + state; capture sẽ cấp phát mỗi frame.
- Phần tử trong danh sách động cần `.Key()` ổn định cho focus/cuộn/transition; vùng cuộn **luôn** cần `.Key()`.
- Không nối chuỗi class mỗi frame ở phần tử lặp nhiều: dựng bảng chuỗi trước hoặc dùng hằng.
- `VectorPath`/`ImageSource`/`Func<>` tạo một lần (static readonly / field), không tạo trong `Render`.
- Animation liên tục (`animate-*`) làm cả chuỗi view cha render lại mỗi frame: đặt trong view nhỏ, nông.
- Con `Notify()` làm bẩn mọi view cha: chia view theo vùng thay đổi.

## Đo và kiểm
- Headless cần font: dùng `UiTest`/harness có sẵn (nạp Segoe UI/Consolas); trang dùng Geist phải nạp Geist trước, nếu không chữ lệch bề rộng cả trang.
- Số đo layout lấy từ `Window.Inspect`/`TreeDump.ToJson`, không từ ảnh; ảnh chỉ để nhìn và đo % pixel.
- Raster chữ khác trình duyệt (hinting, AA): luôn còn lệch vài pixel ở mép chữ; đừng "sửa" bằng cách đổi class.
- Frame soi (`Inspect`) render lại **mọi** view: đừng đếm render bằng biến trong `Render()`, dùng `t.Perf.CountRenders`.
- Trạng thái focus-visible: bấm Tab (`t.Press(Key.Tab)`), không dùng `FocusKey` (luôn bật vòng focus) khi so với `:focus-visible` của trình duyệt.
- Reference trình duyệt chụp với animation tắt: GotX chờ transition kết thúc trước khi chụp (`t.Advance(…)`).

## Form / TextEntry (từ lần thử skill)
- Đặt `.Key("id")` trên `w.View(entry)`: chữ nhập trong ô không phải node chữ nên `ClickText`/`FindText` không thấy.
- Chữ của `TextEntry` một dòng nằm sát trên trong ô cao (`h-9`): dùng BoxClass `flex flex-col justify-center` (qua `BoxClassOverride`).
- Nền trong suốt + bóng (`bg-transparent shadow-xs`) lộ bóng xám trong ô (lỗi Core đang sửa): tạm `bg-white`/`bg-card`, đánh dấu `TODO(core)`.
- Sự kiện: `OnClick` chỉ chạy cho phần tử trên cùng tại điểm bấm; GPUI thì chạy cả cha (bubble) — xem `gpui.md`.