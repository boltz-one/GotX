using GotX.Components;

namespace GotX.Footprint;

/// <summary>Root views of the scenarios. Real components, real layout; row data is generated lazily and cached so the frame cost is the library's, not string building.</summary>
static class Scenes
{
    public static readonly string[] Names = ["hello", "home", "input", "vlist", "spin", "dialog", "resize"];

    public static View Create(string name, App app) => name switch
    {
        "hello" => app.New(new HelloView()),
        "home" or "resize" => app.New(new SiteShell()),
        "input" => app.New(new InputView()),
        "vlist" => app.New(new VListView()),
        "spin" => app.New(new SpinView()),
        "dialog" => app.New(new DialogView()),
        _ => throw new ArgumentException("unknown scenario " + name),
    };

    public static (int W, int H) Size(string name) => name is "home" or "resize" ? (1440, 900) : (1000, 700);
}

sealed class HelloView : View
{
    public override Div Render(Window w) => w.Div().Class("size-full flex items-center justify-center bg-white text-slate-900 text-2xl font-semibold").Child("Hello, GotX");
}

sealed class InputView : View
{
    Input _input = null!;
    protected override void OnCreated() => _input = App.New(new Input { Placeholder = "Type here" });
    public override Div Render(Window w) => w.Div().Class("size-full flex items-center justify-center bg-background text-foreground")
        .Child(w.Div().Class("w-64").Child(w.View(_input)));
}

sealed class SpinView : View
{
    public override Div Render(Window w) => w.Div().Class("size-full flex items-center justify-center bg-background text-foreground").Child(Ui.Spinner(w, 64));
}

sealed class VListView : View
{
    public const int Rows = 100_000;
    readonly string[] _keys = new string[Rows], _labels = new string[Rows];
    VirtualList _list = null!;

    protected override void OnCreated() => _list = App.New(new VirtualList
    {
        Count = Rows, ItemHeight = 32, Class = "h-[640px]",
        ItemKey = i => _keys[i] ??= "r" + i,
        RenderItem = (w, i, active) => w.Div().Class(active ? "flex items-center px-3 bg-muted text-sm" : "flex items-center px-3 text-sm border-b")
            .Child(_labels[i] ??= "Row " + i + " - virtualised list item"),
    });

    public override Div Render(Window w) => w.Div().Class("size-full bg-background text-foreground").Child(w.View(_list));
}

sealed class DialogView : View
{
    public static int Opened;
    Dialog _dialog = null!;
    Window? _w;

    protected override void OnCreated()
    {
        _dialog = App.New(new Dialog { Title = "Edit profile", Description = "Change the profile, then press Save.", Body = static w => w.Div().Class("py-2 text-sm").Child("Dialog body") });
        App.Subscribe<Dialog.OpenChanged>(_dialog, static e => { if (e.Open) Opened++; });
    }

    public override Div Render(Window w)
    {
        _w = w;
        return w.Div().Class("size-full flex items-center justify-center bg-background text-foreground")
            .Child(Ui.Button(w, "Open dialog", ButtonVariant.Outline).OnClick(this, static (v, _) => v._dialog.Open(v._w!)));
    }
}
