---
name: gotx-core
description: Change GotX core - frame pipeline, invariants, checks.
metadata:
  origin: user
  version: "1.0.0"
---

# GotX Core

Sửa framework: `src/GotX.Core`, `src/GotX.Raster`, `src/GotX.Platform.Windows`. Không PackageReference, không reflection (AOT), Core không chứa code OS.

## Đường đi của một frame (`Window.Draw`)

```
View.Render → ElementTree (arena Node, handle Div) → Tailwind ClassList (UtilityOp[])
→ LayoutEngine (block / flex / grid / absolute / cuộn) → Paint → Scene (Quad, Shadow, Glyph, Image + ClipShape)
→ SoftwareRenderer (CPU) hoặc D3D11Renderer (GPU)
```

| Muốn sửa | File |
|---|---|
| Vòng frame, cache view | `App/Window.cs`, `Window.Cache.cs`, `Window.Views.cs` |
| Vẽ phần tử | `Window.Paint.cs`, `Window.PaintParts.cs`, `Window.Effects.cs`, `Window.Text.cs`, `Window.Media.cs` |
| Chuột, phím, focus | `Window.Input.cs`, `Window.Pointer.cs`, `Window.Focus.cs`, `WindowTypes.cs` (Hitbox, HoverRegion) |
| Overlay, cuộn | `Window.Overlays.cs`, `Overlay/`, `Window.Scroll.cs` |
| Layout | `Layout/LayoutEngine*.cs` |
| Class Tailwind | `Styling/` (skill `gotx-tailwind`) |
| Chữ, font, atlas | `Text/` |
| Path, ảnh | `Media/` |
| Effect, hẹn giờ | `App/App.cs`, `App.Timers.cs`, `Entity.cs` |
| Móc chẩn đoán | `Diagnostics/`, `Window.Diagnostics.cs` |

## Bất biến phải giữ

- **Hai renderer cho cùng kết quả.** Thêm/sửa primitive → sửa cả `SoftwareRenderer.*.cs` lẫn shader HLSL trong `D3D11Renderer.Shader.cs` (cùng công thức SDF).
- **Phát lại cache.** View không bẩn + cùng bounds → chép primitive/hitbox/hover của frame trước, có thể tịnh tiến. Mọi dữ liệu mới gắn vào hitbox có chỉ số (như `KeyParent`, `ScrollParent`) phải được dời trong `Window.Replay`. Thứ gì đổi hình vẽ mà không qua `Notify` thì phải `MarkViewDirty`.
- **State qua frame** (cuộn, transition, focus) khoá bằng `ElementKey` = (view, `.Key()` hoặc thứ tự). Gọi `ElementKey` đúng **một lần** mỗi node; state mồ côi được dọn trong `Window.State.cs`.
- **Không cấp phát ở frame ổn định.** Struct + arena + lambda static + invoker cache theo kiểu (`ClickInvoker<T>`). Cuộn trang và frame không đổi phải 0 B.
- **Chỉ số glyph** hết hiệu lực khi `GlyphAtlas.Reset()` (Generation đổi) → cache view bị xoá.
- **Variant trạng thái chỉ đổi phần vẽ**, áp lên bản sao `Style` lúc paint, không chạm arena, không đổi layout.
- **Thứ tự hitbox = thứ tự vẽ**; phần tử sau nằm trên; `Blocker` (modal) chặn mọi thứ đứng trước nó.
- Handler chạy trong lúc vẽ (hover, scroll) chỉ đổi state rồi `Notify`; `Draw` tự `Flush` sau đó.

## Quy ước code

- File < 200 dòng: tách partial theo trách nhiệm (`Window.<Việc>.cs`).
- Chú thích tiếng Việt, nói *vì sao*; định danh tiếng Anh. UTF-8 không BOM, LF.
- API công khai mới → cập nhật `README.md` và bảng ở `docs/components.md` §3.
- Giới hạn bộ nhớ có ngưỡng rõ (cache class, wrap, atlas): chạm ngưỡng thì xoá và dựng lại.

## Kiểm chứng (bắt buộc, theo thứ tự)

```
dotnet build GotX.slnx                                  # 0 cảnh báo, 0 lỗi
dotnet run --project samples/Headless -- <out>         # mọi kiểm tra đạt; xem dòng cấp phát mỗi frame
dotnet run --project samples/ComponentsCheck -- <out>  # 0 lỗi, không class lạ
```

- So PNG với lần chạy trước (hash). Ảnh đổi → phải giải thích được vì sao, rồi mở ảnh xem.
- Sửa renderer GPU / Win32: chạy `samples/Hello` hoặc `samples/Components` trên cửa sổ thật. Chụp cửa sổ D3D11 (kể cả khi màn hình khoá): `PrintWindow(hwnd, dc, PW_CLIENTONLY | PW_RENDERFULLCONTENT)`.
- Đụng tới trim/AOT: `dotnet publish samples/Hello -r win-x64 -c Release` không có cảnh báo, chạy thử exe.
- Thêm tính năng → thêm kiểm tra đo hành vi thật (không mock) vào bộ test.
