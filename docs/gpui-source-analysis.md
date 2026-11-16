# GPUI vs GotX – phân tích mã nguồn

Nguồn: Zed commit 430a781, sparse `crates/{gpui,gpui_windows,gpui_wgpu,...}` (đọc từ bản clone cục bộ, không build/chạy).
Đường dẫn dưới đây tương đối `crates/`. **[V]** = đọc trực tiếp trong mã, **[I]** = suy luận. Windows dùng `gpui_windows` (D3D11 + DirectComposition + DirectWrite), KHÔNG dùng wgpu.
GotX: `docs/performance.md`, `src/GotX.*`. Bảng số liệu so sánh: `docs/gpui-benchmarks.md`.

## 1. Vẽ lại một phần / damage / dựng lại mỗi frame

**GPUI không có damage rect [V].** `grep damage|scissor|Present1|dirty_rect` trên gpui, gpui_windows, gpui_wgpu: chỉ trúng `ScissorEnable: false` (`gpui_windows/src/directx_renderer.rs:1490`). Mỗi present vẽ cả scene lên back buffer mới (`Clear` + vẽ hết, `directx_renderer.rs:369-459`); `Present(0, 0)` (`:270`).
Frame không bẩn: không draw, không present (`gpui/src/window.rs:2057-2098`: `should_draw = invalidator.is_dirty() || force_render`; `else if needs_present`). Giống GotX (frame rảnh = 0).

Chu trình khi bẩn [V] (`window.rs:3783-3941`, `draw_roots` `:4029`):
- `root_element = root view .into_any_element(); request_layout(...)` rồi `prepaint_as_root`, `paint` (`:4062-4106`). **Cây element dựng lại từ đầu mỗi draw**; mọi `ViewElement` KHÔNG `cached` gọi lại `Render::render` (`view.rs:495-499`).
- `layout_engine.clear()` (`:3866`) xoá toàn bộ cây Taffy; `next_frame.clear()` (`:3876`) xoá scene/hitbox/listener; `Frame.finish` chỉ giữ `element_states` được truy cập (`:1208-1218`).
- `notify` -> `invalidate_view` + `mark_view_dirty` đánh dấu **entity và MỌI view tổ tiên** (`:171-196`, `:2522-2534`). GotX chỉ đánh dấu view đó (đã tốt hơn).
- `AnyView::cached(style)` là tuỳ chọn (opt-in), cần kích thước xác định; layout chỉ theo `style`, KHÔNG đo nội dung (`view.rs:486-494`, `:249-261`). Tái dùng khi `bounds`, `content_mask`, `text_style`, `opacity` khớp key, view không nằm trong `dirty_views` và `!window.refreshing` (`view.rs:544-560`). Tái dùng = `reuse_prepaint` + `reuse_paint` (`window.rs:4369-4479`).
- `reuse_paint` -> `Scene::replay` (`scene.rs:227-245`) **chèn lại từng primitive qua `insert_primitive`** (clone + `BoundsTree::insert` gán draw order). Replay vẫn O(primitive), không phải memcpy; mỗi primitive nằm 2 lần (`paint_operations` + vec theo loại) [I về kích thước enum ~168 B].
- `window.refresh()` (resize, kéo thả, click, tooltip, ... `div.rs:3031,3071,3199,3350`) đặt `refreshing=true` -> **bỏ qua mọi cache view** (`view.rs:550`). Resize: `bounds_changed` -> `refresh()` (`window.rs:3125`) => dựng lại toàn bộ.
- Hover: listener MouseMove từng phần tử có `hover_style`, chỉ `cx.notify(current_view)` khi trạng thái đổi (`div.rs:2950-2975`). Mỗi mouse event chạy MỌI listener đã đăng ký (`window.rs:6414-6435`) + hit-test tuyến tính qua mọi hitbox (`:1178-1200`, gọi ở `:6404`).
- Animation sprite: `Scene::advance_animations` đổi ma trận/alpha của sprite đã có rồi present lại, KHÔNG dựng lại scene (`scene.rs:84-124`, `window.rs:3057-3060`, `:2076`). Kỹ thuật đáng học (spinner/fade không tốn Render).

**Verdict:** GotX hơn (damage theo đoạn, notify 1 view, ReplaceRange thay vì replay từng primitive, idle 0 frame). GPUI hơn ở: view `cached` tái dùng prepaint (hitbox, dispatch tree) cả khi cha bẩn; animation sprite không dựng lại.

## 2. Primitive, batching, atlas

Kích thước [V] (có test cứng `gpui_wgpu/src/wgpu_renderer.rs:2757-2768`, định nghĩa `gpui/src/scene.rs`):

| Struct | Byte | Dòng | Ghi chú |
|---|---|---|---|
| Quad | 160 (40 từ) | scene.rs:743 | Background 72 B (tag, color space, solid Hsla, 2 stop gradient, pad) |
| Shadow | 112 | :782 | + shape_bounds/shape_corner_radii (vùng loại trừ) |
| Underline | 64 | :763 | |
| MonochromeSprite | 112 | :943 | AtlasTile 32 B + TransformationMatrix 24 B |
| SubpixelSprite | 112 | :1008 | ClearType, atlas RGBA riêng |
| PolychromeSprite | 96 | :1027 | emoji/ảnh, opacity, corner_radii |
| Path | tile 56 + curve 40 + paint 112 | :1063-1097 | bin theo tile, curve trong buffer |
| GotX `Primitive` | **200** | Scene.cs:11 | một struct cho mọi loại |

- 8 buffer instance riêng theo loại (`scene.rs:50-64`), `StructuredBuffer` D3D11, `DrawInstanced(4, n)` triangle strip, `Map(WRITE_DISCARD)` chép CẢ vec mỗi frame (`directx_renderer.rs:1658-1670`), dung lượng nở luỹ thừa 2, trần 256 MB (`:29`, `:1137-1160`).
- **Draw order = BoundsTree** (R-tree, `bounds_tree.rs`): mỗi primitive nhận `order` = (max order của primitive chồng lấn)+(kind); không chồng lấn thì chung order (`scene.rs:166-198`, `:375-390`). `finish()` sắp (order, texture) theo cặp khoá + hoán vị tại chỗ (`:247-342`). `BatchIterator` gộp theo loại/texture khi cùng order (`:493-674`). Hệ quả: nhiều chữ/quad rời nhau chung batch bất kể thứ tự vẽ.
- **Pre-pass quad đục + depth buffer** [V]: `draw_opaque_quads` vẽ mọi quad đục từ cuối về đầu có ghi depth (`directx_renderer.rs:390-398`, `:556-567`, `:1296-1323`, DepthFunc GREATER `:1554`); batch sau test depth => pixel bị che không chạy pixel shader. `quad_depth_scale` trần 2^24 quad (`scene.rs:345`). `insert_quad` tách quad có viền thành ruột + dải viền để ruột vẫn "đục" (`window.rs:5077-5098`).
- Cắt primitive rỗng sau clip ngay lúc chèn (`scene.rs:171-175`); content_mask nằm trong từng instance (cắt trong shader).
- Swap chain: `FLIP_SEQUENTIAL`, 3 buffer, `Present(0,0)`, composition swap chain + DirectComposition khi cho phép (`directx_renderer.rs:1340-1390`, `:1718`); BGRA8.
- **Atlas** [V] (`gpui_windows/src/directx_atlas.rs`, `gpui/src/platform.rs:1868-1924`): 3 danh sách texture (Monochrome R8, Polychrome BGRA8, Subpixel RGBA8), mỗi texture mặc định **1024²**, tối đa **16384²** (`:164-174`), allocator `etagere::BucketedAtlasAllocator`, `HashMap<AtlasKey, Tile>` băm theo key mỗi glyph mỗi lần paint, khoá Mutex. **Không có đuổi glyph theo LRU hay trần bộ nhớ**; chỉ `remove(key)` cho ảnh/SVG bị drop, texture rỗng về free_list (`:108-130`). Mất thiết bị thì xoá hết.
- **Sub-pixel**: `SUBPIXEL_VARIANTS_X = 4`, `Y = 1` (`text_system.rs:50-53`, lượng tử ở `window.rs:5251-5259`) – **4 biến thể, không phải 16 như blog**. Khoá glyph = (font, glyph, size, subpixel_variant, scale, emoji, subpixel_rendering, dilation) (`text_system.rs:1110-1119`). Raster-bounds có cache riêng (`:235`).

**So với GotX:** GotX glyph làm tròn nguyên pixel theo x (`Window.Text.cs:115`, `MathF.Round`) – không có sub-pixel x [V]; chữ lượng tử 1/4 px theo cỡ (GlyphAtlas.cs `sizeQ`). GotX một uber-shader, một DrawRun/ảnh, giữ đúng thứ tự vẽ nên không cần sort; không có depth pre-pass. Atlas GotX có trần (CPU 32 MB, GPU 512²..2048², co/xoá khi đầy); GPUI không trần. GotX Primitive 200 B > Quad 160 B > Mono sprite 112 B: một glyph tốn 200 B/frame ở GotX so với 112 B ở GPUI [V].

## 3. Chữ

- `LineLayoutCache` hai thế hệ `previous_frame`/`current_frame` (`text_system/line_layout.rs:478-513`): tra current, rồi previous (`remove_entry` chuyển lên), trượt thì gọi DirectWrite (`:597-728`). `finish_frame` hoán đổi + xoá (`:584-595`). Mục không dùng trong 1 frame bị bỏ. Khoá = (text, font_size, runs, wrap_width, force_width) băm CẢ chuỗi mỗi lookup (`:790-856`). `reuse_layouts` chuyển khoá theo range khi replay view cached (`:541-569`). Xoá hết khi `font_generation` đổi (`:730-745`).
- Wrap [V]: `layout_wrapped_line` gọi `layout_line(text,size,runs,None)` (shape) rồi `compute_wrap_boundaries` trên glyph đã shape (`:636-646`) => đổi bề rộng chỉ tính lại biên dòng, không shape lại; nhưng khoá có `wrap_width` nên **mỗi bề rộng mới là một lần trượt** + cấp phát `Arc`. `LineWrapper` (truncate/wrap thô) dùng `CharWidthCache` ASCII[128]+HashMap theo (font,size), pool (`line_wrapper.rs:34-57`, `:551-585`, `text_system.rs:236`).
- Shaping: **DirectWrite `CreateTextLayout` mỗi lần trượt** (`gpui_windows/src/direct_write.rs:479-560`), tách theo dấu xuống dòng; fallback font hệ thống qua `IDWriteFontFallback` (`:286-340`); phân giải font có cache `font_ids_by_font` (`text_system.rs:233`, `:569-615`). Không có cache shape qua frame ngoài hai thế hệ [V].
- Elements `Text`: `request_measured_layout` + memo trong `TextLayoutInner` theo wrap_width, chỉ trong một frame vì element dựng lại (`elements/text.rs:654-669`) [V].

| | GPUI | GotX |
|---|---|---|
| Khoá cache | (text, size, runs, wrap_w) băm cả chuỗi | tham chiếu chuỗi + [start,end) + font + cờ, ánh xạ trực tiếp 2048 ô (ShapeCache.cs:7-60) |
| Vòng đời | 2 thế hệ theo frame | trần cố định (6 khối x 16k glyph, thế hệ chunk), nhận khoá ở frame khác |
| Wrap | tính lại biên khi đổi bề rộng, 1 khoá/bề rộng | cache theo KHOẢNG bề rộng (kéo giãn 0 miss), 4096 mục/1M ký tự |
| Shaping | DirectWrite (ngoài, ~COM/lần) | engine riêng (GSUB/GPOS/kern, bidi) trong tiến trình |

**Verdict:** GotX hơn về wrap theo khoảng và chi phí băm; GPUI hơn ở fallback font hệ thống đầy đủ (DirectWrite) và sub-pixel positioning. Không có kỹ thuật cache nào của GPUI cần bê sang.

## 4. Layout

- Taffy (`gpui/src/taffy.rs`): `TaffyTree` **clear mỗi frame** (`:55-60`, `window.rs:3866`) => không dùng cache nút của taffy qua frame; dựng lại node mỗi draw (`new_leaf`/`new_with_children` `:62-83`). Chữ = `new_leaf_with_context(measure closure)` (`:85-107`); closure `Box<dyn FnMut>` cấp phát heap mỗi node chữ mỗi frame.
- `compute_layout` bỏ cache khi cùng id tính lần hai (`:205-219`); làm tròn tắt (`disable_rounding`), tự snap sang device pixel trước/sau (`:281-390`, tuỳ chọn cân bằng: snap theo cạnh tuyệt đối, nửa về 0).
- Relayout boundary duy nhất = view `cached` (kích thước cố định): `layout_as_root` riêng, gốc taffy riêng (`view.rs:562-569`, `element.rs:570-618`). Không `cached` thì đo/xếp lại cả cây khi bẩn.
- `uniform_list::measure_item` render+layout phần tử đo **hai lần mỗi frame** (request_layout `:283` và prepaint `:359`) [V].
- GotX: `LayoutEngine` tự viết, `MeasureMemo` 4 mục/node theo (availW, availH) (`Node.cs:6-45`, `LayoutEngine.cs:62,104`), khung một phần + relayout boundary theo kích thước/min-content khớp.
- Chi phí GotX hiện đo: 3.8 Measure/node ở stress/resize (`performance.md §6.4`); GPUI cũng không giữ cây (gpui-fast fork đạt -34..-73% bằng cách giữ) [secondary, benchmarks G14].

**Verdict:** ngang hàng ở "dựng lại khi resize"; GotX có memo trong frame, GPUI không memo giữa các lần `measure` ngoài per-element. Ý tưởng "giữ cây" vẫn là đường lớn nhất cho cả hai.

## 5. Mô hình bộ nhớ

- `Arena` bump allocator (`arena.rs`): chunk **1 MiB** (`window.rs:338` `Arena::new(1024*1024)`), thêm chunk khi đầy, `clear()` chỉ reset offset + drop `ArenaElement` (`:139-158`). Chỉ chứa các `Drawable<E>` của `AnyElement` (`element.rs:658-671`); `ArenaBox` kiểm `valid` mỗi deref (`arena.rs:218-249`).
- KHÔNG zero-alloc: `Div.children: SmallVec<[AnyElement;2]>` (heap khi >2), `Interactivity` hơn 1 KB (div.rs:1906 chú thích), Taffy node, closure đo `Box`, `Arc<LineLayout>`, `Arc<CacheKey>`, `FxHashMap` frame (hitbox/listener/element_states) [V]. Zero-alloc chỉ ở phần đối tượng element (một lần xoá) – mô hình tránh Drop rải rác, không tránh malloc [I].
- Entity: `Entity<T>` store trong `EntityMap` (app.rs/entity_map), notify qua `Effect::Notify` hàng đợi (`window.rs:188`).
- Hai `Frame` (rendered/next) hoán đổi (`window.rs:3873-3876`): scene, hitbox, listener, tab stop bị xây lại mỗi lần bẩn; replay chỉ tiết kiệm Render/prepaint/paint của view `cached`.
- GotX: cấp phát có trần + bump (khối shape, `Scene.Reserve`), 0 B/frame ở idle/hover/click/gõ, nhưng resize ~0.8-3 KB, `Primitive` 200 B.

**Verdict:** GotX hơn về "0 B/frame" thực đo vì .NET đã chọn cấu trúc phẳng; điểm GPUI duy nhất đáng nhớ: arena xoá một lần cho cả cây element (GotX đã giữ node và tái dùng nên không cần).

## 6. Nhịp frame, resize, input, present (Windows)

- Nhịp: luồng `VSyncProvider` chạy suốt đời ứng dụng, `DwmFlush` mỗi vsync rồi `RedrawWindow(RDW_INVALIDATE)` mọi cửa sổ hiển thị (`gpui_windows/src/platform.rs:429-486`, `vsync.rs:31-60`), dự phòng `Sleep(interval)` khi DwmFlush trả sớm. WM_PAINT -> `draw_window` -> `request_frame` (`events.rs:1361-1412`). **Idle vẫn ~60-120 wakeup/s** (callback chạy rồi bỏ qua vì không dirty) [V], GotX idle = 0 frame.
- `FramePacer` giữ nhịp theo `minimum_frame_interval` (không hoạt động / pin yếu / nhiệt), dung sai 1 ms, bù trễ ≤ interval/4 (`window.rs:1342-1400`, `:1991-2038`).
- `InputRateTracker`: cửa sổ 100 ms, ≥ 6 sự kiện gây invalidate => giữ present 1 s sau để màn hình không hạ xung (`window.rs:1334-1434`, `:6218-6220`) [V].
- Present: `FLIP_SEQUENTIAL`, 3 buffer, `Present(0,0)` KHÔNG chặn luồng UI chờ vsync; nhịp do luồng vsync (`directx_renderer.rs:264-273`). GotX: `Present(_swap, 1, 0)`, `FLIP_DISCARD`, 2 buffer, chặn luồng UI tới vsync (`D3D11Renderer.Frame.cs:121`, `Init.cs:44-45`) [V].
- Input: KHÔNG gộp mouse move [V] (grep `coalesc` chỉ trúng thống kê profiler). Mỗi `WM_MOUSEMOVE` chạy ngay listener (`events.rs:350-393`; bỏ move trùng vị trí); khung chỉ vẽ ở WM_PAINT do vsync => N sự kiện/frame chỉ 1 draw (gộp ngầm nhờ dirty flag). GotX cũng không gộp tường minh [V grep].
- Resize: `WM_SIZE` -> `ResizeBuffers` đồng bộ mỗi lần + recreate RTV/depth (`events.rs:221-275`, `directx_renderer.rs:569-612`); vòng modal dùng timer `USER_TIMER_MINIMUM` gọi `draw_window` (`:277-316`). Swap chain composition `SCALING_STRETCH` => DWM co dãn khung cũ, không hở viền. `refresh()` => dựng lại cả cây [V].
- Mất thiết bị: `skip_draws` bỏ frame đầu sau recover, ép `force_render` (`directx_renderer.rs:346`, `window.rs:2070-2074`). GotX tương đương (dựng lại + Software fallback).
- Frame đầu vào chạy trên luồng chính, không có luồng render riêng [I: gpui_windows không spawn renderer thread].

**Verdict:** GotX hơn về idle (0 wakeup). GPUI hơn về không chặn UI ở Present và về composition swap chain (resize không hở) và chống hạ xung.

## 7. `uniform_list` / `list`

- `uniform_list` (`elements/uniform_list.rs`): đo 1 phần tử (index chỉ định) -> chiều cao chung, **render+layout 2 lần/frame** (`:283`, `:359`, `:653-675`); range = `floor((scrollTop-pad)/h)..ceil((scrollTop+viewport)/h)`, tính bằng f64 (`:437-448`), **không overscan**; chỉ phần tử trong range được render lại mỗi frame, `layout_as_root` từng cái với chiều cao cố định (`:473-486`). Scroll-to-item theo chiến lược Top/Center/Bottom/Nearest (`:395-434`), chiều cao tổng = h*count (f32 mất chính xác sau 2^27 px).
- `list` (`elements/list.rs`): `SumTree<ListItem>` lưu chiều cao đo của từng phần tử (`:65`, `:996-1269`); `layout_items` render+layout phần tử nhìn thấy mỗi frame, phần tử trong `overdraw` (px, tham số) dùng chiều cao đã đo nếu có, nếu chưa thì render+đo; bù neo cuộn (`scroll_anchor`), hỗ trợ follow-tail, `splice/remeasure` khi nội dung đổi. Chỉ vẽ các phần tử nhìn thấy (`:1301-1330`).
- GotX `VirtualList`: có chiều cao xác định thì 0.66-0.72 ms/1M-100k hàng; không có: trước dựng hết (3.5 s), nay dự phòng `max-h-screen` (~0.8 ms, `vlist-auto-100k`) + cảnh báo. Không tự đo hàng đầu (ItemHeight vẫn do người dùng đặt); chỉ chặn vòng khung nhìn = nội dung.

**Verdict:** GotX đủ nhanh; thiếu (a) đo phần tử đầu để suy chiều cao (b) `list` biến chiều cao dùng SumTree/chỉ mục tích luỹ + neo cuộn. Đây là khoảng trống API, không phải hiệu năng.

## 8. Kỹ thuật đáng áp dụng cho GotX (xếp hạng)

| # | Kỹ thuật | Lợi ước tính | Rủi ro |
|---|---|---|---|
| 1 | `Present(0,...)` + luồng/đồng hồ vsync (DwmFlush hoặc waitable swap chain `SetMaximumFrameLatency`), `FLIP_SEQUENTIAL` 3 buffer | bỏ chặn UI tới 1 vsync; giảm độ trễ input->frame tới ~1 frame [I] | Trung bình: flip-model + retained target + damage (`performance.md §5`) cần kiểm trên GPU thật |
| 2 | Composition swap chain (`CreateSwapChainForComposition`, `SCALING_STRETCH`, premultiplied) | resize không hở viền, có thể bỏ vẽ đồng bộ khi kéo [I] | Trung bình: DirectComposition thêm phụ thuộc, test UIA/DPI |
| 3 | Sub-pixel glyph x 4 biến thể (khoá atlas thêm 2 bit) | chữ đều khoảng cách hơn khi cuộn mượt/zoom; atlas x4 (tối đa) [V cho cách GPUI] | Thấp-trung: đổi ảnh PNG golden, tăng bộ nhớ atlas ~4x cho chữ động |
| 4 | Sprite-animation: dữ liệu chuyển động/alpha gắn primitive, advance trước present không dựng lại scene | spinner/fade 0 Render, 0 Layout (hiện đã cô lập view, 0.03 ms) | Thấp, lợi nhỏ vì đã cô lập |
| 5 | Pre-pass quad đục + depth (hoặc tách `Primitive` theo loại 112-160 B) | giảm overdraw trên GPU thật; giảm 200->~112 B/glyph [V kích thước] | Trung bình: shader/renderer lớn; chỉ đáng khi đo trên GPU thật |
| 6 | `InputRateTracker` giữ present ~1 s sau input nhanh | tránh hạ xung màn hình VRR/laptop [V] | Thấp; chỉ ý nghĩa trên GPU/màn hình thật, không đo được ở máy WARP |
| 7 | uniform_list tự đo hàng đầu cho `VirtualList` | thay 3.5 s/frame bằng ~1 ms khi quên chiều cao | Thấp |
| 8 | `list` chiều cao biến thiên: bảng cộng dồn + neo cuộn + overdraw | thiếu tính năng hơn là hiệu năng | Trung bình |
| 9 | Phần tử `cached` GPUI giữ cả hitbox/dispatch tree khi cha bẩn | GotX đã có tương đương (replay + ReplaceRange) | – |

**Không nên bê:** BoundsTree + sort theo (order, texture) (GotX giữ thứ tự vẽ, 1 uber-shader, ít draw); LineLayoutCache hai thế hệ băm cả chuỗi (shape cache theo tham chiếu của GotX rẻ hơn); xoá Taffy mỗi frame; mark ancestors dirty; VSync thread chạy liên tục khi rảnh.

## 9. GotX đã tốt hơn GPUI (có bằng chứng)

1. Damage theo đoạn (ô 64²) + canvas giữ lại; GPUI vẽ lại cả scene mỗi present.
2. Frame rảnh 0 wakeup (GPUI: luồng vsync đánh thức mỗi vsync).
3. Notify chỉ làm bẩn view đó (GPUI: cả chuỗi tổ tiên, `window.rs:2522`).
4. `Scene.ReplaceRange` phát lại đoạn primitive bằng sao chép/dời; GPUI `replay` chèn lại từng primitive + BoundsTree.
5. Cache ngắt dòng theo khoảng bề rộng (kéo giãn 0 miss); GPUI mỗi bề rộng một lần trượt.
6. Atlas/shape cache có trần cố định + giám sát; atlas GPUI tăng tới 16384² mỗi loại không đuổi.
7. 0 B/frame đo được ở hover/click/gõ + bộ test chặn hồi quy; GPUI vẫn alloc `Arc`, closure, hashmap mỗi frame.
8. Bộ test chia sẻ khả năng quan sát (PerfLog, parity CPU/GPU, damage/partial parity).

## 10. Câu hỏi chưa giải

- Giá trị `inactive/low_power/thermal_frame_interval` mặc định trong `WindowOptions` (chưa đọc `platform.rs` đủ).
- GPUI có luồng/độ trễ input->frame thực: histogram sau `profiler` feature, không có số công bố.
- Hiệu năng thực của depth pre-pass và sắp xếp BoundsTree: chỉ phân tích mã, không đo (không build).
- `frame_waker` mặc định của Windows (không thấy override) nên coi như luồng vsync là nguồn duy nhất [I].
