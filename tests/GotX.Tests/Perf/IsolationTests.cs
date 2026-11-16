using GotX;
using GotX.Test;
using GotX.Widgets;

/// <summary>
/// Cô lập cập nhật: con trỏ nháy / animation / hover chỉ Render đúng view đó (CountRenders) và vùng hỏng gửi cho renderer
/// chỉ xấp xỉ hộp thay đổi, không phải cả trang (xem docs/performance.md §6).
/// </summary>
static class IsolationTests
{
    sealed class Spinner : View
    {
        public override Div Render(Window w) => w.Div().Key("spin").Class("size-6 rounded-full border-2 border-blue-500 animate-spin");
    }

    sealed class Page(bool spin = true) : View
    {
        public TextInput Field = null!;
        View _spin = null!;
        protected override void OnCreated()
        {
            Field = App.New(new TextInput { Multiline = false, Text = "hello", Blink = true, BoxClass = "h-8 w-56 px-3 border bg-white text-slate-900", AriaLabel = "f" });
            _spin = App.New(new Spinner());
        }

        public override Div Render(Window w)
        {
            var list = w.Div().Class("flex flex-col");
            for (int i = 0; i < 40; i++) list.Child(w.Div().Class("px-3 py-1 text-sm border-b").Child($"Dòng {i} lorem ipsum dolor sit amet"));
            return w.Div().Class("size-full bg-white p-4 flex flex-col gap-3 text-slate-900")
                .Child(w.Div().Key("field").Child(w.View(Field))).Child(spin ? w.View(_spin) : w.Div()).Child(list);
        }
    }

    static (int tiles, Bounds box) Damaged(SceneDamage.Result r, int w, int h) => (r.Full ? int.MaxValue : r.DirtyTiles, r.BoundingRect(w, h));

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/isolation/caret-blink-touches-only-the-caret", t =>
        {
            const int W = 700, H = 500;
            t.Open(app => app.New(new Page(spin: false)), new Size(W, H));
            t.Click(new Point(60, 36));
            t.Draw();
            var damage = new SceneDamage();
            damage.Update(t.Window.LastScene, W, H); // frame đầu: Full
            int checkedFrames = 0, maxTiles = 0;
            for (int i = 0; i < 12; i++)
            {
                int renders = t.Perf.CountRenders(() => t.Advance(0.55));
                var scene = t.Window.LastScene;
                var (tiles, box) = Damaged(damage.Update(scene, W, H), W, H);
                if (renders == 0) continue; // chưa tới nhịp nháy
                checkedFrames++;
                maxTiles = Math.Max(maxTiles, tiles);
                t.Assert.True(t.Window.LastFramePartial && renders <= 2, $"nhịp nháy chỉ render view ô nhập ({renders} lượt, partial={t.Window.LastFramePartial})");
                t.Assert.True(tiles <= 2, $"vùng hỏng của một nhịp nháy ≤ 2 ô 64×64 ({tiles} ô, hộp {box})");
            }
            t.Assert.True(checkedFrames >= 8, $"đã kiểm {checkedFrames} nhịp nháy");
            t.Notes.Add($"{checkedFrames} nhịp nháy, tối đa {maxTiles} ô hỏng trên {(W + 63) / 64 * ((H + 63) / 64)} ô");
        });

        suite.Add("perf/isolation/spinner-rerenders-only-its-view", t =>
        {
            const int W = 700, H = 500;
            t.Open(app => app.New(new Page()), new Size(W, H));
            t.Draw();
            var damage = new SceneDamage();
            damage.Update(t.Window.LastScene, W, H);
            int total = 0, maxTiles = 0;
            for (int i = 0; i < 20; i++)
            {
                total += t.Perf.CountRenders(() => t.Frames(1));
                var (tiles, _) = Damaged(damage.Update(t.Window.LastScene, W, H), W, H);
                maxTiles = Math.Max(maxTiles, tiles);
            }
            t.Assert.True(total <= 20, $"animate-spin render đúng một view mỗi frame: {total} lượt trong 20 frame");
            t.Assert.True(maxTiles <= 2, $"vùng hỏng của spinner ≤ 2 ô ({maxTiles})");
            t.Notes.Add($"spinner: {total} lượt render / 20 frame, tối đa {maxTiles} ô hỏng");
        });
    }
}
