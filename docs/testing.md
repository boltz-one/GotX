# GotX.Test – test và soi lỗi giao diện

`src/GotX.Test` là khung test/debug headless cho GotX (chỉ dùng BCL, AOT-compatible, không reflection). Mục tiêu: để một
agent "nhìn" được giao diện qua terminal – cây phần tử, hộp, lỗi bố cục, pixel, số lượt render – với đường dẫn phần tử
và đường dẫn file bằng chứng trong mọi thông báo. Bộ test của repo nằm ở `tests/GotX.Tests`.

## 1. Chạy

```
dotnet run --project tests/GotX.Tests -- [--filter chuỗi] [--out thư-mục] [--list] [--verbose]
                                        [--update-snapshots] [--update-baseline]
```

- Test đạt không in gì (trừ `--verbose`); mỗi test hỏng in một khối; cuối cùng một dòng tổng kết. Mã thoát 0/1 (2 = sai tham số).
- `--out`: nơi ghi ảnh/bản in cây/`report.json` (mặc định `test-out` cạnh file chạy, tức trong thư mục build).
- `--update-snapshots` ghi đè ảnh chuẩn bị lệch; `--update-baseline` ghi lại `lint-baseline.txt` cho các test đã chạy.

## 2. Viết test

```csharp
var suite = new TestSuite();                       // đăng ký tường minh, tên dạng nhóm/tên
suite.Add("counter/click", t =>
{
    var view = t.Open(new CounterView());          // App + Window headless, cache view BẬT, 900×700 @1x
    t.Click("inc");                                // theo .Key(); hoặc Click(point), ClickText("Lưu")
    t.Assert.Text("value", "Giá trị: 1");          // sai → ghi lỗi, test chạy tiếp
    t.Assert.NoLint();
});
return suite.Run(args);
```

- `Open(view | app => view, size, scale, dark, cache)`; gọi lại để mở cấu hình khác. Một đồng hồ ảo (`t.Now`) lái cả `Window.Clock` và `App.Clock`.
- Input: `Click / RightClick / ClickText / Hover / MouseLeave / Drag(from, to) / Press(Key, Modifiers) / Type(text) / Scroll(key|point, dy)`. Mỗi thao tác vẽ 2 frame.
- Thời gian: `Advance(giây)` (bước 16 ms: đẩy đồng hồ → `RunTimers` → vẽ), `Frames(n)`, `Step()`, `Settle()` → `false` nếu không bao giờ đứng yên.
- Mọi truy vấn đi qua `t.Tree` (một frame `Window.Inspect`, nhớ tới frame kế). Luôn vẽ bằng `t.Draw()/Step()`, đừng gọi thẳng `Window.Draw` (cây và ảnh đã nhớ sẽ cũ).
- **Lưu ý:** frame soi render lại MỌI view → đừng đếm lượt render bằng biến đếm trong `Render()`; dùng `t.Perf.CountRenders` (đếm theo Trace, bỏ qua frame soi).
- Assertion hỏng cần bằng chứng "sai có chủ ý"? Chạy test con: xem `Kit.Probe` trong `tests/GotX.Tests/Self/SelfTestKit.cs`.

## 3. Các khả năng

| Việc | API | Ghi chú |
|---|---|---|
| Tìm phần tử | `t.Get("key")` (thiếu → dừng test), `t.Find`, `t.Tree.FindText("…")`, `FindAll(pred)`, `FindView("Kiểu")`, `Ancestors`, `Descendants`, `OwnerView`, `TopAt(point)` | `UiNode`: `Screen` (hộp thật), `Layout`, `Visible` (= Screen ∩ Clip), cờ, style đã tính |
| Đường dẫn | `t.Tree.Path(node)` | `Host >> CounterView > div[1] > div#save .px-3 h-8…` – giữ view, node có khoá, hai mức cuối; `>>` = bỏ qua các mức vô danh; `[n]` = thứ tự trong cha |
| Bản in cây | `t.Dump(new DumpOptions { RootKey, MaxDepth, OnlyVisible, Mark })`, `TreeDump.ToJson(t.Tree)` | cờ: `clipped hidden click focusable focused scroll disabled hover xf abs` |
| Assertion | `t.Assert.Exists/NotExists/Visible/Hidden/Text/TextContains/TextVisible/Size/Position/Aligned(Edge, tol, keys…)/Inside/NoOverlap/Pixel/Focused/NotFocused/Sequence/NoLint/NoNewLint/True/Equal/Near` | `Edge`: Left Right Top Bottom CenterX CenterY Baseline |
| Lint | `t.Lint(options)` → `List<LintIssue>`; `t.Assert.NoLint()` | mục 5 |
| Soi một view | `UiDoctor.Run(app => view, new DoctorOptions { Sizes, Dark, Scales, OutDir, Prepare })` → `.ToText()` | ma trận kích thước × sáng/tối × scale, gộp trùng, xếp hạng, ảnh có đánh số |
| So ảnh | `t.Snapshot("tên", tolerance, maxDiffPixels)` | `__snapshots__/tên.png` cạnh dự án test; lệch → `.actual.png`, `.diff.png`, vùng khác → phần tử |
| Vì sao pixel có màu này | `t.Explain(point)` | mọi primitive phủ pixel theo thứ tự vẽ + node sở hữu + màu cuối |
| Ảnh khoanh vùng | `t.Save("tên", node…)`; tự động khi lỗi | đỏ = phần tử chính, cam = liên quan, tím = vùng |
| Hiệu năng | `t.Perf.Measure(frames)`, `ExpectFrameBudget(ms)`, `ExpectAllocations(bytes)`, `ExpectIdle()`, `ExpectSettles()`, `ExpectRenders(() => t.Click("x"), max: 2)`, `CountRenders`, `ExpectReplayMatchesFresh()` | mục 6 |
| Nhật ký | `t.Trace.Mark()`, `Since(mark)`, `Count(kind)`, `Names(kind)`, `Tail(n)`; `t.Assert.Sequence(mark, (TraceKind.Click, "Counter"), …)` | 30 sự kiện cuối tự kèm vào mỗi lỗi |

```csharp
t.Assert.Aligned(Edge.CenterY, 0.5f, "icon", "label", "badge");   // lỗi nêu toạ độ từng cái và cái lệch nhất
t.Assert.Pixel(new Point(40, 40), Rgba.FromHex(0x0ea5e9));          // lỗi kèm sẵn kết quả Explain
Console.Write(t.Dump(new DumpOptions { RootKey = "toolbar", MaxDepth = 2 }));
Console.Write(UiDoctor.Run(app => app.New(new MyView()), new DoctorOptions { OutDir = "out" }).ToText());
```

## 4. Đọc một khối lỗi

```
FAIL  form/save (38.29 ms) — 2 lỗi
  ✗ Text: #c-value mong đợi "Giá trị: 5", thực tế "Giá trị: 1"        ← điều sai, có số đo
    tại:  CounterHost >> CounterView > div[1] > div#c-value .text-sm [91,30 52×20]   ← đường dẫn + hộp [x,y w×h]
    file: …/form_save.fail1.png          ← ảnh, phần tử khoanh đỏ
    file: …/form_save.fail1.tree.txt     ← bản in cây, node lỗi có ">>" đầu dòng
    trước đó:                            ← nhật ký: input → click → ViewDirty → ViewRender
      [5] Click: CounterView (view #2) @ 24,24 55×32
```
Chi tiết đầy đủ (mọi lỗi, toàn bộ trace, stack) nằm trong `report.json`.

## 5. Luật lint

UiDoctor chạy mọi luật ở scale 1 / 1.25 / 1.5 / 2. Bỏ qua theo mẫu:
`new LintOptions().Suppress("small-target", "close-btn").Disable("low-contrast").Only(…)`; ngưỡng: `MinTarget`, `AlignMax`, `Tolerance`, `ContrastWarn/Info`.

| Luật | Bắt gì | Heuristic / giới hạn |
|---|---|---|
| `subpixel` | nền/viền/ảnh có mép không trúng pixel thiết bị (> 0.02) → nhoè | Core tự bắt mép vào lưới pixel khi vẽ (kể cả toạ độ lẻ do layout và DPI 125–150%), nên luật này chỉ còn bắt phần tử bị `scale-*` lúc đứng yên. Không tính chữ/icon |
| `misaligned` | anh em cùng hàng/cột mà đỉnh, tâm và đáy ĐỀU lệch, độ lệch nhỏ nhất trong [0.75, 2] px (lệch tâm đúng 0.5 px giữa hộp rộng chẵn và lẻ là tất yếu sau khi bắt vào lưới pixel) | Cùng hàng = đứng cạnh nhau + khoảng dọc trùng ≥ 50% cái thấp hơn. Bỏ qua cặp chênh kích thước > 1.6 lần, phần tử absolute/xoay. Chỉ so anh em trực tiếp (không so "anh em họ"). Một phần tử lệch trong hàng chỉ báo một lần |
| `overlap` | anh em trong luồng đè nhau > 1 px cả hai trục | Bỏ qua margin âm (`-ml-2`, `-space-x-2`) và ô grid trùng hẳn nhau |
| `overflow` | phần nhìn thấy của con thò ra ngoài cha không cắt/cuộn > 0.5 px | Bỏ qua con absolute, margin âm, node chữ; phần tràn đã bị tổ tiên xa cắt thì không tính |
| `text-overflow` | chữ (đo lại bằng kiểu chữ đã tính) rộng/cao hơn hộp chữ hoặc padding box của cha, không có `truncate` | Nêu rõ "bị cắt" hay "đè lên phần bên cạnh". Coi như căn trái khi tính phía tràn |
| `clipped` | chữ/icon/ảnh/ô nhập/vùng bấm bị `overflow-hidden` cắt MỘT PHẦN | Trong vùng cuộn thì bỏ qua; bị cắt hết (accordion đóng) không báo; không biết vùng cắt bo góc |
| `zero-size` | có viền (warning) hoặc nền (info) mà rộng/cao = 0 | Thanh tiến độ 0% sẽ ra info |
| `offscreen` | nằm hẳn ngoài cửa sổ, không thuộc vùng cuộn | Chỉ báo node ngoài cùng; bỏ qua phần tử xoay/skew |
| `small-target` | vùng bấm có cạnh < 16 px | Không xét vùng bấm cha bao ngoài |
| `occluded` | tâm phần tử bấm/focus được nằm dưới vùng bấm vẽ sau (không phải con nó) | Chỉ trong cùng lớp (overlay che nền là chủ ý); không xét lớp phủ không bấm được |
| `low-contrast` | tương phản WCAG chữ/nền < 3 (warning), < 4.5 (info; chữ lớn miễn) | Nền = màu trội trong hộp chữ trên ẢNH đã dựng. Bỏ qua chữ disabled, chữ dưới overlay; nền là ảnh/nhiều màu thì kém chính xác |
| `duplicate-key` | cùng `.Key()` hai lần trong một view (error) | View khác nhau dùng cùng khoá thì hợp lệ |
| `unknown-class` | token Tailwind Core không hỗ trợ (error) | Hỏi lại `Tailwind.Parse` từng token |

**Baseline:** `t.Assert.NoNewLint("nhóm", issues)` so với `tests/GotX.Tests/lint-baseline.txt` (dòng: `mức | luật | ×số | đường dẫn  ## ghi chú`,
khoá = luật + đường dẫn). Chỉ vấn đề mới làm hỏng test; sửa xong thì xoá dòng hoặc `--update-baseline`.

## 6. Hiệu năng

- `Measure` trả min/trung vị/p95 của layout, paint, toàn bộ Draw, raster CPU (đo riêng), cấp phát/frame, số view render/phát lại. Trace bị gỡ trong lúc đo.
- `ExpectIdle`: sau khi đứng yên, các frame tiếp theo phải render 0 view. Lỗi liệt kê view đã render + view bị ViewDirty.
- `ExpectRenders(action, max)`: một thao tác chỉ render lại từng ấy lượt view (Notify một view làm bẩn cả các view cha).
- `ExpectReplayMatchesFresh`: ảnh từ frame có cache phải giống hệt ảnh dựng lại từ đầu – bắt lỗi phát lại nội dung cũ.
- Ngân sách thời gian dùng trung vị và nên đặt rộng (bắt thoái hoá cỡ bậc độ lớn, không phải đo chính xác).

## 7. Gỡ lỗi thường gặp

- **Lệch pixel:** `t.Lint(new LintOptions().Only("misaligned", "subpixel"))` → đọc Δtop/Δcenter/Δbottom; `t.Dump(RootKey)` để xem hộp từng node; khoá lại bằng `Assert.Aligned`. Mép nhoè → `subpixel` (chỉ còn xảy ra với phần tử đang `scale-*`; toạ độ lẻ do layout đã được Core bắt vào lưới pixel).
- **Bị cắt / tràn:** `Only("clipped", "overflow", "text-overflow")`; trong bản in cây tìm cờ `clipped` / `hidden`; so `Screen` với `Clip` của node; mở ảnh `fail*.png` (đỏ = phần tử, cam = tổ tiên cắt).
- **Render chậm / render lại quá nhiều:** `t.Perf.ExpectIdle()` rồi `t.Perf.ExpectRenders(() => thao tác, max)`; đọc "view đã render" và "view bị đánh dấu bẩn"; `t.Trace.Since(mark)` để thấy chuỗi nhân quả. Animation (`animate-*`) đặt trong view lớn làm cả view render mỗi frame → tách nó vào view con.
- **Pixel sai màu:** `Console.Write(t.Explain(point))` → lớp cuối danh sách là thứ nằm trên cùng; cột cuối là node vẽ ra nó.
- **Click không ăn:** `Only("occluded")`; `t.Tree.TopAt(point)`; trace có `Input MouseDown` mà không có `Click` nghĩa là không hitbox nào nhận.
- **Giao diện đổi ngoài ý muốn:** `t.Snapshot("tên")` → báo vùng khác thuộc phần tử nào; mở `.diff.png`.

## 8. Cấu trúc

`UiTest*.cs` harness · `UiTree*.cs` truy vấn + đường dẫn · `UiAssert*.cs` · `TreeDump.cs` · `Lint/` (luật, tuỳ chọn, baseline) ·
`Visual/` (so ảnh, giải thích pixel, khoanh vùng) · `UiPerf*.cs` · `TraceLog.cs` · `Runner/` · `UiDoctor.cs`.
Core cung cấp qua `src/GotX.Core/Diagnostics` và `Window.Diagnostics.cs`: `Window.Trace`, `Window.LastFrame`, `Window.Inspect`.
