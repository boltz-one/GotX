# GPUI yardstick – bảng mục tiêu benchmark cho GotX

Ngày thu thập 2026-11-16. Quy ước: **[S]** = có nguồn (trích/số nguyên văn), **[I]** = suy luận của người viết, **[?]** = không tìm được số có nguồn.
GotX đo trên **raster CPU phần mềm + máy không có GPU thật** (Microsoft Basic Display Adapter / WARP): thời gian frame GotX **không so được** với số GPU của GPUI.
Chỉ dùng GotX để so *kiến trúc / hành vi* (cái gì được vẽ lại, cấp phát, bộ nhớ), không so ms tuyệt đối.

## 1. Bằng chứng GPUI (nguồn sơ cấp trước)

| # | Chủ đề | Dữ kiện | Nguồn |
|---|---|---|---|
| G1 | Ngân sách frame | "only has 8.33ms per frame to push pixels to screen" (màn 60-120 Hz); game giữ "a constant rate of 120 frames per second" | https://zed.dev/blog/videogame |
| G2 | Chống GC jank | Electron jank do "random pause due to garbage collection" làm rớt frame (động lực dùng Rust) | https://zed.dev/blog/videogame |
| G3 | Primitive | UI = "rectangles, shadows, text, icons, and images"; "a custom shader for each specific graphical primitive"; rect vẽ "in a single draw call" bằng instancing, glyph cũng "a single, instanced draw call" | https://zed.dev/blog/videogame |
| G4 | Atlas glyph | glyph raster cache trong "a long-lived texture stored on the GPU"; blog nói "up to 16 variants" nhưng **mã hiện hành: `SUBPIXEL_VARIANTS_X=4`, `Y=1` (4 biến thể)** [S: gpui/src/text_system.rs:50-53]; atlas D3D11 mặc định 1024², tối đa 16384², 3 loại texture (R8/BGRA/RGBA subpixel), không đuổi glyph [S: gpui_windows/src/directx_atlas.rs:164-174]; text đã shape được cache | zed.dev/blog/videogame; zed commit 430a781 |
| G5 | Batching | `Scene::finish` sort theo `(order, kind)` (sprite theo `(order, texture_id)`); `BatchIterator` gộp primitive liên tiếp cùng loại/texture thành `PrimitiveBatch::{Shadows,Quads,Underlines,MonochromeSprites,PolychromeSprites,Paths,Surfaces}`; struct `#[repr(C)]`, field pad để "align to 8 bytes"; `quad_depth_scale` trần `(1<<24)-1` quad. Kích thước byte [S, test cứng wgpu_renderer.rs:2757-2768, commit 430a781]: Quad 160, Shadow 112, Underline 64, MonochromeSprite 112, SubpixelSprite 112, PolychromeSprite 96, PathTile 56, PathCurve 40, PathPaint 112 | https://raw.githubusercontent.com/zed-industries/zed/main/crates/gpui/src/scene.rs |
| G6 | Frame time đo được | Zed đo "consistently measuring frame times under 4ms" nhưng vẫn rớt frame mắt thấy; CVDisplayLink thử nghiệm cho "frame times were oscillating between 8ms and 16ms"; kết quả "a smooth 120 fps" | https://zed.dev/blog/120fps |
| G7 | Giữ nhịp sau input | Zed vẫn render thêm 1 giây sau input để display không hạ xung; code: `InputRateTracker` cửa sổ 100 ms, >= 6 sự kiện -> sustain 1 s; `FRAME_INTERVAL_TOLERANCE` 1 ms | https://zed.dev/blog/120fps ; https://raw.githubusercontent.com/zed-industries/zed/main/crates/gpui/src/window.rs |
| G8 | Dựng lại cây | Mỗi window có 2 `Frame` (`rendered_frame`/`next_frame`); `notify` -> `invalidate_view` ghi `dirty_views` + `mark_view_dirty` đi lên view tổ tiên. Mô hình immediate: view dirty `render` lại toàn bộ element con. Thân `Window::draw` không đọc được trong trích đoạn [?] | window.rs (như trên) |
| G9 | `cached` | `AnyView::cached(style)`: subtree "recycled from the previous frame unless" `notify` trên entity hoặc `Window::refresh`. Prepaint tái dùng chỉ khi `bounds`, `content_mask`, `text_style`, opacity khớp key và entity không nằm trong `dirty_views`; tái dùng = `reuse_prepaint`/`reuse_paint` (replay range primitive cũ). Layout của view cached chỉ xin theo style, không render con | https://raw.githubusercontent.com/zed-industries/zed/main/crates/gpui/src/view.rs |
| G10 | Cache text | `LineLayoutCache` hai thế hệ (`previous_frame`/`current_frame`), `finish_frame` hoán đổi + xoá; entry sống chỉ khi dùng mọi frame; `reuse_layouts` chuyển key theo range khi view cached được replay; key wrapped = (text, font_size, runs, wrap_width, force_width); xoá hết khi `font generation` đổi | https://raw.githubusercontent.com/zed-industries/zed/main/crates/gpui/src/text_system/line_layout.rs |
| G11 | Damage | **Đã xác nhận không có damage rect** [S: đọc cả window.rs + gpui_windows + gpui_wgpu, commit 430a781]: grep `damage|scissor|Present1|dirty_rect` chỉ trúng `ScissorEnable: false` (directx_renderer.rs:1490); mỗi present Clear + vẽ cả scene, `Present(0,0)`; frame không bẩn thì không draw/present (window.rs:2057-2098) | zed commit 430a781 |
| G12 | uniform_list | "uniform_list simply measures the first element and then lays out all remaining elements in a line"; "will only render the visible subset of items"; chỉ chạy với "uniform height"; range = floor(scrollTop/h) .. ceil(bottom/h), **không overscan**; test 10M item; f32 "Past 2^27px ... multiples of 16px" | https://raw.githubusercontent.com/zed-industries/zed/main/crates/gpui/src/elements/uniform_list.rs |
| G13 | Latency input | Zed có histogram input->frame latency sau feature flag (commit #53822), **không công bố con số**. Blog 2-8 ms/"<1 ms" không có phương pháp (bỏ qua). 1 người dùng chạy typometer thấy Zed 0.125.1 không nhanh hơn Sublime (giai thoại) | https://git.secluded.site/zed/commit/a9e7b7767207ccf40bcba04a4c5cad96d6916f13 ; https://forum.sublimetext.com/t/zed-is-faster-than-sublime-text4/67482?page=2 |
| G14 | Điểm yếu đã biết | gpui-fast (fork Longbridge, M4, median 5 lần, main-thread ms/frame): 2500 label không đổi **8.27** (fork 3.71); 2500 label đổi hết 8.27 (5.44); bảng rộng cuộn liên tục 7.82 (4.29); chèn trước 200 hàng có key 7.43 (1.97); 800 hàng 33.95 (9.98). Nguyên nhân nêu: GPUI "rebuilds its layout tree at the end of each frame" nên Taffy mất cache. Nguồn thứ cấp (AlphaSignal) | https://hermes-ai.net/news/longbridge-s-gpui-fast-fork-cuts-rust-ui-frame-costs-by-73/ ; repo https://github.com/longbridge/gpui-fast |
| G15 | RSS / bộ nhớ | **Không có số RSS của app GPUI tối thiểu** [?]. Zed full: 142 MB / 222 MB (5 tiến trình) / 200-400 MB tuỳ blog (không phương pháp, mâu thuẫn; bài DEV 0.12/0.150 sai phiên bản -> loại). Commit "Don't preallocate 600MB for GPUI profiler (#45197)" cho thấy profiler từng đặt trước 600 MB | https://git.secluded.site/zed/commit/a92283111b1e7ee1a1381c2307cddf8387347c8c |
| G16 | Idle CPU, startup | **Không tìm được số có nguồn** [?]. Chỉ có: sau input vẫn render 1 s (G7); HN giai thoại idle Zed làm tăng latency ứng dụng khác (không dùng) | - |

## 2. Số của GotX (từ docs + baseline json, không chạy lại)

Máy 16 core, Windows 11, .NET 11, Release, không GPU thật. Nguồn: `docs/performance.md` §4/§6, `docs/stress.md`, `docs/footprint.md`, `tools/*/*-baseline.json`.

| Mã | Chỉ số | Giá trị |
|---|---|---|
| N1 | `Window.Draw` (CPU: Render+layout+paint, chưa raster) median / p95 | shadcn-home cuộn 0.63 / 0.86 ms; gõ 0.13 / 0.25; resize 1.81 / 4.86; stress cuộn 2.34 / 3.17; stress resize 5.89 / 7.19 (35k primitive) |
| N2 | Frame đầy đủ gồm raster CPU (stress rig) p50 / p95 / p99 / max | vlist-100k 0.72 / 1.03 / 1.44 / 1.44; vlist-1m 0.66 / 0.78 / 0.89; input 5.2 / 6.2 / 7.6 / 11.4; resize-dpi 3.75 / 7.8 / 9.4 / 12.9; tree-10k 1.96 / 2.7 / 3.3 / 6.0; tree-100k 21.7 / 31 / 70.9; text 7.0 / 12.7 / 14.6 / 17.5 |
| N3 | Frame rảnh | draw 0.007 ms (stress/idle), 0 view render, 0 B; footprint idle CPU 0.00 % (home, hello; software JIT home 1.2 %) |
| N4 | Hover / gõ | stress hover 0.01 ms; shadcn-home gõ 0.13 ms, 0 B/frame; `lat` input->Draw có trong `PerfLog` nhưng không có baseline số |
| N5 | Primitive | `Primitive` 200 B; scene 35k primitive (stress), 316k (tree-100k); batch/draw call: bộ đếm `GpuCounters` (`draws`, `pipe`, `texsw`, `instB`) chỉ cho WARP, chưa có baseline |
| N6 | Glyph atlas | CPU atlas trần 32 MB; GPU atlas 512² -> 2048² (4 MB); **không có sub-pixel x** (glyph làm tròn nguyên pixel, Window.Text.cs:115; chỉ lượng tử cỡ 1/4 px) |
| N7 | Cache text | shape cache 6 khối chia sẻ 16k glyph; wrap cache 4096 mục / 1M ký tự, theo khoảng bề rộng; resize không miss wrap (WrapSpanTests) |
| N8 | Mô hình cây | Notify chỉ làm bẩn view đó; khung một phần (`Scene.ReplaceRange`), view `cached`; **cây element chưa giữ qua frame** -> resize Render lại 54-61 view (docs: "chưa làm") |
| N9 | Damage | Có, mặc định bật: ô 64², hint theo đoạn, CPU canvas giữ lại, D3D11 scissor + CopyResource; frame không đổi không Present. Chưa kiểm trên swap chain thật (§6.5) |
| N10 | Bộ nhớ | WS hello AOT d3d 33 MB / software 25 MB; home AOT 56-66 MB; JIT hello 47-53, home 88-91 MB; private hello 10-14 MB |
| N11 | Startup | first present AOT hello 61-132 ms, home 188-205 ms; JIT 247-310 / 525-648 ms; bench cold shadcn-home firstFrame 241 ms |
| N12 | VirtualList | 100k/1M hàng 0.72/0.66 ms p50, 1.4 KB/frame; **không có chiều cao xác định thì dựng mọi hàng** (100k = 3.5 s/frame) + cảnh báo |
| N13 | Resize | software 5.5 % CPU, d3d(WARP) 13-16 %; 3-4 KB/frame; heap leak 428 KB/200 bước |
| N14 | GC | gen2 = 0 mọi kịch bản footprint; soak 30 phút: ws +0.39 MB/phút, heap +367 KB/phút (bậc thang, phẳng sau phút 20) |

## 3. (a) Bảng so sánh

| Chỉ số | GPUI | GotX | Trạng thái |
|---|---|---|---|
| Ngân sách frame 120 FPS = 8.33 ms | G1 | Draw CPU 0.1-2.3 ms, raster CPU thêm 0.4-3.3 ms (N1,N2); worst p99 trừ tree-100k <= 14.6 ms | **par/không so được** (CPU raster vs GPU); nội dung trang thật nằm trong 8.33 ms, resize-dpi/text p99 vượt |
| Frame tĩnh (không đổi gì) | 8.27 ms/2500 label chưa đổi (G14, fork benchmark, M4) | 0.007 ms, 0 B, không Present (N3) | **ahead** về kiến trúc (không rebuild); số ms không so được |
| Dirty/rebuild theo view | G8: notify -> render lại view + tổ tiên; `cached` replay (G9) | Notify chỉ view đó + khung một phần + replay (N8) | **par** (cùng ý tưởng; GotX không phải render tổ tiên) |
| Giữ cây layout qua frame | GPUI không (G14); fork gpui-fast có | Không (N8) | **par** với GPUI upstream, behind fork |
| Batching primitive | 8 buffer theo loại, draw order = BoundsTree (chồng lấn), sort (order,texture), pre-pass quad đục + depth [S] | 1 uber-shader, 1 DrawRun/ảnh, giữ thứ tự vẽ, `draws/pipe/texsw` đo được (N5) | **khác kiến trúc**, GotX ít draw hơn, GPUI bớt overdraw |
| Kích thước instance | Quad 160 / Shadow 112 / Mono 112 / Poly 96 / Underline 64 B [S] | `Primitive` 200 B (mọi loại) | **GotX lớn hơn** 25-80 % |
| Glyph atlas / sub-pixel | 4x1 biến thể sub-pixel; atlas 1024² -> 16384², không đuổi [S] | 512²-2048² GPU, 32 MB CPU, không sub-pixel x (N6) | **GotX có trần bộ nhớ; GPUI có sub-pixel x** |
| Cache text | 2 thế hệ, replay theo range (G10) | shape + wrap cache có trần, theo khoảng bề rộng (N7) | **par** [I] |
| Damage / vẽ một phần | Không có [S: G11 xác nhận] | Có, mặc định bật (N9) | **ahead** |
| Input latency | Có histogram, không công bố số (G13) | `lat` trong PerfLog, không có baseline | **unknown** cả hai |
| RSS idle | [?] (Zed full 142-400 MB không tin cậy) | 25-66 MB AOT | **unknown** (không có yardstick minimal) |
| Idle CPU | [?] | 0.00 % | **unknown** |
| Startup | [?] | AOT 61-205 ms | **unknown** |
| Virtual list | uniform_list: đo hàng đầu, chỉ visible, không overscan, 10M OK (G12) | VirtualList 1M hàng 0.66 ms; thiếu chiều cao thì dựng hết (N12) | **par** về hiệu năng, **behind** về an toàn API (không có đường tự đo hàng đầu / không cảnh báo mềm) |
| Resize | GPUI rebuild cây mỗi frame (G14) | Render lại 54-61 view/frame (N8) | **par** với upstream; chưa tối ưu |
| Chống GC pause | Rust, không GC (G2) | .NET GC: gen2 = 0, nhưng 91-530 B/frame input, 3-4 KB resize | **behind** về nguyên lý (cần zero-alloc để không pause); thực đo chưa thấy gen2 |

## 4. (b) Mục tiêu đo được, xếp hạng (chưa đạt)

Xếp theo tác động / rủi ro so với thực hành GPUI; mỗi mục có công cụ sẵn có.

1. **Cây element giữ qua frame (resize không Render lại mọi view).** Mục tiêu: shadcn-home/resize views/frame 61 -> <= 10, stress/resize drawMed 5.9 -> <= 3 ms (G14: gpui-fast đạt giảm 34-73 % bằng cách này). Đo: `GotX.Perf bench --only home,stress --scenarios resize`; đọc `rendPerFrame`, `drawMed`.
2. **Steady-state 0 B/frame ở input / dialog / vlist / resize.** Hiện 91-530 / 209-292 / 1.6-3.9 KB / 3-4 KB. Mục tiêu: input, dialog 0 B; vlist <= 400 B; resize <= 1 KB (G2: tránh nguồn GC pause). Đo: `GotX.Footprint run --scenarios input,dialog,vlist,resize --renderer software` (cột B/frame); định vị: `GotX.Perf run-target <t> --alloc-types`.
3. **Ngân sách 8.33 ms p99 cho frame đầy đủ (Draw + raster) ở mọi kịch bản trừ tree-100k.** Hiện text p99 14.6, resize-dpi 9.4, input 7.6. Mục tiêu: p99 <= 8.33 ms (G1) ở `text`, `resize-dpi`; ghi rõ là CPU raster. Đo: `GotX.Stress --only text,resize-dpi,input --check`; thêm cổng p99 8.33 trong baseline.
4. **Input -> frame latency có số baseline.** Mục tiêu: p95 `lat` <= 8.33 ms (1 frame) cho hover/gõ/click headless và `real`. Đo: `GotX.Perf bench` + `report` (trường `lat`), `GotX.Perf real shadcn`; đưa `lat` p95 vào `perf-baseline.json` (hiện chưa gate). Cùng ý với histogram G13.
5. **Batching GPU có mục tiêu.** Mục tiêu (chọn sau khi đo, I): draws/frame <= số loại primitive x số thay đổi clip/texture; `texsw` thấp; `instB` theo primitive 200 B. Đo: `GotX.Perf bench --gpu` (WARP offscreen), ghi `draws`, `pipe`, `texsw`, `instB`; so sánh thứ tự sort kiểu G5 (đoán: gom theo (loại, texture) trước khi submit). Số tuyệt đối chỉ đúng khi chạy trên GPU thật.
6. **Thu nhỏ `Primitive` 200 B.** GPUI dùng struct riêng cho từng loại (G5) và bind riêng từng buffer; GotX dùng một struct 200 B chung (stress 2 ms chép 35k primitive trước khi có damage). Mục tiêu: <= 128 B hoặc tách theo loại; đo copy: `stress/tree-100k` (316k primitive) p50 21.7 ms, `GotX.Perf profile-raster`, `GotX.Stress --only tree-100k,churn`.
7. **VirtualList tự đo hàng đầu (kiểu uniform_list) / không dựng hết khi thiếu chiều cao.** Mục tiêu: 100k hàng không có `h-*` <= 2 ms/frame thay vì 3.5 s. Đo: `GotX.Stress --only vlist-100k,vlist-1m` thêm biến thể không đặt chiều cao.
8. **Sub-pixel glyph variants + atlas hit-rate có bộ đếm.** Mục tiêu: ghi số biến thể và `atlasHit` >= 99 % ổn định sau warm-up cho kịch bản `text`. Đo: `GotX.Perf bench --gpu`, PerfLog `glyphN` + `atlasHit`. (G4: GPUI dùng 4 biến thể x 1 y, mã commit 430a781.)

## 5. (c) Không so sánh công bằng được

| Chỉ số | Lý do |
|---|---|
| Frame ms tuyệt đối | GotX đo raster CPU + máy không GPU (GPU timer WARP 13-18 ms/frame ở 1440x900 chỉ là bộ raster phần mềm của driver); GPUI số G6/G14 trên Metal/M4 |
| `Draw` ms vs main-thread frame ms của GPUI | GotX Draw = Render+layout+paint chưa raster, không gồm submit; số GPUI (G14) gồm toàn bộ main-thread và là fork, kịch bản khác (2500 label) |
| Idle RSS / startup / idle CPU | GPUI không có số minimal có nguồn (G15, G16); Zed full = editor đầy đủ, nhiều tiến trình; RSS trên macOS còn lệch so với footprint |
| Input latency | Zed không công bố số (G13); GotX chưa có baseline; đo nhìn thấy phụ thuộc compositor/vsync, WARP không đại diện |
| Damage D3D11 | Chưa kiểm trên swap chain/driver thật (performance.md §6.5); GPUI không có để so |
| Kích thước instance / atlas | GPUI không công bố byte từng struct hay kích thước atlas trong file đã đọc |
| Resize mượt | GotX `SetWindowPos` đồng bộ khác kéo chuột thật (modal loop); GPUI chưa có số |
| Soak / leak | .NET GC heap bậc thang vs Rust không GC: metric khác bản chất |

## Chưa giải quyết

Đã giải quyết bằng đọc mã (xem `docs/gpui-source-analysis.md`): thân `Window::draw` + phần sau `window.rs` (không có damage/scissor), kích thước struct, atlas size / số biến thể sub-pixel.

- Số RSS/idle CPU/startup của app GPUI tối thiểu: cần tự build example GPUI và đo (mã cho thấy luồng vsync đánh thức ~mỗi vsync dù rảnh [S: platform.rs:429-486], nên idle CPU GPUI > 0 [I]).
- Giá trị mặc định `inactive/low-power/thermal` frame interval trong `WindowOptions`.
- Độ trễ input->frame thực của GPUI (chỉ có histogram sau feature `profiler`, không số).
