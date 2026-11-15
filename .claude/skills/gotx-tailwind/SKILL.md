---
name: gotx-tailwind
description: Check or add Tailwind utility support in GotX.
metadata:
  origin: user
  version: "1.0.0"
---

# GotX Tailwind

GotX biên dịch chuỗi class Tailwind v4 thành lệnh gán `Style`. Bảng hỗ trợ đầy đủ: `docs/tailwind.md`.

## Class có được hỗ trợ không?

1. Tra bảng trong `docs/tailwind.md`.
2. Thử thật: `Tailwind.Parse("...").Unknown` liệt kê token không hiểu; hoặc gán `Tailwind.OnUnknownClass = (classes, token) => ...`.
3. `samples/ComponentsCheck` và `tests/GotX.Tests` (luật lint `unknown-class`) coi class lạ là lỗi; `samples/Headless` chỉ in danh sách. Class lạ **không** được bỏ qua im lặng.

## Class chưa hỗ trợ: xử lý theo thứ tự

1. **Thay bằng thứ có sẵn** (thường là đủ):

| Tailwind | Trong GotX |
|---|---|
| `mx-auto`, `ml-auto` | `justify-center` / `justify-between`, phần tử đệm `flex-1` |
| `z-*`, `fixed`, `sticky` | overlay (`window.OpenOverlay`); con `absolute` vẽ sau anh em |
| `data-[state=open]:…`, `aria-*:`, `peer-*:` | chọn class bằng điều kiện C# |
| `hover:p-4` (đổi layout theo state) | không có; state chỉ đổi màu/bóng/transform |
| gradient, `backdrop-*`, blur | màu đặc + `shadow-*` |
| keyframes tuỳ ý | `animate-spin/pulse/ping/bounce`, hoặc `transition` + state |
| `tracking-[…]` | `tracking-tighter…widest` |

2. **Giá trị tuỳ ý**: `w-[320px]`, `text-[15px]/[22px]`, `bg-[#0f172a]`, `rotate-[17deg]`, `grid-cols-[200px_1fr_auto]`.
3. **Thêm utility vào Core** nếu thật sự cần (dưới đây).

## Thêm một utility

| Bước | File |
|---|---|
| Field mới trong `Style` (nếu chưa có); thuộc tính chữ kế thừa thì thêm cả `TextStyle` + `Refine` | `Styling/Style.cs`, `Styling/TextStyle.cs` |
| Giá trị `Prop` mới | `Styling/ClassList.cs` |
| Phân tích token → `Emit(Prop.X, …)` | không giá trị: `Tailwind.Utilities.cs`; layout: `Tailwind.LayoutUtilities.cs`; vẽ/chữ: `Tailwind.VisualUtilities.cs` |
| Gán vào `Style` | `Tailwind.Apply.cs` |
| Dùng field khi layout / vẽ | `Layout/LayoutEngine*.cs` / `App/Window.Paint*.cs` |
| Token theme (màu, spacing, radius…) | `Styling/Theme.cs` |
| API có kiểu (`.Flex().P4()`) | sửa `tools/StyleGen/StyledWriter*.cs` / `TwWriter.cs`, rồi sinh lại — **không sửa tay `*.g.cs`** |
| Bảng hỗ trợ | `docs/tailwind.md` |

```
dotnet run --project tools/StyleGen -- src/GotX.Core/Styling
```

## Lưu ý khi viết parser

- Variant (`hover:`, `dark:`, `md:`, `max-md:`) đã được tách trước; utility chỉ nhận phần sau dấu `:` cuối và cờ dấu âm.
- Màu là token ngữ nghĩa có biến thể tối (`Theme.SetColor(tên, sáng, tối)`) → `AddOp` tự gắn màu tối; không tự viết `dark:` cho token.
- Thuộc tính đổi **layout** không được có hiệu lực qua variant trạng thái.
- Variant mới → `ApplyVariant` trong `Tailwind.cs` + cờ `StateVariant` + nơi bật cờ (`Window.PaintParts.cs`).

## Kiểm chứng

1. Thêm class vào `samples/Headless/ClassChecks.cs` (`Tailwind.Resolve(...)` rồi so field `Style`).
2. `dotnet run --project samples/Headless -- <out>`: mọi kiểm tra đạt, danh sách "Chưa hỗ trợ" không có class vừa thêm.
3. Utility ảnh hưởng hình vẽ → render PNG và mở xem; kiểm cả renderer GPU nếu đụng tới primitive.
