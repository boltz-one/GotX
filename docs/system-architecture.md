# Kiến trúc hệ thống GotX

> Cập nhật: 2026-11-15 · Đối chiếu với `src/GotX.Core`, `src/GotX.Raster`, `src/GotX.Platform.Windows`. Ngoài phạm vi: yêu cầu sản phẩm → [project-overview-pdr.md](project-overview-pdr.md); danh mục file → [codebase-summary.md](codebase-summary.md); quy tắc code → [code-standards.md](code-standards.md); pipeline class Tailwind → [tailwind.md](tailwind.md); component → [components.md](components.md); công cụ test → [testing.md](testing.md).

## 1. Phân tầng và chiều phụ thuộc

```
GotX.Core                  BCL only, không P/Invoke, không code OS, InvariantGlobalization
  ▲            ▲                 ▲
  │            │                 │
GotX.Raster    GotX.Components   GotX.Test (Core + Raster)
  ▲   ▲                          
  │   └──────── GotX.Platform.Windows (Core + Raster): Win32, D3D11, IMM32, clipboard
  │                 ▲
  │                 │
samples/Hello, samples/Components (Platform.Windows [+ Components])
samples/Headless                  (Raster)            ── chạy mọi OS, không cửa sổ
samples/ComponentsCheck           (Raster + Components)
tests/GotX.Tests                  (Test + Components)
```

Quy tắc: Core không tham chiếu project nào. Chỉ `Platform.Windows` biết Win32/D3D11. `Raster` là renderer CPU dùng chung cho Headless, Test và fallback của Windows.

**Ranh giới platform** (`src/GotX.Core/App/Platform.cs`):

| Interface | Vai trò | Cài đặt |
|---|---|---|
| `IPlatform` | `Run(onLaunch)`, `OpenWindow`, `PostToMain`, `RegisterDefaultFonts`, `Clipboard` | `WindowsPlatform` |
| `IPlatformWindow` | `LogicalSize`, `ScaleFactor`, `RequestFrame`, `RequestAnimationFrame`, sự kiện `Input` / `Frame` / `Closed`, `Draw(Scene)`, `ImeCaret`, `CursorProvider` | `Win32Window` |
| `IClipboard` | `GetText` / `SetText` | `WindowsClipboard`; headless dùng `MemoryClipboard` |

`Application.Run` (Platform.cs) nối hai bên: `pw.Frame → window.Draw → pw.Draw(scene)`; `pw.Input → window.DispatchInput`; `window.FrameRequested → pw.RequestFrame`.

## 2. Mô hình runtime

### 2.1 App: entity, effect, timer

`App` (`App/App.cs`) sở hữu mọi entity, các `Window` và hàng đợi effect.

```
entity.Notify() ──► _pendingNotify (khử trùng theo Id) ─┐
entity.Emit(e)  ──► (không khử trùng)                   ├─► _effects (Queue<Effect>)
                                                        │
App.Flush() ◄───── gọi sau mỗi input, timer, Post, New ─┘
   ├─ Notify: mọi Window.MarkViewDirty(id) → observer.OnNotify()   (Observe)
   └─ Emit:   subscriber theo (id, kiểu sự kiện)                   (Subscribe<T>)
```

- `Flush` không lồng nhau (`_flushing`). Effect sinh ra trong lượt được xử lý tiếp trong cùng lượt. Vượt `MaxEffectsPerFlush` (100 000) thì xoá hàng đợi và ném `InvalidOperationException`.
- Đăng ký lưu dạng mảng bất biến (copy-on-write), nên đăng ký hoặc huỷ ngay trong callback không làm hỏng vòng lặp.
- `Release(entity)`: gọi `Window.OnEntityReleased` (bỏ focus/kéo, đóng overlay chứa nó), huỷ observer/subscriber của entity, `Detach`. Entity bị bỏ quên được dọn khi `SweepDeadEntities` chạy (nhờ `WeakReference`).
- `Post(action)`: đưa việc lên main thread rồi `Flush`. Không có platform (headless) thì chạy ngay.

**Timer** (`App/App.Timers.cs`): `SetTimeout` / `SetInterval` trả `IDisposable`. `RunTimers()` gọi các hẹn giờ đến hạn theo thứ tự `Due`, rồi `Flush`. Interval trễ nhiều chu kỳ thì không gọi dồn (`Due = max(Due + Period, now)`). Có platform: `System.Threading.Timer` đánh thức và `PostToMain(RunTimers)`. Headless/test: gọi `RunTimers()` sau khi đẩy `Clock`.

### 2.2 Entity và View

`Entity` (`App/Entity.cs`) có `Id` (tăng dần từ 1), `Notify()`, `Emit<T>()`. `View : Entity` có `abstract Div Render(Window)`; `TextInput` cũng là `View`. Chỉ view được cache; `Notify()` đánh dấu bẩn view đó và các view cha (mục 4).

### 2.3 Window: đơn vị vẽ

Mỗi cửa sổ OS có một `Window` (`App/Window.cs`). Cặp `_scene/_prevScene`, `_hitboxes/_prevHitboxes`, `_hovers/_prevHovers`, `_cache/_prevCache` là bộ đệm đôi, hoán đổi mỗi frame; `_prevCache` phục vụ phát lại. `_tree` (arena) reset mỗi frame. `_scrolls`, `_transitions` giữ state qua frame (mục 5); `_overlays` là lớp trên cùng (mục 7). `TextSystem` (font, `GlyphAtlas`, cache wrap) dùng chung giữa các cửa sổ qua `App.Text`.

### 2.4 Luồng và thread

- Toàn bộ `App`, `Window`, `Entity` chạy trên **main thread** (thread gọi `Application.Run`). Không có khoá.
- Thread khác gọi `App.Post(action)` → `WindowsPlatform.PostToMain` → `ConcurrentQueue` + `PostThreadMessageW(WM_APP)` → `Drain()` trong vòng lặp message (`WindowsPlatform.cs`).
- Ngoại lệ có khoá: `ImageSource` (đăng ký id) và `ImageSource.Find` (đọc mảng `s_all`).

```
Win32 message                     Core
─────────────                     ────────────────────────────────────
WM_PAINT ───────► pw.Frame ─────► Window.Draw ─► Scene ─► pw.Draw ─► D3D11 | CPU
WM_MOUSE/KEY/IME► pw.Input ─────► Window.DispatchInput ─► App.Flush
WM_TIMER (15ms) ► RequestFrame  (hẹn khi WantsAnimationFrame)
app timer ─────► PostToMain ────► RunTimers ─► Flush
other thread ──► App.Post ──────► Drain ──────► action ─► Flush
```

## 3. Pipeline khung hình (`Window.Draw`)

```
Draw(logicalSize, scale)                                       App/Window.cs
 1. hoán đổi bộ đệm đôi; atlas.OverBudget → atlas.Reset()
 2. scale / breakpoint / atlas.Generation đổi, hoặc CacheEnabled = false → _cache.Clear()
 3. _tree.Reset(); root = _tree.NewView(Root).SizeFull()
 4. _layout.Run(root)      Measure ─► Arrange   (view: CanReplay? phát lại : RenderView() lười)
 5. Paint(root)            trạng thái chuột ─► transition ─► backdrop ─► nội dung ─► hitbox
                                             ─► clip/cuộn ─► con ─► divider/thanh cuộn ─► outline
 6. PaintOverlays          layer + Floating.Place + Blocker (modal)
 7. PruneState; ResolveFocus; _dirty.Clear; _animating ─► MarkViewDirty; UpdateHover
 8. Flush chỉ khi handler OnHover/OnScroll đã chạy trong frame
```

**Dữ liệu ở từng bước:**

| Giai đoạn | Cấu trúc | Ghi chú |
|---|---|---|
| Arena | `ElementTree` → `Node[][]` (chunk cố định 1024), `Node` chứa `Style`, `Text`/`View`/`Input`, handler, `Bounds`, `Memo` | `Div` chỉ là handle `(tree, id)` trên stack. Chunk không di chuyển nên `ref Node` luôn hợp lệ |
| Áp class | `Div.Class(str)` → `Tailwind.Get` (cache ≤ 4096) → `ClassList.ApplyBase(ref Style, VariantContext)` → `Node.Style`. Class có variant trạng thái lưu thêm ở `Node.Classes` | Chạy khi `Render()` dựng cây, **không** ở bước layout. Breakpoint và dark được đọc tại đây |
| Layout | `Node.Bounds` (logical px), `ContentSize`, `Memo` (4 kết quả đo) | `LayoutEngine.Flow / Grid / Absolute` |
| Paint | bản sao `Style` → `Primitive` (device px) trong `Scene`; `Hitbox`, `HoverRegion`, `ViewCache` | Paint không ghi vào arena |
| Scene | `Primitive[]` + `ClipShape[]` (1-based) | Renderer chỉ hiểu 4 kind |
| Renderer | CPU: `Canvas` (uint BGRA) hoặc GPU: instance buffer + atlas | |

**Layout** (`Layout/LayoutEngine*.cs`):
- `Measure(id, text, availW, availH)`: tra `Memo` trước. View chưa render thì thử `TryCachedMeasure`, không được thì `RenderView`. Text đo bằng `TextSystem.Wrap`.
- `Arrange(id, bounds)`: view mà `CanReplay` thì đặt `n.Replay = true` và **không** đi xuống con. Không thì `RenderView` rồi xếp con.
- Flex (`ArrangeFlow`): hai lượt (basis, rồi chia dòng khi `flex-wrap`); grow/shrink trong dòng; justify, align-items/align-self; đo lại chiều cao khi chiều rộng đổi.
- Grid (`GridLayout`): dùng chung cho đo và xếp, theo `arrange`.
- Absolute (`ArrangeAbsolute`): định vị theo `Inset` so với padding box của cha trực tiếp.
- Vùng cuộn: `ComputeScrollExtent` tính `ContentSize` từ con (kể cả margin cuối).

**Paint** (`App/Window.Paint.cs`, `Window.PaintParts.cs`, `Window.Effects.cs`):
1. `ApplyMouseState`: ghi `HoverRegion` nếu có hover/active/group/cursor, rồi áp variant trạng thái lên **bản sao** `Style`.
2. Transition (`ApplyTransition`) và keyframes (`Keyframes.Apply`, theo `_now`).
3. Biến đổi riêng: `Xf.Element(origin, translate, rotate, scale, skew)`, nối vào `xf` của cha.
4. `PaintBackdrop`: bóng → ring → nền và viền từng cạnh (`Quad`).
5. Nội dung: `PaintText` / `PaintInput` / `PaintVector` / `PaintImage`.
6. Hitbox: Click, Aux, Drag, Anchor, FocusInput, Key (IsKey), Scroll.
7. `overflow-hidden` → `PushClip` (clip thẳng trục, cộng `ClipShape` nếu bo góc hoặc bị xoay). Cuộn → `UpdateScroll`.
8. Con: không absolute trước, absolute sau.
9. `PaintDividers`, `PaintScrollbars`, `PaintOutline`.

**Đổi hệ toạ độ** (`Window.Effects.cs`): `Place(local, xf)` đưa hình layout về device px. Với `xf` đơn giản và `S == 1` thì `SnapToPixels` làm tròn từng **mép** (không làm tròn kích thước) để hai hình chung mép vẫn khít. Với `xf` không đơn giản (xoay, skew) thì trả về `Mat2` và tâm.

## 4. Cache view và phát lại

`ViewCache` (`App/WindowTypes.cs`) lưu, cho mỗi view ở frame trước: `ParentView`, `Measures`, `Bounds`, `Clip`, `Xf`, `InnerClip`, `Text` (kiểu chữ kế thừa), `OuterShape`, và đoạn `[Start, End)` của `Scene`, `Hitbox`, `HoverRegion`, thứ tự view.

**Phát lại** khi `CanReplay(v, bounds, text)`: cache bật, view không bẩn, có entry ở `_prevCache`, bounds và kiểu chữ trùng. `PaintViewContent` còn kiểm tra thêm:
- Clip, phần tuyến tính của `Xf` và `OuterShape` không đổi, và (không dời hoặc không có `InnerClip`) → `Replay(vid, prev, xf)`: chép đoạn Scene, hitbox, hover, và cache của các view con; nếu dời thì tịnh tiến theo `round((Tx - prevTx) * scale)` pixel device.
- Ngược lại: `RenderView` và `ArrangeChildren` tại chỗ.

Khi phát lại, các chỉ số trong hitbox được remap: `KeyParent` và `ScrollParent` nằm trong đoạn phát lại thì dời `hitOff`, nằm ngoài thì nối vào `_keyScope` / `_scrollScope` hiện tại.

**Lan truyền bẩn** (`MarkViewDirty`): đi từ view lên gốc theo `ViewCache.ParentView`, dừng khi gặp view đã bẩn. View không có trong `_cache` (chưa vẽ ở frame trước) thì bỏ qua lúc đó. `App.Flush` gọi `MarkViewDirty` cho mọi cửa sổ khi có Notify.

**Nguồn đánh dấu bẩn khác:**
- `UpdateHover`: chỉ view chứa `HoverRegion` có `Visual` và đổi trạng thái.
- `Scroll()` (lăn chuột): view **chứa** vùng cuộn (`Hitbox.ViewId`). Con là view khác được phát lại có tịnh tiến.
- `_animating` (transition, animation, cuộn đang chạy): đánh dấu bẩn ở cuối `Draw`, `WantsAnimationFrame` yêu cầu platform hẹn frame (~15 ms).

**Xoá toàn bộ cache** (`_cache.Clear()`):
- đổi `scale`;
- đổi `BreakpointBucket` (`VariantContext`);
- `atlas.Generation` đổi (chỉ số glyph hết hiệu lực);
- setter `DarkMode`;
- `CacheEnabled = false` (`Inspect`, một số test).

Vì class được áp trong `Render()`, xoá cache nghĩa là mọi view render lại. Đo bằng `LastFrameRenderedViews` / `LastFrameReplayedViews`.

## 5. State giữ qua frame

Cây được dựng lại mỗi frame, nên state được khoá bằng **ElementKey** = `(view.Id << 32) | local`, trong đó `local` là `.Key("...")` đã đăng ký hoặc bộ đếm theo view (`_keyCounters`). Chỉ gọi `ElementKey` cho node có focus/handler phím, transition hoặc cuộn, đúng một lần mỗi node.

| State | Lưu ở | Cập nhật / dọn |
|---|---|---|
| Cuộn | `_scrolls: Dictionary<long, ScrollState>` | `UpdateScroll`: áp `ScrollTo` đang chờ, kẹp về `[0, Max]`, tiến về `Target` theo hằng số 60 ms, dừng khi cách dưới 0,25 px. Vị trí dùng để tịnh tiến được làm tròn theo pixel device |
| Transition | `_transitions: Dictionary<long, TransitionState>` | Đổi đích giữa chừng thì tween mới bắt đầu từ `Current`. Mặc định 150 ms |
| Focus phần tử | `_focusKey`, `_focusVisible` | `ResolveFocus` bỏ focus nếu không còn hitbox |
| Focus ô nhập | `Focused` (`TextInput`) | Loại trừ với focus phần tử (`SetElementFocus` và `Focus` xoá nhau) |
| AutoFocus | `_autoFocus: Dictionary<long, FocusMark>` | Chỉ tự focus ở frame đầu tiên; đóng overlay thì quên mark |
| Khoá `.Key()` | `ElementTree._keyIds` (`string → id`, bit 30 bật) | `PruneKeys` khi quá 4096 khoá |

**Dọn** (`Window.State.cs`): `PruneElementState` xoá entry khi view đã cache nhưng không được vẽ frame này, hoặc view không còn sống (`App.IsAlive`). View chỉ bị ẩn (đổi tab) thì giữ state.

## 6. Input

### 6.1 Thứ tự xử lý `DispatchInput` (`App/Window.Input.cs`)

1. Overlay: `HandleOverlayInput` (Escape đóng overlay trên cùng nếu cho phép; nhấn ngoài nội dung thì đóng, và chặn lại nếu modal).
2. Nút không phải trái → `DispatchAuxButton`: nhấn và thả trên cùng hitbox, cùng nút → `OnAuxClick`.
3. `MouseMove`: `UpdateHover`, kéo chọn ô nhập, `RaiseDrag(Move)`.
4. `MouseDown`: `HitTest` → `Focus(input)` hoặc `FocusFromPointer`; `StartDrag`; lưu `_pressed` + `State` + `ViewId`.
5. `MouseUp`: `RaiseDrag(End)`; nếu hitbox trùng chỉ số, cùng view, cùng `State` thì gọi `OnClick`.
6. `Scroll` → `Window.Scroll()`. `KeyDown` → `DispatchKey`. `TextInput` → `Focused.Insert`. `ImeCompose` → `SetComposition`.
7. `App.Flush()`; trả về `_needsFrame`.

### 6.2 Hitbox và hit-test

Hitbox được thêm trong lúc Paint nên **thứ tự danh sách = thứ tự vẽ**. Chỉ số `KeyParent`, `ScrollParent` là 1-based.

`HitTest(p, clickOrInput, aux)` duyệt từ cuối, lấy phần tử đầu tiên chứa `p` (kiểm `Clip` và `Local` sau `Xf⁻¹`). Bỏ qua: `Anchor`, `DragInvoke`, `IsKey`, `ScrollKey` (khi là click), và hitbox không khớp `aux`. Gặp `Blocker` (modal) thì trả -1: mọi thứ nằm dưới modal không nhận chuột. `StartDrag` là vòng lặp riêng cho hitbox kéo.

### 6.3 Hover

`HoverRegion` theo dõi `hover:`, `active:`, `group-hover:`, `cursor-*` và `OnHover`. `UpdateHover` tính `Inside` / `Pressed`, đánh dấu bẩn nếu `Visual`, gọi `OnHover(inside)` (đặt cờ để `Draw` Flush). `_hoverFloor` và `_blockHover` làm vùng dưới modal không được coi là hover. `CursorAtMouse` cho platform biết con trỏ (`Pointer` → tay, `Text` → I-beam).

### 6.4 Lăn chuột

Win32: 120 đơn vị = một nấc = 64 logical px; Shift = cuộn ngang. `Scroll(p, delta)` chọn vùng cuộn trên cùng chứa `p` còn cuộn được theo hướng đó; gặp `Blocker` thì dừng.

### 6.5 Routing phím (`DispatchKey`, `Window.Focus.cs`)

```
KeyDown
 ├─ Focused (TextInput)?  ── HandleKey (trừ Tab/Escape) ──► true: dừng
 │                          └─ Bubble(KeyParent của ô)
 ├─ _focusKey (phần tử)?  ── OnKey (KeyInvoke) ──► true: dừng
 │                          ├─ Enter/Space + OnClick ──► dừng
 │                          └─ Bubble(KeyParent)
 └─ mặc định: Tab → MoveFocus(backward = Shift); Escape → Blur
Bubble: đi theo KeyParent tối đa 256 bước, gọi OnKey của từng mục đến khi có true
```

- **Focus visible:** `_keyboardMode` bật ở `KeyDown`, tắt ở `MouseDown`. `focus-visible:` chỉ áp khi focus đến từ bàn phím. Ô nhập luôn nhận `Focus | FocusVisible`.
- **Thứ tự Tab** = thứ tự hitbox (thứ tự vẽ), quay vòng, và chỉ trong lớp trên cùng (`InputFloor`).
- **Cuộn vào tầm nhìn** (`ScrollIntoView(at)`): đi từ vùng cuộn trong cùng ra ngoài, mỗi vùng cuộn tối thiểu để phần tử nằm trong khung (inset 2 px), đặt `Target` nên vẫn cuộn mượt. `FocusKey` và Tab đều gọi nó.

## 7. Lớp overlay

- `OpenOverlay(view, options)` thêm `Overlay` vào `_overlays`. Lớp được dựng và vẽ **sau** view gốc mỗi frame (`PaintOverlays`), không nằm trong cây của view mở nó, nên không bị cha cắt hay cuộn.
- Mỗi overlay là một `Div` lớp (`size-full`, `LayerClass` hoặc `relative` khi neo; thêm `BackdropClass` nếu modal) chứa `_tree.NewView(content)`. Khi neo, `Position = Absolute` và `Inset` lấy từ `Floating.Place`.
- **Modal**: thêm `Blocker` phủ viewport trước lớp nội dung, đặt `_hoverFloor` và `_blockHover` cho các lớp dưới. Lúc mở gọi `Focus(null)`.
- **Anchor**: `AnchorBounds(name)` đọc `_prevHitboxes` khi đang Render (vì hitbox của frame chưa đủ), ngoài ra đọc `_hitboxes`. Không tìm thấy thì dùng `AnchorRect`, rồi `LastAnchor`.
- **Định vị** `Floating.Place(anchor, size, viewport, placement, offset, padding)`: `side = placement / 3` (0 dưới, 1 trên, 2 phải, 3 trái), `align = placement % 3` (0 giữa, 1 đầu, 2 cuối). Lật sang cạnh đối diện khi `need > room(side)` và `room(opposite) > room(side)`. Sau đó kẹp vào viewport trừ `ViewportPadding`; nội dung lớn hơn viewport thì bám mép đầu.
- **Đóng**: Escape đóng overlay trên cùng nếu `DismissOnEscape`. Nhấn ngoài nội dung (trừ chính anchor) đóng nếu `DismissOnOutsideClick`; modal thì nuốt sự kiện. Overlay không đóng được và không modal thì sự kiện đi tiếp.
- **Focus**: `CloseOverlay` trả focus về `PreviousFocus` hoặc `PreviousFocusKey` nếu overlay là modal hoặc focus nằm trong nó, và quên các `AutoFocus` của nội dung. Nội dung bị `Release` thì overlay tự đóng.
- Nội dung overlay là `View` nên cache áp dụng như thường.

## 8. Chữ

### 8.1 Font

- `TextSystem.TryAddFont(path, weight, family, italic)`: đăng ký TrueType (chỉ `Font.Probe` đọc bảng thư mục; file nạp LAZY khi `FontFor` chọn tới, `ResidentFontCount`). `Font` (`Text/Font.cs`) tự parse `head`, `hhea`, `maxp`, `hmtx`, `cmap` (ưu tiên format 12, sau đó format 4), `loca`, `glyf` (kể cả glyph ghép). Kerning (GPOS/kern), GSUB (tnum, ccmp, liga, clig) và GPOS mark/mkmk: xem 8.5. **Chưa có**: OTF/CFF, TTC.
- `FontFor(family, weight, italic)` chấm điểm: `|Δweight| + 100 000` nếu sai họ `+ 10 000` nếu sai nghiêng. Không có font nghiêng thật thì `PaintRun` nghiêng giả bằng phép trượt `-0,2`.
- Windows (`RegisterDefaultFonts`): Segoe UI (thường, semibold, bold, nghiêng, bold nghiêng), Consolas (thường, bold, nghiêng). Thiếu Segoe thì dùng Arial.

### 8.2 Rasterizer và atlas

- `GlyphRasterizer` (`Text/GlyphRasterizer.cs`): tích luỹ diện tích có dấu. Đường cong bậc hai được làm phẳng; cộng dồn theo hàng khi `ToAlpha`.
- `GlyphAtlas` (`Text/GlyphAtlas.cs`): khoá `(fontId, glyph, sizeQ)`, `sizeQ = round(px × 4)`. Mỗi mask là `GlyphMask` (Alpha 8-bit, `Left`, `Top`). Hộp glyph vượt 2048 px thì mask rỗng. Bộ nhớ tính `alpha + 64` byte; `MaxBytes = 32 MB`.
- Quá ngân sách thì `Window.Draw` gọi `Reset()` ở frame kế, `Generation++` làm mọi cache view và chỉ số GPU hết hiệu lực.

### 8.3 Wrap và đo

`TextSystem.Wrap` (`Text/TextSystem.Wrap.cs`): ngắt tại khoảng trắng, từ quá dài ngắt theo ký tự, `\n` ngắt cứng; `Width` không tính khoảng trắng cuối. Cache `WrapKey(text, fontId, size, maxWidth, tracking)`: tối đa 4096 kết quả hoặc 1 MB ký tự, chạm ngưỡng thì xoá hết. `ApplyCase` có cache riêng (2048 mục). `ellipsis` dùng `FitIndex`.

### 8.5 Định hình chữ (shaping), font dự phòng, ngắt dòng, bidi

`TextSystem.Shape(text, start, end, style, visual)` (`Text/TextSystem.Shape.cs`) là đường duy nhất cho đo, ngắt dòng, vẽ và hit-test; kết quả là `ShapedGlyph[]` dùng chung (không cấp phát khi ổn định, chỉ hợp lệ tới lần Shape sau). Mỗi glyph có `Font` riêng, `Pre` (kerning), `Advance`, `XOff/YOff` (dấu), `Cluster..ClusterEnd` (UTF-16) và `Level` bidi.

- **Font dự phòng**: `TryAddFallbackFont(path)` (Core chỉ nhận font đã đăng ký; platform tự nạp font hệ thống). Chọn theo từng codepoint khi font chính cmap = 0: dự phòng đầu tiên có glyph; không có thì vẽ `.notdef` (ô vuông) của font chính. Chuỗi theo thứ tự đăng ký, không phân biệt đậm/nghiêng. Id font dự phòng ≥ `FallbackIdBase` (4096) nên khoá atlas riêng. Kerning chỉ giữa hai glyph cùng font; đường gốc / chiều cao dòng theo font chính. Cache: trang 256 codepoint (≤ 128 trang = 32 KB, quá thì xoá); font dự phòng chỉ nạp khi có ký tự thiếu glyph.
- **GSUB**: tnum, `ccmp` + `liga`/`clig` (lookup 1, 2, 4, kể cả Extension). Có letter-spacing thì tắt liga/clig (như CSS). Chưa có lookup ngữ cảnh (5/6), cờ bỏ qua dấu, `calt`, `rlig`. Ligature gộp nhiều ký tự thành một cụm: `MeasureRange` chia theo tỉ lệ ký tự khi caret rơi giữa cụm, `CaretX`/`IndexAtX` cũng vậy.
- **Dấu kết hợp**: dấu tiếng Việt tổ hợp (NFD) được ghép thành ký tự dựng sẵn nếu font có glyph (như HarfBuzz; `Vietnamese.ComposeRun`), nếu không thì GPOS mark-to-base / mark-to-mark (`Font.GposMark.cs`), advance dấu = 0. Chưa có mark-to-ligature.
- **Ngắt dòng** (`LineBreak.cs`, UAX#14 rút gọn): Latin chỉ ngắt ở khoảng trắng (không ngắt sau gạch nối, giữ nguyên snapshot); chữ biểu ý CJK/kana/full-width ngắt giữa các chữ, không ngắt trước dấu đóng/dấu câu (、。）」ー, kana nhỏ) hay sau dấu mở. Hangul theo khoảng trắng.
- **Bidi** (`Bidi.cs`, UAX#9 rút gọn): đoạn LTR một cấp nhúng, W1–W7, N1–N2, I1–I2, L2 theo nhóm (chữ + dấu), phản chiếu ngoặc. `Shape(visual: true)` trả glyph theo thứ tự hiển thị; `CaretX` / `IndexAtX` là toạ độ hiển thị ↔ vị trí logic. `MeasureRange` là bề rộng (không phụ thuộc thứ tự). **Chưa có**: ký tự điều khiển hướng tường minh (LRE/RLE/isolate), cặp ngoặc N0, đoạn RTL, **ghép chữ Ả Rập (init/medi/fina, lam-alef)**: chữ Ả Rập hiện hiện dạng isolated và sai nối nét.
- `TextInput` cần dùng `CaretX` thay `MeasureRange(lineStart, caret)` cho vị trí caret/vùng chọn để đúng với chữ RTL (với ligature thì `MeasureRange` đã đúng).

### 8.4 TextInput và IME

`TextInput` (`Widgets/TextInput.cs`) là `View` với `Text`, `Caret`, `Anchor` (vùng chọn), `Composition`. Lúc Paint, `PaintInput` ghi lại `Lines`, `Content`, `Xf`, `CaretRect` để xử lý chuột và IME.

- Sửa chữ: `ReplaceSelection` → `Vietnamese.Compose` (ghép dấu tổ hợp `e + U+0302 + U+0323` thành ký tự dựng sẵn, vì `string.Normalize` không dùng được khi `InvariantGlobalization`) → `OnChanged` (`Notify` + `Emit(TextChanged)`).
- Phím xử lý trong `HandleKey`: Backspace/Delete (theo grapheme), mũi tên, Home/End (theo dòng hiển thị), lên/xuống (theo `IndexAtX`), Ctrl+A/C/X/V, Enter (xuống dòng hoặc `Submitted`). Đang soạn IME thì `HandleKey` trả false.
- Đường gõ: Unikey/EVKey gửi Backspace (`WM_KEYDOWN`) rồi `WM_CHAR`. Telex Windows và IME CJK gửi `WM_IME_COMPOSITION` → `ImeCompose` → `SetComposition` (vẽ gạch chân trong ô). `PositionIme` đặt cửa sổ gợi ý tại `ImeCaret`.

## 9. Hình vector và ảnh

**VectorPath** (`Media/VectorPath.cs`, `PathParser.cs`): path SVG hoặc dạng tắt (`circle`, `rect`, `polyline`…) làm phẳng thành điểm và contour trong viewBox. Tạo một lần (`static readonly`).

**Raster** (`Window.PaintVector`): kích thước device `pw × ph = round(hộp × đơn vị)`. `GlyphAtlas.GetPath(path, pw, ph)` gọi `PathRasterizer`: nét là hợp các hình chữ nhật cho mỗi đoạn và đĩa tròn cho mỗi đỉnh (đầu và khớp nối tròn), lề 1 px. Kết quả là một `Primitive` `Glyph` tô bằng màu chữ, nên hai renderer vẽ nó như glyph. `object-fill` kéo giãn, còn mặc định giữ tỉ lệ và căn giữa.

**ImageSource** (`Media/ImageSource.cs`): pixel BGRA đã nhân alpha. `Id` lấy từ danh sách tĩnh `s_all` (`WeakReference`). `FromRgba` nhân alpha; `FromPng` đi qua `PngDecoder` (dùng `ZLibStream`, không interlace). `PaintImage`: `object-cover` cắt giữa, `object-contain` thu hộp và bỏ bo góc, `object-fill` kéo giãn. Primitive `Image`: `Glyph` mang `ImageSource.Id`, `Source` là vùng nguồn tính theo pixel ảnh.

## 10. Backend renderer

### 10.1 Primitive (`Scene/Scene.cs`)

| Kind | Bounds | Trường dùng | Ghi chú |
|---|---|---|---|
| `Quad` (0) | device px, trước `Transform` | `Color`, `Corners`, `BorderWidths`, `Border{Top,Right,Bottom,Left}`, `BorderStyle` | nền, viền từng cạnh (liền, gạch, chấm), bo góc |
| `Shadow` (1) | đã mở rộng theo offset và spread | `Color`, `Corners`, `Blur` | phân phối Gauss qua hàm erf |
| `Glyph` (2) | device px | `Glyph` (chỉ số atlas), `Color` | glyph và hình vector |
| `Image` (3) | device px | `Glyph` (= `ImageSource.Id`), `Source`, `Corners`, `Color.A` (độ mờ) | |

Chung cho mọi kind: `Clip` (hình chữ nhật device px), `ClipShape` (chỉ số 1-based vào `Scene.ClipShapes`), `Transform` (`Mat2` quanh tâm). `ClipShape` là hình chữ nhật bo góc có thể xoay, dùng cho `overflow-hidden` trên phần tử bo góc hoặc bị biến đổi.

### 10.2 CPU (`GotX.Raster`)

`SoftwareRenderer.Render(scene, canvas)`: xoá nền, rồi vẽ từng primitive theo thứ tự. Mỗi pixel được đưa về hệ cục bộ (`Frame.Local`, bỏ xoay và skew), tính SDF hình chữ nhật bo góc `RoundedBox`, rồi `cov = clamp(0.5 - d)`.

- Quad: viền dùng SDF bên trong; cạnh được chia theo đường chéo (`Side`); gạch và chấm dùng `Pattern`. Pixel nằm sâu bên trong được tô nhanh, không SDF, khi không biến đổi và không bị cắt hình.
- Shadow: `0.5 · (1 - erf(d / (σ√2)))` với erf Abramowitz–Stegun.
- Glyph: blit alpha (song tuyến khi biến đổi). Image: song tuyến trên ảnh premultiplied, **không có mipmap hay lọc vùng**. `Blend` là source-over premultiplied.

### 10.3 GPU (`GotX.Platform.Windows`)

- **Khởi tạo** (`D3D11Renderer.cs`): swapchain flip-discard BGRA, 2 buffer. Thử driver phần cứng, sau đó WARP. Nếu cả hai lỗi thì `TryCreate` trả null và `Win32Window` dùng CPU.
- **Instance**: mỗi primitive là một `Instance` trong `StructuredBuffer` động (`MAP_WRITE_DISCARD`, sức chứa tăng gấp đôi). Quad dùng 4 đỉnh triangle strip.
- **Vertex shader** (`D3D11Renderer.Shader.cs`): mở rộng hộp 1 px (Quad, Image), `blur × 1,5 + 1` (Shadow), không mở rộng Glyph. Xoay quanh tâm bằng `xform`. Đưa về NDC theo viewport.
- **Pixel shader**: loại pixel ngoài `clip`, nhân `clipShape` (buffer `t2`), rồi tính SDF và erf giống CPU. Glyph lấy mẫu atlas R8 (point khi không biến đổi, linear khi biến đổi).
- **Atlas glyph**: `Texture2D` R8 2048 × 2048, xếp kệ (shelf) với khe 1 px. Mỗi glyph được tải lên lần đầu dùng. Khi `Generation` đổi thì reset vị trí; khi đầy thì xếp lại từ đầu và **dựng lại frame đúng một lần**.
- **Lượt vẽ**: `Render` chia primitive theo id ảnh. Mỗi lượt là một `DrawInstanced(4, count, start)`; khi không có ảnh, cả frame là một lượt. `SV_InstanceID + baseInstance` cho chỉ số toàn cục.
- **Ảnh**: mỗi `ImageSource` có một texture BGRA, tải một lần, giải phóng sau 600 frame không dùng (đếm theo `Render`). Present vsync (`Present(swap, 1, 0)`).

Hai backend dùng chung công thức SDF và erf. `GOTX_RENDERER=software` ép dùng CPU.

## 11. Windows platform

- **Khởi động** (`WindowsPlatform.Run`): `SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2)`, lưu thread id, gọi `onLaunch`, rồi vòng `GetMessageW`. Message `WM_APP` với `hwnd == 0` gọi `Drain()`.
- **Cửa sổ** (`Win32Window`): lớp `GotXWindow` với `WndProc` là `UnmanagedCallersOnly`, tra `hwnd → Win32Window` trong dictionary tĩnh. Kích thước client = logical × DPI / 96 (`AdjustWindowRectExForDpi`). `ScaleFactor = GetDpiForWindow / 96`, đọc lại mỗi lần. `WM_DPICHANGED` áp rect gợi ý của hệ thống.
- **Message**: `WM_PAINT` → `Frame` (vẽ), rồi `Blit` (CPU, `SetDIBitsToDevice`) nếu không có GPU. `RequestAnimationFrame` đặt `SetTimer(id 1, 15 ms)` một lần; `WM_TIMER` → `RequestFrame`. `WM_SIZE` → `RequestFrame`. Chuột `SetCapture` khi nhấn trái. `WM_KEYDOWN` ánh xạ `VK` → `Key`; `WM_CHAR` ghép cặp surrogate và tạo `TextInput`. Các message IME xem mục 8.4; `WM_IME_SETCONTEXT` tắt cửa sổ soạn mặc định.
- **Exception**: `WndProc` bắt mọi lỗi và chuyển cho `WindowsPlatform.ReportException` (hook `UnhandledException`, mặc định ghi stderr). `Drain` bắt lỗi của từng việc `Post`.

## 12. Chẩn đoán

- `Window.Trace` (`Action<TraceEvent>`): `Input`, `Click`, `Key`, `Focus`, `ViewRender`, `ViewDirty`, `Overlay`, `Frame`. `TraceKind.Scroll` đã khai báo nhưng chưa phát.
- `Window.LastFrame` (`FrameStats`): thời gian layout, paint, tổng (ms); view render và phát lại; số node, primitive, hitbox, hover; byte cấp phát.
- `Window.Inspect(size, scale)` → `UiSnapshot`: vẽ một frame với cache tắt; `InspectBegin/End` ghi `UiNode` cho mỗi node (hộp layout và màn hình, clip, class, `TextStyle`, đoạn primitive `[PrimStart, PrimEnd)`).

- `Window.Log` (`PerfLog`, bật bằng `GOTX_LOG=<file>`): mỗi frame một dòng JSON (thời gian từng pha, view render, nguyên nhân bẩn, cấp phát, GC, input, độ trễ) + bản ghi `present` của platform (raster CPU / nộp GPU / Present / thời gian GPU), khởi động, cảnh báo. Tắt thì chỉ tốn một phép so sánh null. Định dạng và công cụ đọc: [performance.md](performance.md).

`GotX.Test`: `UiTest.Open` dùng đồng hồ giả. `Step()` cộng 16 ms, chạy `RunTimers`, `Draw`, `Flush`. `Settle` dựa trên `FramePending` và `WantsAnimationFrame`. Ảnh chụp dùng `SoftwareRenderer.Render`. Chi tiết: [testing.md](testing.md) §8.

## 13. Lớp component

Hai loại: hàm tĩnh `Ui.<Tên>(w, …)` không state, và lớp `View` có state (`App.New` rồi `w.View(x)`). `Theme.SetColor(name, light, dark)` (`Styling/Theme.cs`) đổi token theo `DarkMode`. Chi tiết: [components.md](components.md).

## 14. Điểm mở rộng và giới hạn đã kiểm chứng

**Điểm mở rộng**: primitive mới (thêm `PrimitiveKind`, sửa cả hai renderer); handler mới trên `Div` (thêm trường `Node`, hitbox, nhánh `Paint`, và remap trong `Replay` nếu có chỉ số); nền tảng mới (`IPlatform` + `IPlatformWindow`, `Draw(Scene)` là điểm nối renderer); font (`TextSystem.TryAddFont`).

**Giới hạn**
- Chỉ có một nền tảng (Win32); `IPlatform` đã tách nhưng chưa có bản thứ hai.
- Không có damage region: mọi frame vẽ lại toàn bộ Scene. Cache giảm công layout và render view, không giảm số primitive gửi GPU.
- Thứ tự Tab = thứ tự vẽ, không có `tabindex`.
- Layout: chưa có `margin: auto`, `align-content`, `col-start`. Thanh cuộn không kéo được.
- Chữ: không kerning, shaping, font fallback, TTC/CFF.
- Ảnh CPU không có lọc vùng (mục 10.2).
- Một luồng UI, không có khoá. Một view bẩn được `Render()` lại toàn bộ; cuộn làm bẩn view chứa vùng cuộn.
- `Notify` gọi trong lúc vẽ (ngoài handler OnHover/OnScroll) không được Flush trong `Draw`; nó chờ lần `Flush` kế (input, timer, `Post`).
