using GotX;
using GotX.Test;
using GotX.Widgets;

/// <summary>Trạng thái ổn định phải gần 0 B/frame: gõ phím, nhấp nháy con trỏ, hover (xem docs/performance.md §6).</summary>
static class ZeroAllocTests
{
    sealed class Host : View
    {
        public TextInput Field = null!;
        protected override void OnCreated() =>
            Field = App.New(new TextInput { Multiline = false, Text = "", Blink = false, AriaLabel = "f", BoxClass = "h-8 w-60 px-3 border bg-white text-slate-900" });
        public override Div Render(Window w) =>
            w.Div().Class("size-full bg-white p-4").Child(w.Div().Key("field").Child(w.View(Field)));
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/alloc/typing-allocates-near-zero", t =>
        {
            t.Open(new Host());
            t.Click("field");
            string alphabet = "abcdefghijklmnopqrstuvwxyz";
            for (int i = 0; i < 40; i++) t.Type(alphabet[i % 26].ToString()); // khởi động cache / JIT
            long total = 0; int n = 0;
            var w = t.Window;
            for (int i = 0; i < 20; i++)
            {
                string s = alphabet[i % 26].ToString();
                w.DispatchInput(new InputEvent(InputKind.TextInput, Text: s));
                long a0 = GC.GetAllocatedBytesForCurrentThread();
                w.Draw(t.Size, 1);
                long d = w.LastFrameAllocatedBytes;
                t.Notes.Add($"frame alloc {d}");
                total += d; n++;
            }
            t.Notes.Add($"avg {total / n} B/frame");
        });
    }
}
