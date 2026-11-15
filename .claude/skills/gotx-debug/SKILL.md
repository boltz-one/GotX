---
name: gotx-debug
description: Diagnose GotX UI bugs - layout, clipping, pixels, perf.
metadata:
  origin: user
  version: "1.0.0"
---

# GotX Debug

Tìm nguyên nhân lỗi giao diện bằng công cụ của `GotX.Test`, không đoán từ code. Tra cứu: `docs/testing.md` §3–§7.

## Bước 1: dựng lại lỗi trong một test nhỏ

```csharp
suite.Add("debug/ten-loi", t =>
{
    t.Open(new ViewBiLoi(), new Size(900, 700));
    // thao tác dẫn tới lỗi: t.Click("…"); t.Press(Key.Down); t.Advance(0.5);
    Console.Write(t.Dump(new DumpOptions { RootKey = "vùng-nghi-ngờ", MaxDepth = 3 }));
    t.Save("hien-trang", t.Get("phần-tử"));     // PNG có khoanh vùng → mở bằng Read
});
```

Chạy: `dotnet run --project tests/GotX.Tests -- --filter debug/ --out <thư mục>`. Luôn **mở ảnh** và **đọc bản in cây** trước khi sửa.

Chưa biết lỗi ở đâu → soi cả view trên ma trận kích thước × sáng/tối × scale:

```csharp
Console.Write(UiDoctor.Run(app => app.New(new ViewBiLoi()), new DoctorOptions { OutDir = "out" }).ToText());
```

## Bước 2: chọn công cụ theo triệu chứng

| Triệu chứng | Công cụ | Đọc gì |
|---|---|---|
| Lệch 1–2 px giữa các phần tử | `t.Lint(new LintOptions().Only("misaligned"))`, `t.Dump` | Δtop / Δcenter / Δbottom; hộp `[x,y w×h]` từng node |
| Mép nhoè | `Only("subpixel")` | chỉ còn do `scale-*` lúc đứng yên (Core đã bắt mép vào lưới pixel) |
| Bị cắt, tràn, chữ đè | `Only("clipped", "overflow", "text-overflow")` | cờ `clipped` / `hidden` trong bản in cây; so `Screen` với `Clip` |
| Không thấy phần tử | `t.Find("key")`, `Only("zero-size", "offscreen")` | hộp 0px có viền không vẽ gì (border-box); `display` none; nằm ngoài vùng cắt |
| Pixel sai màu / bị thứ khác đè | `Console.Write(t.Explain(point))` | lớp cuối danh sách nằm trên cùng; cột cuối là node vẽ ra nó |
| Click không ăn | `Only("occluded")`, `t.Tree.TopAt(point)`, `t.Trace.Tail(20)` | có `Input MouseDown` mà không có `Click` → không hitbox nào nhận (bị che, `Disabled`, dưới lớp modal) |
| Phím / focus sai | `t.Trace` (`Key`, `Focus`), `t.Assert.Focused("key")` | thứ tự Tab = thứ tự vẽ; phần tử trong danh sách động thiếu `.Key()`; phím bị phần tử con xử lý trước |
| Giật, chậm, render lại nhiều | `t.Perf.ExpectIdle()`, `ExpectRenders(() => thao tác, max)`, `t.Perf.Measure(60)` | "view đã render" + "view bị đánh dấu bẩn"; cấp phát/frame |
| Hiện nội dung cũ | `t.Perf.ExpectReplayMatchesFresh()` | ảnh từ cache khác ảnh dựng lại → state đổi mà thiếu `Notify()` |
| Giao diện đổi ngoài ý muốn | `t.Snapshot("tên")` | vùng khác thuộc phần tử nào; mở `.diff.png` |
| Chữ khó đọc | `Only("low-contrast")` | tỉ lệ tương phản và hai màu |

## Nguyên nhân hay gặp

- **Không cập nhật:** đổi field mà quên `Notify()`; hoặc đổi state của entity khác mà view không `Observe`.
- **Render mỗi frame:** `animate-*` hoặc transition nằm trong view lớn → tách phần chuyển động vào view con.
- **Mất hover/focus style:** state chỉ đổi phần vẽ; class đổi layout theo `hover:` không có tác dụng.
- **Overlay lệch:** tên `.Anchor(...)` trùng giữa hai instance, hoặc dùng độ rộng phần trăm trong nội dung overlay.
- **Phần tử "biến mất":** phần tử đệm/nét vẽ cao hoặc rộng 0; con `absolute` thiếu cha `relative`.
- **Class không ăn:** token chưa hỗ trợ → luật `unknown-class` (skill `gotx-tailwind`).

## Bước 3: sửa và khoá lại

1. Sửa ở nơi gây ra lỗi (đường dẫn phần tử trong thông báo chỉ tới view và node).
2. Đổi test dựng lỗi thành test hồi quy: thay `Dump/Save` bằng assertion đúng triệu chứng (`Aligned`, `Inside`, `Pixel`, `Focused`, `ExpectRenders`…), đổi tên khỏi `debug/`.
3. Chạy lại toàn bộ (skill `gotx-test`, mục "Trước khi báo xong").

Lỗi nằm trong framework chứ không phải trong view → skill `gotx-core`. Lỗi chỉ thấy trên cửa sổ thật (D3D11, DPI, IME) → chạy `samples/Components` và chụp bằng `PrintWindow`.
