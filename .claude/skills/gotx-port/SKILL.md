---
name: gotx-port
description: Port gpui or HTML+Tailwind UI code to GotX.
metadata:
  origin: user
  version: "1.0.0"
---

# GotX Port

Chuyển UI từ **HTML + Tailwind** (kể cả JSX/TSX, shadcn) hoặc **GPUI (Rust)** sang GotX và chứng minh kết quả khớp. Kiến thức nền: skill `gotx-ui`, `gotx-components`, `gotx-tailwind`; kiểm chứng: `gotx-test`, `gotx-debug`.

## Quy trình (làm đủ thứ tự, không nhảy bước)

1. **Phân loại nguồn.** HTML/TSX → `references/html-tailwind.md`. GPUI `.rs` → `references/gpui.md`. Cả hai cần `references/pitfalls.md` và `references/unsupported.md`.
2. **Kiểm kê** (công cụ trong `tools/GotX.Port`, xem `scripts/README.md`):
   - `dotnet run --project tools/GotX.Port -- classes <nguồn>`: class Tailwind nào GotX chưa hỗ trợ + cách thay.
   - GPUI: `… -- gpui <file.rs>`: dịch chuỗi style sang class Tailwind + liệt kê chỗ phải viết tay (sự kiện, focus, action, list).
   - HTML: `… -- html <file>` sinh khung C# để sửa tiếp (không phải đầu ra cuối).
3. **Chụp nguồn làm thước đo** (HTML): `node .claude/skills/gotx-port/scripts/capture-html.mjs <url|file> --out <dir>` → ảnh + bản đồ layout (hộp và style đã tính của mọi phần tử). GPUI không chụp được trong môi trường này: suy ra kích thước từ code (spacing, `px()`, `rems()`) và ghi giả định.
4. **Lập bản đồ**: mỗi cụm giao diện → (a) component có sẵn trong `docs/components.md` §4 → **dùng nó**, không dựng lại; (b) `View` có state; (c) hàm tĩnh trả `Div`. Quyết định **style component** trước: `ComponentStyle { Default, Rhea, NewYorkV4 }`, bật bằng `ComponentTheme.Use(app, style)` (hoặc `SitePage.Style`, `ComponentStyles.Use(...)` theo phạm vi); trang dùng style khác thì thêm một sheet (`Sheet with {…}`) thay vì ghi đè rải rác. Component nào chỉnh được gì: bảng "Tuỳ biến component" trong `references/html-tailwind.md`.
5. **Viết code** theo `templates/PortedView.cs.txt`: một file < 200 dòng cho mỗi cụm, class **giữ nguyên chuỗi gốc** (đừng "dọn" class), `.Key()` đặt theo chữ/`id`/`data-slot` gốc, handler `static`, icon thành `static readonly VectorPath`, ảnh thành `ImageSource`.
6. **Kiểm chứng bằng số**, không bằng mắt:
   - build 0 cảnh báo; **cổng chặn class lạ** là `Tailwind.OnUnknownClass` (in ra stderr, thoát mã lỗi) trong chương trình dựng ảnh — `classes` trên C# chỉ là phụ (nó không thấy chuỗi `const`/`static readonly`). Parser **nhận** một class chưa chắc là hỗ trợ đầy đủ: một số class chỉ được xấp xỉ (xem `unsupported.md`, mục "nhận nhưng xấp xỉ");
   - dựng headless (`templates/ShotProgram.cs.txt`) rồi `… -- diff ref.png gotx.png` và `compare-layout.mjs`; mục tiêu: vị trí lệch trung vị ≤ 1px, pixel lệch (dung sai 24) < 3%, phần còn lại giải thích được (raster chữ);
   - test `templates/PortTests.cs.txt`: lint sạch ở ≥ 2 độ rộng × sáng/tối, hành vi thật.
7. **Hành vi**: kiểm từng trạng thái của nguồn (hover, focus-visible bằng Tab, active, disabled, mở/đóng, bàn phím) bằng test; so sánh bằng mắt với ảnh nguồn nếu có. Kiểm theo **ngữ nghĩa CSS/nguồn**, không theo trực giác: ví dụ CSS vẫn áp `:hover` cho nút `disabled`, và GotX cũng vậy; nguồn không có handler thì giữ nguyên là trang trí (không tự thêm hành vi).
8. **Báo cáo** theo mẫu cuối trang. Mọi chỗ lệch nguồn phải nêu: cái gì, vì sao (GotX chưa có tính năng nào), cách làm tạm.

## Quy tắc cứng

- Giữ **ngữ nghĩa**, không giữ cú pháp: cùng bố cục, cùng chữ, cùng màu theo token, cùng hành vi. Không thêm tính năng không có ở nguồn.
- Không có CSS cascade/kế thừa tuỳ ý: thuộc tính chữ (màu, cỡ, đậm, căn) kế thừa xuống con; còn lại phải ghi class trên từng phần tử.
- Phần tử mặc định là **block, border-box, không có UA stylesheet**: mọi `h1`, `p`, `a`, `button`, `ul` phải được ghi class tường minh (Tailwind preflight đã xoá style mặc định nên class gốc thường đủ).
- Tailwind chưa hỗ trợ **không được bỏ qua im lặng**: thay bằng thứ tương đương (`references/unsupported.md`), nếu không thể thì ghi vào báo cáo.
- Cần tính năng Core/renderer còn thiếu → **dừng và báo** (precise repro + cần gì), đừng vá vòng bằng thủ thuật làm sai hành vi.
- Dữ liệu/chữ của nguồn dùng nguyên văn; không bịa dữ liệu để vừa giao diện.
- Màu của nguồn không đạt lint tương phản: **giữ màu**, `LintOptions.Suppress` hẹp (luật + phần tử) kèm chú thích, và nêu trong báo cáo — không đổi màu để qua lint.

## Báo cáo cuối

```
Nguồn: <file/url, loại>      Đầu ra: <đường dẫn file>
Component dùng lại: …        View mới: …
Số đo: layout median Δ …px, P90 …px; pixel lệch (tol 24) …%; test …/… pass; lint …
Lệch nguồn: [mục, lý do, cách tạm] …
Hành vi đã kiểm: …           Chưa kiểm: …
Yêu cầu Core/công cụ: …      Chỗ skill này thiếu/mơ hồ: …
```
