---
name: gotx-components
description: Use or author GotX shadcn-style components.
metadata:
  origin: user
  version: "1.0.0"
---

# GotX Components

Dùng hoặc viết component trong `src/GotX.Components`. Nguồn chuẩn: `docs/components.md` (quy ước, bảng API, danh sách, giới hạn). Đọc nó trước khi viết component mới.

## Dùng component

```csharp
ComponentTheme.Install();                    // một lần lúc khởi động: token màu + radius
Ui.Button(w, "Lưu", ButtonVariant.Outline, ButtonSize.Sm).OnClick(this, static (v, _) => v.Save());
_agree = App.New(new Checkbox { Label = "Đồng ý" });           // trong OnCreated()
App.Subscribe<Checkbox.CheckedChanged>(_agree, e => ...);
w.View(_agree)                                                  // trong Render
```

Tìm component: `src/GotX.Components/{Display,Forms,Overlays,Navigation,Chat}`. Ví dụ dùng thật: `samples/Components/Pages/`.

## Chọn loại khi viết mới

| Có state riêng? | Dạng | File | Mẫu |
|---|---|---|---|
| Không | hàm tĩnh `Ui.X(Window w, ...)` → `Div` | `Ui.<Tên>.cs` | `Display/Ui.Button.cs` |
| Có | `sealed class X : View`, event `record struct` | `<Tên>.cs` | `Forms/Checkbox.cs`, `Overlays/Select.cs` |

## Quy tắc

- Setter: so sánh giá trị cũ → gán → `Notify()` → `Emit(new XChanged(...))`. Gọi được trước khi entity gắn vào app.
- Entity con tạo trong `OnCreated()` bằng `App.New(...)`.
- Trạng thái chọn class bằng điều kiện C# (không có `data-[state=...]`).
- Phần tử lặp cần chỉ số: `Indexed<T>` (`Core/Indexed.cs`).
- Nội dung do người dùng đưa vào: thuộc tính `Func<Window, Div>?`.
- Overlay: `.Anchor("gotx-<loại>-" + Id.Value)` duy nhất cho từng instance; mở bằng `window.OpenOverlay`.
- Tương tác được thì phải dùng được bằng bàn phím: `.Focusable()` + vòng focus `focus-visible:ring-[3px] focus-visible:ring-ring/50`; danh sách/menu: `OnKey` ở gốc nội dung + chỉ số "đang chọn" trong state; nội dung overlay `.AutoFocus()`.
- Icon lấy từ `Icon.*` (`Core/Icon.cs`); thiếu thì thêm `static readonly VectorPath` bằng path Lucide.
- Token màu: `bg-background text-foreground bg-primary text-muted-foreground border-border ring-ring…`, không dùng màu cứng.
- `VirtualList` BẮT BUỘC có chiều cao xác định (`Class = "h-96"`, hoặc cha có chiều cao + `flex-1 min-h-0`). Thiếu thì vùng cuộn cao bằng nội dung nên dựng hết mọi hàng (100.000 hàng ≈ 3,5 s/frame) mà không báo lỗi; Core chỉ phát cảnh báo dev một lần `virtual-list-unbounded` (`TraceKind.Warn`, `PerfLog` warn, `Debug` output).
- Mỗi file < 200 dòng; chú thích tiếng Việt nói *vì sao*; không thêm package.

## Khi đối chiếu bản gốc

Bản gốc để đối chiếu: các file `components/ui/*.tsx` của shadcn/ui (https://github.com/shadcn-ui/ui, `apps/v4/registry/new-york-v4/ui`). Chép class rồi bỏ/đổi thứ Core chưa có (§5 của `docs/components.md`). Khác bản gốc ở đâu thì ghi vào §4.

## Kiểm chứng (bắt buộc)

```
dotnet run --project samples/ComponentsCheck -- <thư mục ra>
```

- Thêm trang/section gallery: `samples/Components/Pages/<Nhóm>Page.cs` (`Demo.Page / Section / Row`).
- Thêm kiểm tra hành vi: `samples/ComponentsCheck/<Nhóm>Checks.cs` qua `CheckHarness` (`Click`, `Press`, `Type`, `Wait`, `Box`, `Pixel`, `Check`). Đo hành vi thật: state sau click/phím, vị trí overlay, `w.IsFocused("key")`, màu pixel. Không dùng dữ liệu giả để cho qua.
- Kết quả phải: 0 lỗi, "Không có class Tailwind nào chưa hỗ trợ".
- Mở PNG trang gallery (sáng + tối) và overlay đang mở để xem bằng mắt.
- Cập nhật bảng ở `docs/components.md` §4.
