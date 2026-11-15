# Chưa hỗ trợ → thay bằng gì

Chạy `tools/GotX.Port classes` để biết token nào bị từ chối; bảng này cho cách xử lý. Bảng có thể lỗi thời: nếu parser từ chối thì phải xử lý theo bảng; nếu parser **nhận**, vẫn kiểm mục "nhận nhưng xấp xỉ" bên dưới (parser nhận một class không đảm bảo nó giống trình duyệt).

| Nguồn dùng | Làm gì | Mức lệch |
|---|---|---|
| `mx-auto`, `m*-auto` | `justify-center`/`items-center` ở cha, hoặc phần tử đệm `flex-1` | không (nếu cha là flex) |
| `z-*`, `fixed`, `sticky` | đổi thứ tự cây; nổi lên → `OpenOverlay`; header dính → bố cục flex cột + vùng cuộn bên dưới | nhỏ |
| `peer-*`, `group-focus*`, `focus-within`, `has-*` | chọn class bằng điều kiện C# (`w.IsFocused(key)`, state view) | không |
| `aria-*:`, `data-[state=…]:` | điều kiện C# theo state | không |
| `first: last: odd: even:` | chọn class theo chỉ số khi dựng danh sách | không |
| `placeholder:`, `file:`, `selection:` | `TextInput.PlaceholderColor`, màu chọn của ô; `file:` không có | nhỏ |
| `[&>svg]:size-4`, `[&_p]:…` | ghi class trực tiếp trên từng con | không |
| `@container`, `@sm:` | breakpoint cửa sổ `sm: md:` hoặc `min-[…]:` cho cùng kết quả ở các cỡ cần | nhỏ |
| `backdrop-blur`, `blur-*`, `filter`, `mix-blend-*` | màu đặc có độ mờ (`bg-background/80`) | **thấy được** |
| `mask-*`, `bg-conic` | gradient `bg-linear-*` cho dải mờ; còn lại bỏ + báo cáo | thấy được |
| `rotate-x/y`, `perspective`, `translate-z` | bỏ (không có 3D) | thấy được |
| `border-double`, `outline-offset` âm | `border-solid` / bỏ | nhỏ |
| `columns-*`, `float-*`, `table`, `inline-*` flow | grid/flex; chữ liền dòng xem `html-tailwind.md` | tuỳ |
| `select-none`, `pointer-events-*`, `resize`, `appearance-*` | bỏ; ô chặn chuột = `Disabled` hoặc lớp modal | không/nhỏ |
| `transition-[width]`, `animate-[…]`, keyframes tuỳ ý | `transition-*` nhóm có sẵn, `animate-spin/pulse/ping/bounce`, hoặc `SetInterval` đổi state | tuỳ |
| `text-shadow`, `hyphens`, `writing-mode`, `font-feature-settings` khác `tnum` | bỏ + báo cáo | nhỏ |
| `object-position`, `aspect-[…]` phức tạp | `object-cover/contain/fill` + `aspect-*` | nhỏ |
| ảnh JPEG/WebP/SVG file | giải mã trước → `ImageSource.FromRgba`; SVG → `VectorPath` | không |
| font khác (Inter, Geist…) | nạp TTF `glyf` (không CFF/OTF/TTC/variable) qua `TextSystem.TryAddFont`; không có thì Segoe UI | **thấy được** (bề rộng chữ) |
| `<canvas>`, WebGL, video, iframe | không có | báo cáo |

Quy tắc: nếu thay thế làm đổi hình thấy được → ghi vào báo cáo cuối, đừng giấu.
Nếu thiếu mà có thể thêm vào Core (ví dụ một utility mới) → **không tự sửa Core** trong lúc port; báo cáo kèm repro tối thiểu, người phụ trách Core sẽ làm (skill `gotx-tailwind` mô tả cách thêm).

## Nhận nhưng xấp xỉ (không lỗi, nhưng khác trình duyệt)

| Class | GotX làm gì | Cách xử lý |
|---|---|---|
| `items-baseline` / `self-baseline` | hiện được map về `items-start` (chưa căn theo đường cơ sở; Core đang có việc chờ) | `items-end` + `pb-*` bù đúng số đo từ ảnh nguồn, đánh dấu `// TODO(core)`; ghi báo cáo |
| `space-x-*` / `space-y-*` | xấp xỉ bằng khoảng cách giữa các con | đo lại với nguồn nếu con inline/margin khác |
| `w-fit` / `min-content` / `max-content` | gần với `auto` | đo; thêm `self-start` ở cha flex-col cho phần tử `inline-flex` |
| `antialiased` | không có tác dụng (no-op trong Tailwind v4) | bỏ qua |
| `backdrop-blur-*` | không có | màu đặc có độ mờ; ghi báo cáo |
