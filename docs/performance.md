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
