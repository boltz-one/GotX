# Ghi công bên thứ ba

GotX không có PackageReference nào và không chứa mã nguồn chép nguyên từ dự án khác. Các phần dưới đây dẫn xuất
từ, hoặc dùng dữ liệu của, những dự án mã nguồn mở sau. Giấy phép của chúng yêu cầu giữ ghi công này.

## font-rs — Apache-2.0 / MIT

Thuật toán raster tích luỹ diện tích có dấu (cộng diện tích phủ của từng đoạn thẳng vào bộ đệm rồi cộng dồn theo
hàng để ra độ phủ anti-alias) trong `src/GotX.Core/Text/GlyphRasterizer.cs` được viết lại bằng C# theo thuật toán
của font-rs (https://github.com/raphlinus/font-rs). Cũng dùng cho hình vector (`src/GotX.Core/Media/PathRasterizer.cs`).

## Tailwind CSS — MIT

Bảng màu, thang spacing, cỡ chữ, bo góc, bóng đổ, breakpoint và tên utility trong `src/GotX.Core/Styling/`
(`Theme.cs`, các file `Tailwind*.cs`, `Styled.g.cs`, `Tw.g.cs`) lấy theo Tailwind CSS (https://tailwindcss.com),
bản quyền Tailwind Labs, Inc.

## shadcn/ui — MIT

Bộ component trong `src/GotX.Components` tái hiện thiết kế, tên variant và chuỗi class của shadcn/ui
(https://ui.shadcn.com); token màu trong `Core/ComponentTheme.cs` theo theme neutral của shadcn/ui. Ví dụ
`samples/Shadcn` port trang chủ và các block của ui.shadcn.com (văn bản, dữ liệu mẫu, bố cục); ảnh tham chiếu trong
`samples/Shadcn/screenshots/reference/` là ảnh chụp trang đó, chỉ dùng để đối chiếu. `samples/Shadcn/Home/Assets/full-{light,dark}-896.png` là bản thu nhỏ của `apps/v4/public/images/full-*.png` trong repo shadcn-ui (MIT). Bản quyền shadcn.

## Hugeicons — MIT

Dữ liệu path của các icon trong `samples/Shadcn/Home/HugeIcons.cs` lấy từ `@hugeicons/core-free-icons` 4.4.0 (https://hugeicons.com), bản quyền Hugeicons.

## Lucide — ISC

Dữ liệu path của các icon trong `src/GotX.Components/Core/IconPaths*.cs` và các file icon trong `samples/Shadcn`
theo bộ icon Lucide (https://lucide.dev), bản quyền Lucide Contributors. Một phần icon của Lucide dẫn xuất từ
Feather (MIT, bản quyền Cole Bemis).
## Geist / Geist Mono (fonts)

- Files: `samples/Shadcn/Assets/fonts/Geist-{Regular,Medium,SemiBold,Bold}.ttf`, `GeistMono-{Regular,Medium}.ttf` (static TTF, unmodified).
- Source: https://github.com/vercel/geist-font — release v1.9.2 (`geist-font-v1.9.2.zip`).
- Copyright 2026 The Geist Project Authors (https://github.com/vercel/geist-font).
- License: SIL Open Font License 1.1 — full text in `samples/Shadcn/Assets/fonts/OFL.txt`.
- Used by the shadcn/ui port (`samples/Shadcn`) to match the typography of ui.shadcn.com.

---

Toàn văn các giấy phép (Apache-2.0, MIT, ISC) có tại trang của từng dự án nêu trên; giấy phép OFL của Geist nằm cạnh file font.
