namespace GotX.Stress;

static class ResizeScenario
{
    static readonly float[] Dpis = [1f, 1.25f, 1.5f, 2f, 1f];

    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        var rig = new Rig(text);
        var win = rig.Add(a => a.New(new MixedPage()), new Size(1000, 700));
        var m = Scenarios.Begin(rig, win);
        // Sinusoidal sweep so widths cross every breakpoint; DPI changes every 25 frames (forces full rebuild of caches keyed by scale).
        for (int f = 0; f < 300; f++)
        {
            double t = f / 300.0 * Math.PI * 4;
            m.Add(rig.Frame(win, () =>
            {
                win.Size = new Size((float)(960 + 640 * Math.Sin(t)), (float)(700 + 300 * Math.Cos(t * 1.7)));
                win.Scale = Dpis[f / 25 % Dpis.Length];
            }));
        }
        return m.Finish();
    }
}

/// <summary>Pathological text: very long wrapped lines, one huge unbreakable word, emoji ZWJ/flags, RTL, Vietnamese precomposed + decomposed + stacked marks.</summary>
public sealed class TextPage : View
{
    public static readonly string[] Blocks =
    [
        string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog, again and again. ", 330)), // ~20k chars
        new string('W', 5000), // no break opportunity
        string.Concat(Enumerable.Repeat("😀👨‍👩‍👧‍👦🇻🇳❤️🏳️‍🌈👍🏽 ", 200)),
        string.Concat(Enumerable.Repeat("مرحبا بالعالم هذا نص طويل للاختبار والتفاف الأسطر ", 80)),
        string.Concat(Enumerable.Repeat("שלום עולם זהו טקסט ארוך לבדיקה mixed with English 123 ", 80)),
        string.Concat(Enumerable.Repeat("Tiếng Việt có dấu: Nguyễn Thị Thu Hương, đường Điện Biên Phủ. ", 120)), // precomposed
        string.Concat(Enumerable.Repeat("Tiếng Việt có dấu: Nguyễn Thị Thu Huơng. ", 120)), // decomposed
        string.Concat(Enumerable.Repeat("à́̂̃̄̅̆̇ ë̉̊̋ ", 300)), // stacked combining
    ];

    GotX.Widgets.TextInput _input = null!;

    protected override void OnCreated()
    {
        _input = App.New(new GotX.Widgets.TextInput { Multiline = true, Text = string.Concat(Enumerable.Repeat("long multiline editable line with ñ and Việt\n", 400)), BoxClass = "h-48 w-full px-3 border rounded-md" });
    }

    public override Div Render(Window w)
    {
        var col = w.Div().Class("flex flex-col gap-4 p-4");
        for (int i = 0; i < Blocks.Length; i++)
        {
            col.Child(w.Div().Class("text-sm leading-6 text-foreground").Child(Blocks[i]));
            if (i % 2 == 0) col.Child(w.Div().Class("w-40 text-xs truncate border").Child(Blocks[i]));
        }
        col.Child(w.InputArea(_input));
        return w.Div().Class("size-full overflow-y-auto bg-background text-foreground").Key("text-scroll").Child(col);
    }
}

static class TextScenario
{
    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        var rig = new Rig(text);
        var win = rig.Add(a => a.New(new TextPage()), new Size(900, 700));
        var m = Scenarios.Begin(rig, win, 2);
        var rng = new Random(7);
        for (int f = 0; f < 160; f++) // new width every frame: mostly re-wrap/re-shape cache misses
            m.Add(rig.Frame(win, () => win.Size = new Size(280 + (f * 53) % 1200, 700)));
        var p = rig.ScrollTarget(win);
        for (int f = 0; f < 80; f++) m.Add(rig.Send(win, new InputEvent(InputKind.Scroll, p, Delta: new Point(0, 160))));
        // Focus the multiline input (scroll it into view first), then type mixed-script text into 400 lines of content.
        for (int f = 0; f < 120; f++) m.Add(rig.Send(win, new InputEvent(InputKind.Scroll, p, Delta: new Point(0, 400))));
        for (int i = 0; i < win.Window.HitboxCount; i++)
            if (win.Window.HitboxIsInput(i) && win.Window.HitboxBounds(i) is { Width: > 8, Height: > 8 })
            {
                var c = rig.Center(win, i);
                m.Add(rig.Send(win, new InputEvent(InputKind.MouseDown, c))); m.Add(rig.Send(win, new InputEvent(InputKind.MouseUp, c)));
                break;
            }
        string[] chars = ["é", "😀", "ب", "ố"];
        for (int f = 0; f < 60; f++) m.Add(rig.Send(win, new InputEvent(InputKind.TextInput, Text: chars[rng.Next(chars.Length)])));
        return m.Finish();
    }
}
