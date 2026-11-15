extern alias HeadlessSample;
using GotX.Components;
using GotX.Text;
using ShadcnShots;

namespace GotX.Perf;

/// <summary>A page the bench can host: name, root-view factory and window size.</summary>
public sealed record Target(string Name, Func<App, View> Root, Size Size);

/// <summary>Pages to benchmark: Headless demo, every Components gallery page, shadcn home + blocks, a synthetic stress page.</summary>
public static class Targets
{
    public static readonly Size Desktop = new(1100, 760), Shadcn = new(1440, 900);

    public static List<Target> All()
    {
        ComponentTheme.Install();
        var list = new List<Target>
        {
            new("demo", static app => app.New(new HeadlessSample::DemoView()), new Size(1100, 720)),
            new("gallery-display", static app => Page(app, new DisplayPage()), Desktop),
            new("gallery-forms", static app => Page(app, new FormsPage()), Desktop),
            new("gallery-overlays", static app => Page(app, new OverlaysPage()), Desktop),
            new("gallery-navigation", static app => Page(app, new NavigationPage()), Desktop),
            new("gallery-chat", static app => Page(app, new ChatPage()), Desktop),
            new("gallery-data", static app => Page(app, new DataPage()), Desktop),
            new("shadcn-home", static app => app.New(new SiteShell()), Shadcn),
        };
        foreach (var block in BlockCatalog.All)
        {
            var page = block;
            list.Add(new Target("shadcn-" + page.Id, app => app.New(new ShotHost(page)), Shadcn));
        }
        list.Add(new Target("stress", static app => app.New(new StressView()), Desktop));
        return list;
    }

    // A gallery page as the gallery shows it: a scrolling column filling the window.
    static View Page(App app, View page) => app.New(new PageHost(app.New(page)));

    /// <summary>Fonts for benchmark runs: Geist only (deterministic, independent of the machine's system fonts).</summary>
    public static TextSystem Fonts() => GeistFonts.Create();
}

sealed class PageHost(View page) : View
{
    public override Div Render(Window w) =>
        w.Div().Class("size-full flex bg-background text-foreground")
            .Child(w.Div().Class("flex-1 overflow-y-auto").Key("page").Child(w.View(page)));
}
