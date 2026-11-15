---
name: gotx-ui
description: Build GotX views - Tailwind classes, events, overlays.
metadata:
  origin: user
  version: "1.0.0"
---

# GotX UI

Viết màn hình / view cho app dùng GotX. Chi tiết API: `README.md`, `docs/components.md` §3. Class hỗ trợ: `docs/tailwind.md`.

## Mô hình

- `View.Render(Window w)` chạy lại mỗi khi view bị `Notify()`; trả về cây `Div` dựng bằng `w.Div()`.
- State nằm trong field của view. Đổi state → `Notify()`. Báo ra ngoài → `Emit(new Event(...))`.
- Tạo entity qua app: `app.New(new MyView())`; nhúng view con: `w.View(child)` (mỗi view có cache riêng).
- Nghe: `app.Observe(entity, () => ...)`, `app.Subscribe<TEvent>(entity, e => ...)`.

```csharp
public sealed class CounterView : View
{
    int _count;
    public override Div Render(Window w) => w.Div().Class("flex items-center gap-2 p-4")
        .Child(w.Div().Key("add").Focusable()
            .Class("px-3 py-1.5 rounded-md bg-indigo-600 text-white cursor-pointer hover:bg-indigo-700 focus-visible:ring-2 focus-visible:ring-indigo-300")
            .OnClick(this, static (v, _) => { v._count++; v.Notify(); }).Child("+"))
        .Child(_count.ToString());
}
```

## Quy tắc bắt buộc

- Handler là lambda `static` + state: `.OnClick(this, static (v, e) => v.Foo())`. Không capture (cấp phát mỗi frame).
- Phần tử mặc định là **block** (con xếp dọc, giãn hết ngang). Muốn xếp ngang: `flex`.
- Kích thước là **border-box**: hộp 0px có viền không vẽ gì.
- Viền mặc định lấy màu chữ: luôn viết `border border-slate-300` (kèm màu).
- `hover: active: focus: focus-visible: disabled: group-hover:` chỉ đổi phần vẽ, **không đổi layout**. Đổi layout theo state → chọn class bằng điều kiện C#.
- `.Key("tên")` cho phần tử có state qua frame trong danh sách động: vùng cuộn, transition, phần tử `.Focusable()`.
- Không có `z-*`, `fixed`, `sticky`, `m*-auto`: nổi lên trên cùng → overlay; căn → `justify-*`, `items-*`, phần tử đệm `flex-1`.
- Không nối chuỗi class mỗi frame ở phần tử lặp nhiều: dùng hằng hoặc bảng dựng sẵn.
- `VectorPath`, `ImageSource`: tạo một lần (`static readonly`), không tạo trong `Render`.

## Tra nhanh

| Việc | API |
|---|---|
| Click / phải / hover / kéo | `.OnClick` `.OnAuxClick` `.OnHover` `.OnDrag` (`e.FractionX`) |
| Bàn phím | `.Focusable()`, `.OnKey(state, static (s, e) => bool)`, `.AutoFocus()`, `w.FocusKey("k")` |
| Ô nhập | `GotX.Widgets.TextInput` (`Text`, `Multiline`, event `TextChanged`, `Submitted`) |
| Cuộn | `overflow-y-auto` + `.Key`, `w.ScrollTo`, `w.ScrollIntoView("anchor")`, `.OnScroll` |
| Lớp trên cùng | `w.OpenOverlay(view, new OverlayOptions { Modal / Anchor / Placement })` |
| Hẹn giờ | `App.SetTimeout(giây, action)`, `SetInterval` → `Dispose()` để huỷ |
| Icon | `w.Path(VectorPath.Icon("M20 6 9 17l-5-5")).Class("size-4")` |
| Ảnh | `w.Image(ImageSource.FromPngFile(p)).Class("size-10 rounded-full object-cover")` |
| Dark mode | `window.DarkMode`; token `Theme.SetColor(tên, sáng, tối)` |

Handler cần `Window` (mở overlay): lưu `_window = w` trong `Render`.

## Kiểm chứng trước khi báo xong

1. `dotnet build GotX.slnx` → 0 cảnh báo, 0 lỗi.
2. Class chưa hỗ trợ không bị bỏ qua im lặng: gán `Tailwind.OnUnknownClass` hoặc chạy bộ kiểm tra; có token lạ → sửa class (xem skill `gotx-tailwind`).
3. Render headless ra PNG và **mở ảnh xem** (skill `gotx-test`; `UiDoctor.Run` soi cả view).
4. Có sẵn component thì dùng, đừng tự dựng lại: skill `gotx-components`.
