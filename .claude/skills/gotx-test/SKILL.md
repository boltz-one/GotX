---
name: gotx-test
description: Write and run GotX UI tests - asserts, lint, snapshots.
metadata:
  origin: user
  version: "1.0.0"
---

# GotX Test

Viết và chạy test giao diện bằng `src/GotX.Test` (headless, không cần cửa sổ). Tài liệu đầy đủ: `docs/testing.md`. Bộ test của repo: `tests/GotX.Tests`.

## Chạy

```
dotnet run --project tests/GotX.Tests -- [--filter chuỗi] [--out thư-mục] [--list] [--verbose] [--update-snapshots] [--update-baseline]
```

Test đạt không in gì. Mỗi test hỏng in một khối: điều sai + số đo, đường dẫn phần tử + hộp `[x,y w×h]`, file ảnh khoanh vùng, bản in cây, nhật ký trước lỗi. Mã thoát 0/1.

## Viết test

```csharp
suite.Add("counter/click", t =>
{
    t.Open(new CounterView());                 // cache view BẬT, 900×700 @1x; đồng hồ ảo lái Window và App timer
    t.Click("inc");                            // theo .Key(); cũng có Click(point), ClickText("Lưu")
    t.Assert.Text("value", "Giá trị: 1");      // sai → ghi lỗi, test chạy tiếp
    t.Press(Key.Tab); t.Assert.Focused("inc");
    t.Assert.NoLint();
});
```

Đăng ký test trong `tests/GotX.Tests/Program.cs` (tường minh, không reflection). Phần tử cần tra phải có `.Key("tên")`.

| Việc | API |
|---|---|
| Input | `Click RightClick ClickText Hover MouseLeave Drag Press Type Scroll` |
| Thời gian | `Advance(giây)` (chạy timer + vẽ), `Frames(n)`, `Settle()` |
| Tìm | `t.Get("key")`, `t.Find`, `t.Tree.FindText`, `FindAll`, `TopAt(point)`; `UiNode.Screen / Visible / Classes / Text` |
| Khẳng định | `Exists NotExists Visible Hidden Text TextContains Size Position Aligned(Edge, tol, keys…) Inside NoOverlap Pixel Focused NoLint NoNewLint True Equal Near` |
| Ảnh chuẩn | `t.Snapshot("tên")` → `__snapshots__/tên.png`; lệch thì báo vùng khác thuộc phần tử nào |
| Hiệu năng | `t.Perf.ExpectIdle()`, `ExpectRenders(() => t.Click("x"), max: 2)`, `ExpectFrameBudget(ms)`, `ExpectAllocations(bytes)`, `ExpectReplayMatchesFresh()` |
| Soi cả view | `UiDoctor.Run(app => app.New(new V()), new DoctorOptions { OutDir = "out" }).ToText()` |

## Quy tắc

- Đo **hành vi thật**: state sau thao tác, vị trí, focus, màu pixel. Không mock, không dữ liệu giả để cho qua.
- Mỗi tính năng mới: 1 test hành vi + `t.Assert.NoLint()` (hoặc `NoNewLint` nếu có vấn đề đã biết trong baseline).
- Vẽ bằng `t.Draw()/Step()/Advance()`, không gọi thẳng `Window.Draw` (cây và ảnh đã nhớ sẽ cũ).
- Đừng đếm lượt render bằng biến trong `Render()` (frame soi render lại mọi view): dùng `t.Perf.CountRenders`.
- Thời gian: chỉ dùng đồng hồ ảo (`Advance`), không `Thread.Sleep`.
- Test tương tác có trạng thái focus/cache: để cache BẬT (mặc định) — lỗi phát lại cache chỉ lộ ra khi đó.
- Lint báo vấn đề thật thì **sửa giao diện**, không tắt luật. Chỉ `Suppress(luật, mẫu)` khi là chủ ý, kèm chú thích vì sao.
- Baseline (`tests/GotX.Tests/lint-baseline.txt`): chỉ vấn đề *mới* làm hỏng test. Sửa xong một vấn đề → `--update-baseline` để dọn dòng cũ. Không dùng `--update-baseline` để giấu lỗi vừa tạo ra.
- `--update-snapshots` chỉ sau khi đã mở `.diff.png` và xác nhận thay đổi là đúng ý.

## Trước khi báo xong

```
dotnet build GotX.slnx                                   # 0 cảnh báo, 0 lỗi
dotnet run --project tests/GotX.Tests                    # 0 hỏng
dotnet run --project samples/ComponentsCheck -- <out>   # nếu đụng component
dotnet run --project samples/Headless -- <out>          # nếu đụng Core
```

Test hỏng → skill `gotx-debug`.
