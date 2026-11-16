using GotX;
using GotX.Raster;
using GotX.Test;

/// <summary>
/// View bẩn đổi kích thước (không giữ được ranh giới layout) thì view cha gần nhất đủ điều kiện vẽ lại từng phần thay vì dựng cả frame;
/// ảnh phải giống từng pixel với cửa sổ tắt khung một phần.
/// </summary>
static class BoundaryPropagationTests
{
    sealed class Label : View
    {
        public string Text = "ngắn";
        public override Div Render(Window w) => w.Div().Class("rounded bg-blue-100 px-2 py-1 text-sm").Child(Text);
    }

    /// <summary>Hàng flex: nhãn đổi bề rộng đẩy các anh em; cha của nó (Row) hấp thụ thay đổi vì kích thước của chính Row khớp với cha.</summary>
    sealed class Row : View
    {
        public Label A = null!;
        protected override void OnCreated() => A = App.New(new Label());
        public override Div Render(Window w) =>
            w.Div().Class("flex h-10 w-[600px] items-center gap-3 bg-slate-50 p-2")
                .Child(w.View(A)).Child(w.Div().Class("rounded bg-green-200 px-2").Child("anh em"))
                .Child(w.Div().Class("ml-auto text-xs").Child("cuối hàng"));
    }

    sealed class Page : View
    {
        public Row Row = null!;
        protected override void OnCreated() => Row = App.New(new Row());
        public override Div Render(Window w) =>
            w.Div().Class("flex size-full flex-col gap-4 bg-white p-4 text-slate-900")
                .Child(w.Div().Class("text-lg font-semibold").Child("Tiêu đề trang"))
                .Child(w.View(Row))
                .Child(w.Div().Class("h-24 rounded border").Child("khối bên dưới"));
    }

    sealed class Rig
    {
        public readonly App App = new();
        public readonly Window Window;
        public readonly Page Page;
        double _now = 1000;
        public Rig(bool partial)
        {
            App.Clock = () => _now;
            Window = new Window(App, TestFonts.Shared) { Clock = () => _now, PartialFrames = partial };
            Window.Root = Page = App.New(new Page());
            Step(); Step();
        }
        public Scene Step() { _now += 0.016; App.RunTimers(); var s = Window.Draw(new Size(700, 300), 1); App.Flush(); return s; }
    }

    static bool SamePixels(Scene a, Scene b)
    {
        var ca = new Canvas(); ca.Resize(700, 300); var cb = new Canvas(); cb.Resize(700, 300);
        SoftwareRenderer.Render(a, ca); SoftwareRenderer.Render(b, cb);
        return ca.Pixels.AsSpan(0, 700 * 300).SequenceEqual(cb.Pixels.AsSpan(0, 700 * 300));
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/partial/boundary-failure-propagates-to-ancestor", t =>
        {
            var on = new Rig(partial: true); var off = new Rig(partial: false);
            string[] texts = ["một nhãn dài hơn hẳn lúc đầu", "ngắn", "vừa vừa", "x", "ngắn"];
            int partial = 0;
            foreach (string s in texts)
            {
                on.Page.Row.A.Text = off.Page.Row.A.Text = s;
                on.Page.Row.A.Notify(); off.Page.Row.A.Notify();
                var sa = on.Step(); var sb = off.Step();
                if (on.Window.LastFramePartial) partial++;
                if (!SamePixels(sa, sb)) { t.Fail($"nhãn \"{s}\": ảnh khung một phần khác dựng đầy đủ"); return; }
            }
            t.Assert.Equal(partial, texts.Length, "nhãn đổi bề rộng được cha hấp thụ: mọi frame đều là khung một phần");
            t.Assert.Equal(on.Window.PartialFallbackCount, 0, "không lùi về khung đầy đủ");
        });
    }
}
