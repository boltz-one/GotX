# Hiệu năng – nhật ký, công cụ đọc, bench

Ba phần: **nhật ký hiệu năng** (`PerfLog`, trong Core), **`tools/GotX.Perf`** (đọc log, so sánh, bench kịch bản headless, chạy cửa sổ thật),
và vòng lặp tối ưu (đo → chọn chi phí lớn nhất → sửa → kiểm chứng → đo lại).

## 1. Nhật ký (`GOTX_LOG`)

```
set GOTX_LOG=C:\tmp\gotx.jsonl            :: bật cho mọi sample (Application.Run, Hello, Components, Shadcn…)
set GOTX_LOG_LEVEL=frames                 :: slow | frames (mặc định) | trace
```

Hoặc bằng API: `window.Log = new PerfLog(path, PerfLogLevel.Frames)` / `PerfLog.Shared = …` (gán trước khi tạo Window).
`Window.Log = null` tắt. Tắt thì mỗi điểm móc chỉ là một phép so sánh null; bật thì bản ghi được viết tay thẳng vào bộ đệm byte dựng sẵn
(không cấp phát khi frame ổn định), luồng nền ghi file theo lô; luồng UI không bao giờ chờ (đầy thì bỏ bản ghi, đếm ở `PerfLog.Dropped`).

Mức: `slow` chỉ ghi frame > 16.7 ms hoặc input→frame > 50 ms (cộng khởi động / cảnh báo / mark); `frames` mỗi frame một dòng (top 5 view render);
`trace` ghi đủ tên view render (≤ 48) và mọi nguyên nhân bẩn.

Mỗi dòng là một JSON object, khoá `t`:

| `t` | Trường |
|---|---|
| `frame` | `n` số frame, `win`, `wall` giây từ lúc mở log, `dt` ms từ Draw trước, `draw` tổng ms, `layout`, `paint`, `rv` (tổng thời gian `View.Render`), `wrap` ms + `wrapHit`/`wrapMiss` (cache ngắt dòng), `glyph` ms + `glyphN` (raster glyph mới), `clsMiss`/`cls` (biên dịch class Tailwind mới), `meas`/`memo` (số lần `Measure` / trúng memo), `rend`/`repl` view render / phát lại, `views` = [tên, µs, …] xếp theo thời gian, `cause` ⊂ {Notify, Hover, Focus, Scroll, Animation}, `dirty` tên view bị đánh dấu bẩn, `clr` lý do dựng lại toàn bộ (scale, cache-off, breakpoint, atlas, dark, invalidate, resize), `nodes`, `prim`, `clips`, `grad`, `hit`, `hov`, `alloc` byte, `gc` [gen0, gen1, gen2] trong frame, `wsMB`/`heapMB`/`atlasKB` mỗi 60 frame, `in` = [loại input, số lượng, …] từ frame trước, `lat` ms input→hết Draw, `anim`, `w`/`h`/`sc`, `slow:1` |
| `present` | `n`, `win`, `ms` (raster CPU hoặc nộp GPU + Present), `build` (CPU: raster; GPU: dựng instance + tải atlas), `submit`, `flip` (Present, chờ vsync), `gpu` (timestamp query D3D11, trễ vài frame), `upBytes` (byte tải lên GPU) |
| `start` | `what` ∈ process-to-launch, fonts, first-frame, …; `ms` |
| `warn` | `kind` ∈ atlas-reset, cache-cleared, effect-loop; `msg`, `n` |
| `mark` | `name`, `detail` – do harness/app đặt bằng `PerfLog.Mark` (bench dùng để chia kịch bản) |

## 2. `tools/GotX.Perf`

```
dotnet run --project tools/GotX.Perf -- report <log> [--top N] [--phases]
dotnet run --project tools/GotX.Perf -- compare <a.jsonl> <b.jsonl>
dotnet run --project tools/GotX.Perf -- bench [--repeat 3] [--only stress,home] [--raster] [--update-baseline] [--verbose]
dotnet run --project tools/GotX.Perf -- real <shadcn|gallery|exe> [--seconds 10]
```

- `report`: min/median/p95/p99/max từng pha, frame chậm + nguyên nhân, view render nhiều nhất, cấp phát/frame, GC, glyph/atlas, cảnh báo; `--phases` chia theo `mark`.
- `compare`: B so với A, tổng thể và theo pha.
- `bench`: mỗi target × mỗi lần lặp chạy trong **tiến trình con mới** (một lần cold với JIT mặc định → đo khởi động; một lần steady với `TieredCompilation=0`
  để đo mã đã tối ưu như Native AOT), đồng hồ ảo, kích thước cố định, font Geist. Target: `demo` (Headless), `gallery-{display,forms,overlays,navigation,chat,data}`,
  `shadcn-home` + từng block, `stress` (hàng nghìn node, chữ dài, bảng 150×8, danh sách 400 dòng). Kịch bản: cold, idle, hover (quét chuột), scroll (bánh xe), type,
  click (nhấp ≤ 24 chỗ + Esc: overlay/menu/select/dialog), resize (đổi breakpoint + kéo cửa sổ), dark. Gộp các lần lặp bằng MIN với số đo thời gian (nhiễu của máy dùng chung chỉ làm tăng thời gian) và trung vị với cấp phát / render mỗi frame, rồi so với
  `perf-baseline.json`: lệch quá dung sai từng chỉ số (draw median +15 % và p95 +25 % kèm ngưỡng tuyệt đối, alloc +10 %, render/frame +5 %, khởi động +25 %) thì mới coi là nghi ngờ.
  Mỗi tiến trình con đo một phép **hiệu chuẩn** CPU cố định; mốc baseline được nhân theo tỉ lệ hiệu chuẩn (chỉ nới khi máy chậm / bận hơn lúc ghi baseline, không bao giờ siết khi máy nhanh hơn: phép hiệu chuẩn không theo kịp tải bộ nhớ),
  và target nghi ngờ được **đo lại** tới 2 lần – chỉ regression sống sót qua đo lại mới làm thoát mã 1.
  `--raster` thêm raster CPU mỗi frame (chậm hơn nhiều), `--scenarios a,b` chỉ chạy vài kịch bản, `--update-baseline` ghi lại các entry đã chạy
  (baseline ghi trên máy này; ghi lại sau khi đổi máy). Luôn đo bản **Release** (`-c Release`): bản Debug không tối ưu và cho số đo sai lệch hẳn.
- `run-target <target> --log f [--scenarios a,b] [--alloc-types] [--trace]`: chạy một target trong tiến trình hiện tại; `--alloc-types` in loại đối tượng chiếm lượng cấp phát (EventListener `GCAllocationTick`).
- `profile-raster <target> [--scroll N]`: raster CPU từng primitive một cho frame cuối, in tổng theo loại và các primitive đắt nhất (tìm bóng/viền/clip bệnh lý mà tổng frame che mất).
- `real`: chạy sample thật (D3D11) với `GOTX_LOG`, đẩy input bằng `PostMessage` (cuộn, quét chuột, nhấp, phím), chụp bằng `PrintWindow(PW_CLIENTONLY | PW_RENDERFULLCONTENT)`
  – được cả khi khoá màn hình – rồi in report.

## 3. Quy trình tối ưu

1. `bench` (hoặc `real`) → đọc report → chọn chi phí lớn nhất (đo trước, đừng đoán).
2. Sửa; kiểm chứng đầu ra giữ nguyên: `samples/Headless` (140 kiểm tra, hash PNG), `samples/ComponentsCheck` (hash PNG trang), `tests/GotX.Tests`, ảnh `samples/Shadcn --screenshots`.
3. `bench --only …` so với baseline; ghi trước/sau. Test hồi quy hiệu năng nằm ở `tests/GotX.Tests/Perf/`.

## 4. Kết quả (Release, máy dùng chung nhiều tiến trình – số đo nhiễu ±15 %)

Trước/sau cùng một cây nguồn (chỉ khác phần tối ưu Core/Raster/Platform), trung vị 5 lần chạy tiến trình mới, `bench` (không raster), trung vị / p95 của `Window.Draw` (ms):

| Kịch bản | Trước | Sau |
|---|---|---|
| demo / cuộn | 0.34 / 0.77 | 0.23 / 0.55 |
| gallery-display / cuộn (có animation) | 0.78 / 1.10 | 0.58 / 0.64 |
| gallery-forms / click | 0.29 / 0.42 | 0.21 / 0.30 |
| shadcn-home / cuộn | 1.23 / 1.60 | 1.03 / 1.18 |
| shadcn-dashboard-01 / cuộn | 1.41 / 1.93 | 1.17 / 1.38 |
| stress / cuộn | 5.09 / 6.81 | 3.93 / 5.62 |
| stress / đổi kích thước | 8.47 / 24.2 | 6.55 / 20.6 |

Trung bình nhân của tổng thời gian Draw sau/trước trên 16 target: click 0.82, dark 0.81, hover 0.88, idle 0.92, resize 0.83, scroll 0.86, type 0.84.

Khởi động (Native AOT, `Hello`): từ tiến trình tới frame đầu được present 190 ms → 81 ms. Phần lớn là biên dịch HLSL (100 ms, O3, hai entry point); bytecode giờ được cache ở
`%LOCALAPPDATA%\GotX\shader-cache` theo hash mã nguồn (lần đầu biên dịch vs/ps song song rồi ghi file, các lần sau 2 ms).

Đã làm (đo trước, kiểm chứng ảnh PNG giống từng bit): tra cmap theo trang 256 ký tự, bảng advance, cache kerning băm nhân, `FontFor` theo (họ, đậm, nghiêng), cache ngắt dòng tầng tham chiếu chuỗi,
bảng glyph→atlas theo (font, cỡ), `ClassList.ApplyBase/ApplyState` một lượt + thoát sớm, `Tailwind.Get` tầng tham chiếu; renderer CPU: tô nhanh dưới vùng cắt theo hình, bóng đổ nhớ alpha theo hàng/cột
+ cắt ngoài vùng chuyển tiếp, bảng blend cho màu lặp; D3D11: cache shader.

Đã thử, không đáng: cache theo token trong `Tailwind.Parse` (class động có token duy nhất mỗi frame nên không trúng); đường nhanh HLSL cho ruột hộp và cắt sớm erf của bóng (A/B ba lần trên GPU của máy này:
không khác biệt đo được, đã bỏ).

Chưa làm (đã đo là còn đáng kể): giữ cây element qua frame để đổi kích thước không phải `Render` lại mọi view (đổi kích thước ~9 ms/frame trên trang chủ shadcn); phát lại view có `overflow-hidden` bị cắt ở mép khung cuộn thay vì render lại; thu nhỏ `Primitive` (200 byte) để giảm chi phí sao chép khi phát lại trang lớn; chạy đoạn thẳng của bóng đổ theo dải trong renderer CPU.

Giới hạn khi đo GPU: máy chạy thử không có GPU thật (Microsoft Basic Display Adapter), nên `gpu` ~13–18 ms/frame ở 1440×900 là thời gian của bộ raster phần mềm của driver (quad ≈ 8 ms, bóng ≈ 5 ms,
glyph < 1 ms); số này chỉ hữu ích để so sánh tương đối, không đại diện cho GPU rời/tích hợp.

## 5. GPU: độ bền, parity, bộ đếm, vùng hỏng

**Mất thiết bị** (`D3D11Renderer.Render`): `DXGI_ERROR_DEVICE_REMOVED/RESET/HUNG/DRIVER_INTERNAL_ERROR` ở `Present`, `ResizeBuffers`, `Map`, tạo buffer, hoặc `GetDeviceRemovedReason` sau mỗi frame (draw/copy không trả HRESULT)
→ dựng lại thiết bị + swap chain + shader + buffer + atlas, vẽ lại đúng Scene, cửa sổ vẫn sống. Quá 3 lần liên tiếp không có frame thành công → `Failed` và `Win32Window` chuyển sang `SoftwareRenderer`.
Atlas glyph/ảnh GPU trống sau khi dựng lại và nạp theo nhu cầu; mask nằm ở `GlyphAtlas` CPU nên chỉ số glyph của cache view vẫn đúng (không phải đổi `Generation` của atlas CPU).
Test (`tests/GotX.Tests/GpuRobustnessTests.cs`): HRESULT lỗi được tiêm qua seam `InjectHr/InjectCount` (driver thật không thể bị rút theo ý muốn), còn dựng lại và pixel sau đó là thật (WARP).

**Parity CPU↔GPU** (`GpuParityTests.cs`): cùng Scene vẽ bằng `SoftwareRenderer` và D3D11 WARP offscreen (`D3D11Renderer.TryCreateOffscreen` + `ReadPixels`), lệch kênh ≤ 3 trên ≥ 99.5 % pixel.
Scene dựng tay (viền từng cạnh / gạch / chấm, viền trong suốt, gradient, bóng + vùng loại trừ, vùng cắt hình, biến đổi, ảnh) + chữ thật + các trang gallery. Thực tế ≥ 99.99 % pixel trong ngưỡng, lệch tối đa ≤ 2 trừ vài pixel ở mép gạch/chấm.

**Bộ đếm GPU** (`GpuCounters.Last`, ghi vào `present` của `GOTX_LOG` và `bench --gpu`): `draws` lệnh vẽ, `pipe` lần đổi state, `texsw` lần đổi texture ảnh, `instB` byte instance/vùng cắt/gradient, `atlasHit` tỉ lệ trúng atlas, `enc` ms encode CPU,
`gpu` ms GPU (nếu đo được). `bench --gpu` dùng WARP offscreen thay raster CPU; `--gpu --update-baseline` chỉ ghi các trường `gpu*` (thông tin) vào baseline, không đụng số đo thời gian và cổng hồi quy.

**Bộ nhớ có trần**: atlas glyph bắt đầu 512² (256 KB) và nở gấp đôi tới 2048² (4 MB) khi đầy (glyph lớn hơn atlas làm nở atlas; lớn hơn trần thì bỏ qua – trước đây ghi ngoài biên làm WARP mất thiết bị);
buffer instance co về gấp đôi mức cần sau 120 frame dùng dưới 1/4; texture ảnh bị đuổi sau 600 frame không vẽ hoặc khi tổng vượt 64 MB. `D3D11Renderer.GpuBytes` (ước tính tài nguyên) và `VideoMemory` (DXGI `QueryVideoMemoryInfo`).

**Vùng hỏng** (prototype, **mặc định tắt**; `GOTX_DAMAGE=1` hoặc `D3D11Renderer.Damage = new SceneDamage()`): `SceneDamage` (Core/Scene) so hash + hộp nhìn thấy (gồm blur bóng, vùng cắt, biến đổi) của frame trước với frame này, đánh dấu ô 64² hỏng; đổi thứ tự vẽ → vẽ cả frame.
CPU: `SoftwareRenderer.RenderDamaged` vẽ lại các ô hỏng vào canvas giữ lại. D3D11: vẽ có scissor vào texture đích giữ lại rồi `CopyResource` sang back buffer – **ràng buộc flip-model**: `FLIP_DISCARD` xoay back buffer và nội dung sau Present không xác định nên không thể chỉ vẽ phần đổi thẳng vào back buffer.
Frame không đổi: không vẽ, không Present. `GpuDamageTests.cs`: chuỗi hover / nháy / cuộn / animation cho ảnh giống từng pixel vẽ cả frame (CPU và GPU).


## 6. Cập nhật một phần (kiểu React): chỉ phần đổi được dựng lại và vẽ lại

### 6.1 Phase 0 – phân tích (trước khi sửa; Release, `bench`, trung vị mỗi frame)

`rend`/`repl` = view render / phát lại, `meas` = số lần Measure, `prim` = primitive của scene, `nodes` = node dựng trong frame.

| target / kịch bản | frame | rend | repl | meas | prim | nodes | draw ms | layout | paint | alloc B |
|---|---|---|---|---|---|---|---|---|---|---|
| demo / idle,hover,scroll | 60-461 | 3 | 5 | 589 | 3125 | 186 | 0.27 | 0.15 | 0.09 | 0 |
| gallery-display / idle (spinner) | 60 | 2 | 0 | 1783 | 2266 | 680 | 0.70 | 0.42 | 0.22 | 144 |
| gallery-forms / type | 42 | 4 | 37 | 595 | 1272 | 225 | 0.26 | 0.16 | 0.07 | 728 |
| gallery-forms / resize | 78 | 54 | 0 | 1707 | 1271 | 528 | 1.16 | 0.70 | 0.43 | 64 |
| shadcn-home / idle (không có gì đổi) | 60 | 0 | 1 | 0 | 2429 | 1 | 0.04 | 0 | 0.03 | 0 |
| shadcn-home / scroll | 82 | 33 | 0 | 1635 | 2429 | 700 | 1.08 | 0.03 | 1.00 | 56 |
| shadcn-home / type | 42 | 9 | 7 | 982 | 2435 | 390 | 0.52 | 0.36 | 0.13 | 760 |
| shadcn-home / resize | 112 | 61 | 2 | 2070 | 1466 | 801 | 9.62 | 8.72 | 1.04 | 839 784 |
| dashboard-01 / type | 42 | 39 | 22 | 3749 | 1541 | 509 | 1.23 | 0.98 | 0.18 | 2000 |
| stress / idle,hover,click | 3-85 | 0 | 1 | 0 | 35751 | 1 | 2.07 | 0 | 2.05 | 0 |
| stress / scroll | 87 | 4 | 1 | 15734 | 35753 | 4152 | 4.50 | 0.44 | 3.79 | 808 |
| stress / resize | 78 | 5 | 0 | 15794 | 33663 | 4181 | 8.33 | 3.82 | 4.13 | 7312 |

Kết luận có bằng chứng:

1. Frame rảnh / hover / click vẫn chép lại cả scene dù không có gì đổi (stress 2 ms = 35k primitive × 200 B).
2. Một Notify render lại mọi view cha (gõ phím: 4-39 view, 0.26-1.23 ms thay vì 1 view) và đo lại cả cây của chúng (595-3749 Measure).
3. Animation (gallery-display) render lại 2 view và đo 1783 node mỗi frame.
4. Gõ phím cấp phát 664-2000 B/frame: shape cache nhận khoá ở lần thứ hai thấy (xảy ra ngay trong frame: đo + vẽ) và bộ đệm định hình nở đúng bằng độ dài chữ mỗi phím.
5. Đổi kích thước: Render lại 54-61 view/frame (7 ms trên trang chủ) và ngắt dòng lại ~200 đoạn/frame (1.5 ms trên 8 ms ở stress).
6. Cuộn trang chủ: 33 view render lại vì view có vùng cắt trong bị cắt ở mép khung cuộn không phát lại được (đã ghi ở §4 "chưa làm").

### 6.2 Cơ chế đã làm (Phase 1-5)

- **Notify chỉ làm bẩn view đó** (`Window.DirtyAncestors` / `GOTX_DIRTY_CHAIN=1` trả lại hành vi cũ). Focus ô nhập vẫn làm bẩn view cha (component cha đọc `IsFocused` trong Render, vd. `Ui.InputGroup`).
  Hệ quả: view cha đọc state của con trong Render mà không `Observe` sẽ không tự cập nhật nữa.
- **Khung một phần** (`Window.Partial.cs`, `Window.Splice.cs`): view bẩn cao nhất được Render + layout + vẽ vào scene tạm, rồi đoạn primitive / hitbox / hover / thứ tự view / bảng cache cũ của nó được thay TẠI CHỖ
  (`Scene.ReplaceRange`; Start/End của tổ tiên và phần đuôi dời theo độ lệch; chỉ số hitbox bao ngoài đổi sang toàn cục). Điều kiện (relayout boundary): mọi kết quả đo + min-content của view khớp với lần cha đã thấy, số vùng cắt hình / gradient
  và cờ cắt trong không đổi, view không phải vùng cuộn, không phủ quá 60 % scene. Không thoả thì dựng khung đầy đủ, dùng lại kết quả Render đã có (không Render hai lần). Frame không có view bẩn không làm gì (scene giữ nguyên).
- **Vùng hỏng theo đoạn** (`FrameDamage`, Phase 3): khung một phần ghi hộp (kể cả blur bóng, vùng cắt, biến đổi) của primitive thật sự đổi (bỏ tiền tố / hậu tố giống hệt); khung đầy đủ đánh dấu Full; `SceneDamage` đổi sang lưới ô 64² không hash scene.
  **Mặc định bật** (CPU: canvas giữ lại; D3D11: đích giữ lại + scissor). `GOTX_DAMAGE=0` tắt, `GOTX_DAMAGE=hash` dùng so hash cũ.
- **Cô lập** (Phase 4): con trỏ nháy / animate-* / hover chỉ Render view chứa chúng; nháy con trỏ ≤ 2 trên 88 ô hỏng; spinner 1 view/frame, ≤ 2 ô.
- **Bộ nhớ** (Phase 5): shape cache chỉ nhận khoá ở frame khác; bộ đệm định hình nở gấp đôi; cache ngắt dòng theo **khoảng bề rộng** (kết quả đúng cho mọi bề rộng trong [dòng rộng nhất, bề rộng nhỏ nhất làm một từ bị ngắt lọt vào dòng trước));
  `Scene.Reserve` (hai scene thay phiên + scene tạm cấp một lần, không nở dần); bảng snapshot view được dọn khi view biến mất.
- Sửa kèm: transition kết thúc đúng từng bit trên đích; cuộn mượt bắt đầu bằng đúng một frame (trước đây phụ thuộc việc view cha có bị vẽ lại hay không).

Test mới (`tests/GotX.Tests/Perf`):

- `PartialParityTests`: chuỗi input ngẫu nhiên có seed trên 8 trang. Cửa sổ bật so cửa sổ tắt khung một phần (primitive, vùng cắt, hitbox, con trỏ) khớp từng phần tử; canvas giữ lại vẽ theo hint bằng vẽ cả frame từng pixel (renderer bỏ qua frame ngẫu nhiên, hint phủ kín ô hỏng của so hash).
- `IsolationTests`: nháy con trỏ, spinner. `WrapSpanTests`: 2400 cặp chuỗi/bề rộng bằng tính trực tiếp; kéo giãn 0 miss, ≤ 256 B. `ZeroAllocTests`: gõ 150 phím ≤ 32 B/frame.
- Các test cũ ghim "view cha cũng render" / "frame rảnh phát lại view gốc" đã cập nhật theo ngữ nghĩa mới.

### 6.3 Số đo trước → sau (Release, `bench`, trung vị, cùng máy)

| kịch bản | views/frame | draw ms | alloc B/frame |
|---|---|---|---|
| gallery-forms / type | 4 → 1 | 0.26 → 0.05 | 728 → 0 |
| shadcn-home / type | 9 → 5 | 0.52 → 0.13 | 760 → 0 |
| dashboard-01 / type | 39 → 1 | 1.23 → 0.05 | 2000 → 0 |
| idle (stress / home) | 0 | 2.07 → 0.01 / 0.04 → 0.01 | 0 |
| hover (stress) | 0 | 2.13 → 0.01 | 0 |
| click (dashboard-01) | 3 → 1 | 0.20 → 0.08 | 0 |
| stress / resize | 5 | 8.33 → 5.98 (baseline 6.16) | 7312 → 10168 (xem §6.4) |
| spinner trong view riêng (IsolationTests) | 1 | 0.03 trên 1159 primitive | 0 |

Footprint (`tools/GotX.Footprint run --renderer software --repeat 1`, trước = `footprint-baseline.json`, JIT): home rảnh CPU 1.20 % → 0.00 %, 1584 → 0 B/frame, 2.1 → 0 frame/giây;
spin CPU 0.90 % → 0.41 %; dialog 1.80 % → 1.02 %; resize 5.63 % → 5.42 %, 4022 → 2891 B/frame; input 0.13 % → 0.09 %; vlist 2.67 % → 2.63 % (B/frame 1594 → 2944 do chuỗi nhãn dòng mới của kịch bản, không phải của Core).
Working set không đổi đáng kể (home 79 MB, hello 46 MB).

### 6.4 Còn đỏ trong cổng bench (4 mục) và nút cổ chai còn lại

- Đã sửa (vòng tối ưu sau): `stress/resize allocMed` 10 168 → 808 B: glyph của ô cache định hình giờ nằm trong ≤ 6 khối dùng chung 16k glyph (bump allocation, tái dùng vòng tròn, ô trỏ vào khối bị ghi đè hết hiệu lực nhờ số thế hệ) thay vì mảng riêng mỗi ô (`TextSystem.ShapeCache.cs`, đường nóng đọc `Text.GlyphSpan`).
- Đã sửa: `shadcn-home/dark` drawP95 4.5 → 1.9 ms. Nguyên nhân thật: cache class Tailwind xoá HẾT khi chạm 4096, mà trang có class sinh động dùng một lần (~195 chuỗi mới mỗi frame resize, 8760 chuỗi khác nhau cả phiên) nên tập class đang dùng bị biên dịch lại cùng frame (74 lần trượt ~1 ms ở frame đổi theme, 2.3 ms mỗi frame resize). Nay hai thế hệ (`s_cache`/`s_old`): chạm ngưỡng thì thế hệ hiện tại thành cũ, trúng ở thế hệ cũ thì chuyển lên. `shadcn-home/resize` drawMed 8.5 → 5.1 ms.
- `shadcn-sidebar-03/dark` drawP95 0.44 → 0.99: chi phí một lần mỗi tiến trình (JIT các đường transition / vẽ lại toàn bộ trong frame đổi theme đầu tiên, paint 0.9 ms; lần đổi thứ hai 0.2 ms), không phải nhiễu và không phải chi phí ổn định. Bench nay đổi theme một vòng trong warm-up (`ChildRun`), phần khởi động vẫn do kịch bản cold đo.
- Đã sửa: `DashboardAreaChart` (có trong trang chủ shadcn và dashboard-01; kéo giãn dựng hình mới mỗi kích thước): Render 7-15 ms → 0.7 ms, shadcn-home/resize drawMed 5.5 → 1.8 ms, allocMed 662 KB → 1.9 KB, dashboard-01/resize 1.4 → 0.5 ms. Nguyên nhân thật không phải Notify mỗi frame (frame rảnh 0 render) mà là ~300 chuỗi class chưa từng gặp mỗi kích thước (biên dịch Tailwind ≈ 60 % chi phí dựng): lưới / nhãn / cột chạm / dải gradient nay đặt top-left-w-h thẳng vào `Style` (chỉ màu còn đi qua class, độ trong suốt lặp lại nên trúng cache), `Length` quy sẵn lúc dựng, cắt dải dùng hai bộ đệm chung thay vì 4 danh sách mỗi dải, bảng hình có trần 64 (trước đây nở mãi theo kích thước). Ảnh `--screenshots` giống từng bit.
- `stress/resize`: layout còn 2.4 ms với 15 794 Measure / 4181 node (3.8 lần mỗi node), paint 3.1 ms. Cần giữ cây element qua frame để đổi kích thước không Render lại mọi view (chưa làm).
- Trang có animation nằm thẳng trong view lớn (gallery-display 0.63 ms, 2 view, 1783 Measure mỗi frame): view phủ trên 60 % scene đi đường khung đầy đủ. Tách spinner thành view riêng đưa về 0.03 ms.
- Cuộn trang chủ shadcn: 33 → 6 view render lại / frame, drawMed 1.05 → 0.60 ms. Nguyên nhân: cờ `InnerClip` chỉ có một bit nên mọi view chứa `overflow-hidden` không nằm trọn trong khung (cả thẻ nằm hẳn dưới trang) bị Render lại khi dời, và view bị đổi vùng cắt ngoài / hình bao (thẻ bo góc cuộn cùng nội dung) không phát lại được. Nay `ClipAcc` (`Window.ClipFlags.cs`) ghi: Cut (vùng cắt đang bị xén, nhìn thấy), Hid (hợp các vùng cắt nằm hẳn ngoài) và In (hợp các vùng cắt nằm trọn trong); phát lại khi dời / đổi vùng cắt ngoài nếu không Cut, Hid vẫn ngoài và In vẫn trọn trong vùng cắt ngoài mới; hình bao ngoài so sau khi dời cùng view. Chỉ view vắt qua mép khung mới phải Render lại. `ReplayParityTests` so ảnh với cửa sổ tắt cache (logic cũ lệch ảnh ở chuỗi cuộn ngẫu nhiên: view từ ẩn thành hiện nhưng vẫn phát lại).
- Relayout boundary: ranh giới hỏng (kích thước / min-content / vùng cắt / gradient đổi) nay thử view tổ tiên gần nhất đủ điều kiện (có cache, không cuộn, ≤ 60 % scene) vẽ lại từng phần thay vì dựng cả frame; Render của view con tái dùng (`_pre`), không Render hai lần. Hiếm gặp: 12 lần lùi vì ranh giới trong cả bench (10 ở gallery-navigation, 2 ở dashboard-01), 2 lần trong 2×8 chuỗi ngẫu nhiên; mọi lần lùi còn lại là `large-view`. `BoundaryPropagationTests`.
- Chưa làm: segment dạng danh sách gián tiếp (đoạn đổi độ dài vẫn dời phần đuôi bằng `Array.Copy`), khung một phần cho view cuộn / view phủ trên 60 % (nguyên nhân của hầu hết `pfb: large-view`; cuộn trang vẫn Render chủ của vùng cuộn), hover dưới con trỏ sau khi nội dung dời đi cập nhật trễ một nhịp so với vẽ mới.
- Chưa giải thích: `ReplayParityTests` bỏ trang chat (glyph icon 18px lệch 1px sau phát lại, có từ trước) và overlays (mở overlay theo hover khác giữa phát lại và vẽ mới ở chuỗi dài, phụ thuộc trạng thái dùng chung của tiến trình).
- `GOTX_PARITY_FULL=1` có hiệu lực (80 s Release so với 10 s mặc định: ~4900 so với ~150 frame so pixel của damage; partial 90 × 2 seed so với 20 × 1 seed mỗi trang); bản Debug ~17 phút.
- `gallery-overlays/click` drawP95 3.1 → 0.2 ms: chi phí một lần của tiến trình (lần mở đầu của mỗi loại overlay 4-20 ms: JIT + biên dịch class + raster glyph; lần mở thứ hai < 1 ms, đo bằng cách nhấp hai lượt). Bench nay mở mọi overlay một lượt trên cửa sổ bỏ đi trước khi đo `click` (`ChildRun.WarmClick`); cold vẫn đo khởi động. `Tailwind.Combine` dùng hai thế hệ như cache class (`ClassCombineCacheTests`).

### 6.5 Rủi ro chưa kiểm chứng trên cửa sổ D3D11 thật (máy chỉ có WARP)

- Vùng hỏng bật mặc định cho D3D11: đích giữ lại + `CopyResource` sang back buffer flip-model và scissor chỉ được test pixel-exact trên WARP offscreen (`gpu/damage`), chưa trên swap chain / driver thật. Mất thiết bị đặt lại về Full qua `Damage.Invalidate()`. Thoát hiểm: `GOTX_DAMAGE=0`.
- Frame không có gì đổi giờ trả scene cũ và renderer bỏ qua Present: hành vi chờ vsync / DWM với cửa sổ không Present chưa quan sát được trên máy thật.
- Khung một phần sửa scene tại chỗ: mã giữ tham chiếu `Scene` của frame trước qua Draw kế tiếp sẽ thấy nó đổi (trong repo chỉ có `SceneDamage` / renderer dùng, cả hai không giữ).

## 7. Vòng tối ưu raster CPU + bộ nhớ (đo bằng `tools/GotX.Stress`)

Bằng chứng ban đầu: raster phần mềm đứng đầu bảng hot spot ở hầu hết kịch bản. Nguyên nhân thật (đo trước khi sửa):

1. **Rig stress vẽ cả frame mỗi frame** (`SoftwareRenderer.Render`), trong khi `Win32Window` dùng `SceneDamage` + `RenderDamaged` vào canvas giữ lại. Rig nay đi cùng đường đó (`STRESS_FULL_RASTER=1` để so A/B).
2. **Khung đầy đủ luôn đánh dấu Full dù chỉ đổi vài primitive**: view lớn bị hover/click (`large-view`), ranh giới layout hỏng (`size`), view phát lại từ cache → hint Full → vẽ lại cả frame (churn 85 %, input 98 % frame Full). Sửa: `SceneDamage` khi hint Full mà bề mặt không đổi thì so hash từng primitive với frame trước (`SceneDamage.Fallback.cs`; hash 4 làn; lùi dần 2..64 frame với scene ≥ 20k primitive khi liên tiếp phủ ≥ 80 %; không hash khi kích thước đổi; so theo vị trí nếu cùng số primitive, từ điển nếu lệch số lượng và ≤ 16k primitive).
3. **`RenderDamaged` quét mọi primitive cho từng dải ô** (O(dải × primitive), `Visual` tính lại mỗi lần). Nay gộp ô thành hình chữ nhật (dải ngang + nối dọc), xoá một lượt, rồi MỘT lượt qua primitive vẽ mỗi primitive vào các hình chữ nhật nó chạm; dùng lại hộp đã tính khi so hash (`Result.Boxes`).
4. **Quad tính SDF từng pixel ở hàng sát cạnh** (nền trang / thẻ có viền: 2 hàng × cả bề rộng). Nay `QuadShader` (tách khỏi vòng lặp) + tô đoạn trong bằng `Span.Fill` + hàng sát cạnh tính MỘT pixel đại diện rồi lặp dọc đoạn giữa (an toàn khi hình thẳng trục, không gradient / vùng cắt hình, viền liền cùng màu: kết quả chỉ phụ thuộc y, chứng minh ở chú thích `QuadShader`); bốn cạnh khác màu bị loại vì `Side()` đổi theo x ngay ở hàng sát mép (test ngẫu nhiên bắt được). `Blend` dùng bảng `k / 255f` (đúng từng bit).

Đúng từng pixel: 195 PNG của `Headless` + `ComponentsCheck` có hash trùng bản raster cũ (HEAD); test mới `perf/raster/quad-fast-paths-match-per-pixel-sdf` (300 cảnh ngẫu nhiên, `SoftwareRenderer.QuadFastPaths = false` là đường SDF gốc) và `damaged-tiles-match-full-raster` (150 cảnh × lưới ô ngẫu nhiên), cộng bộ `perf/damage/*` có sẵn.

Raster ms/frame (rig cũ vẽ cả frame → rig mới + sửa): tree-10k 2.59 → 0.95, vlist-100k 2.74 → 0.40, churn 4.15 → 0.60, input 4.70 → 3.22 (-31 %; cuộn phủ ~76 % diện tích), resize-dpi 4.72 → 2.4, multiwindow 2.98 → 0.68. Cuộn trang lớn vẫn gần vẽ cả frame vì mọi pixel đổi.

**Bộ nhớ**
- `Canvas.Resize` cấp lại mảng LOH (hàng chục MB, gen2) gần như mỗi frame khi kéo giãn (kích thước chỉ nở dần): resize-dpi 1.08 MB/frame và gen2 = 7 → 180 KB/frame, gen2 = 0. Nay cấp dư 25 % và chỉ thu nhỏ khi lớn gấp 4 lần nhu cầu suốt 120 lần Resize liên tiếp. (Lỗi nằm ở Core/Raster, không phải rig.)
- Kịch bản `text` tăng working set +18 MB: không phải cache nào phình mà là font dự phòng hệ thống nạp bằng `ReadAllBytes` (Segoe UI Emoji ~13 MB + Arial + Segoe UI = 14.9 MB) ở lần dùng emoji / RTL đầu tiên. Nay ánh xạ bộ nhớ (`MappedFontData`): private bytes +17.6 → +0.1 MB, GC heap +42 → +10 MB, tốc độ cache-hit không đổi (A/B 5 lần; hai trường + nhánh thay vì `Memory<byte>.Span`). Các cache chữ (wrap 4096 mục / 1M ký tự, shape 6 khối, atlas 32 MB, fallback 128 trang) đã có trần và phẳng.
- `VirtualList` không có chiều cao xác định dựng mọi hàng (100k hàng = 3,5 s/frame): `Window.Warn("virtual-list-unbounded")` một lần (`TraceKind.Warn`, `PerfLog` warn, `Debug`), tài liệu ở `docs/components.md` và skill `gotx-components`.
- `GotX.Stress` có `InternalsVisibleTo` ở `GotX.Platform.Windows`: kịch bản `gpu-loss` (WARP, tiêm 4 loại mất thiết bị mỗi 25 frame) chạy và báo `lossErrors`.
- Soak 30 phút (`--soak 30`, 3 cửa sổ, thay root mỗi 1500 frame, 101k frame): độ dốc phù hợp (sau 1/4 đầu) ws +0.39 MB/min, private +0.57 MB/min, **GC heap +367 KB/min** (> 100 KB/min nên đã điều tra). Heap sau full GC là dạng bậc thang, không phải đường dốc: 73 → 76 MB (5 phút) → 78.3 (10.5) → 82.8 (20 phút) rồi phẳng; từ phút 20 tới 30 độ dốc heap -1 KB/min, private 0.00 MB/min; khoảng cách giữa các bậc tăng gấp đôi (5.5 / 10.5 / 20 phút) và bậc sau lớn hơn bậc trước (1.2 / 2.2 / 4.5 MB) = mức cao nhất mới của bộ đệm/mảng nở theo cấp số nhân (xác suất kỷ lục giảm dần), không phải đối tượng mồ côi tích luỹ theo thời gian. Các mảng scene / hash / canvas / bộ đệm chữ đã soi bằng phản chiếu: ổn định (scene cửa sổ chữ 65536/66736 × 200 B, canvas 11 MB, `_entities` nở theo lượt quét entity chết). Chưa quy được bậc 1.2-4.5 MB cho đúng một cấu trúc (chưa có bộ đếm arena công khai); số đo tương đương lần soak 5 phút trước đây (+275 KB/min) nên không do đợt thay đổi này.
