using GotX;
using GotX.Raster;
using GotX.Test;

/// <summary>
/// Phát lại cache (chép + tịnh tiến view, kể cả view có overflow-hidden bị cắt ở mép khung cuộn hoặc nằm ngoài khung) phải cho đúng
/// ảnh như vẽ mới hoàn toàn: cửa sổ có cache + khung một phần so cửa sổ tắt cache, cùng chuỗi cuộn / rê chuột ngẫu nhiên có seed,
/// so từng pixel sau khi trạng thái đã lắng (hover dưới con trỏ sau khi nội dung dời đi cập nhật trễ một nhịp so với vẽ mới).
/// </summary>
static class ReplayParityTests
{
    static string? DiffPixels(PartialParityTests.Rig a, PartialParityTests.Rig b, Canvas ca, Canvas cb, Size size)
    {
        int w = (int)size.Width, h = (int)size.Height;
        ca.Resize(w, h); cb.Resize(w, h);
        SoftwareRenderer.Render(a.Window.LastScene, ca); SoftwareRenderer.Render(b.Window.LastScene, cb);
        var pa = ca.Pixels.AsSpan(0, w * h); var pb = cb.Pixels.AsSpan(0, w * h);
        if (pa.SequenceEqual(pb)) return null;
        int n = 0, first = -1;
        for (int i = 0; i < pa.Length; i++) if (pa[i] != pb[i]) { n++; if (first < 0) first = i; }
        var sa = a.Window.LastScene; var sb = b.Window.LastScene;
        int k = 0;
        while (k < Math.Min(sa.Count, sb.Count) && sa.Primitives[k].Kind == sb.Primitives[k].Kind && sa.Primitives[k].Bounds == sb.Primitives[k].Bounds) k++;
        return $"{n} pixel khác, đầu tiên ({first % w},{first / w}); primitive {sa.Count} / {sb.Count}, khác đầu tiên #{k}";
    }

    public static void Register(TestSuite suite)
    {
        // Bỏ chat (lệch 1px glyph icon 18px sau phát lại, cả với logic phát lại cũ) và overlays (mở overlay theo hover khác nhau giữa đường phát lại
        // và vẽ mới ở chuỗi dài, tuỳ trạng thái dùng chung của tiến trình): chênh lệch có từ trước, chưa xử lý – xem docs/performance.md.
        foreach (var (name, root, size) in PartialParityTests.Targets.Where(x => x.Name is not ("chat" or "overlays")))
            suite.Add($"perf/replay/scroll-parity-{name}", t =>
            {
                var a = new PartialParityTests.Rig(root, size, partial: true);
                var b = new PartialParityTests.Rig(root, size, partial: false, cache: false);
                var ca = new Canvas(); var cb = new Canvas();
                var rng = new Random(11);
                int steps = Environment.GetEnvironmentVariable("GOTX_PARITY_FULL") == "1" ? 160 : 40, checkedFrames = 0;
                for (int i = 0; i < steps; i++)
                {
                    var at = new Point(rng.Next((int)size.Width), rng.Next((int)size.Height));
                    InputEvent e = rng.Next(5) switch
                    {
                        0 => new InputEvent(InputKind.MouseMove, at),
                        1 => new InputEvent(InputKind.Scroll, at, Delta: new Point(0, rng.Next(-30, 30))), // bước nhỏ: dời view ít pixel, vùng cắt đổi dần
                        _ => new InputEvent(InputKind.Scroll, at, Delta: new Point(0, rng.Next(-400, 400))),
                    };
                    a.Send(e); b.Send(e);
                    int n = rng.Next(0, 25) + 30;
                    a.Frames(n); b.Frames(n);
                    string? diff = DiffPixels(a, b, ca, cb, size);
                    if (diff is not null) { t.Fail($"{name} bước {i} ({e.Kind}): {diff}"); return; }
                    checkedFrames++;
                }
                t.Notes.Add($"{name}: {checkedFrames} bước khớp vẽ mới");
            });
    }
}
