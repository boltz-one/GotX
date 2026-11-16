using GotX.Widgets;

namespace GotX.Stress;

/// <summary>Interactive page used by input / resize / soak / multi-window: header, text input, button row, card grid, long list, table.</summary>
public sealed class MixedPage(int cards = 24, int rows = 300) : View
{
    public Leaf[] Leaves = [];
    TextInput _input = null!;
    int _clicks;
    public int Clicks => _clicks;

    protected override void OnCreated()
    {
        Leaves = Enumerable.Range(0, cards).Select(i => App.New(new Leaf(i))).ToArray();
        _input = App.New(new TextInput { Multiline = false, BoxClass = "h-9 w-64 px-3 rounded-md border focus:ring-2" });
    }

    public override Div Render(Window w)
    {
        var buttons = w.Div().Class("flex gap-2 items-center");
        for (int i = 0; i < 6; i++)
            buttons.Child(w.Div().Class("px-3 py-1.5 rounded-md bg-primary text-primary-foreground text-sm cursor-pointer hover:opacity-90")
                .OnClick(this, static (v, _) => { v._clicks++; v.Leaves[v._clicks % v.Leaves.Length].Bump(); v.Notify(); }).Child("Button " + i));
        var grid = w.Div().Class("grid grid-cols-3 gap-3");
        foreach (var l in Leaves) grid.Child(w.View(l));
        var list = w.Div().Class("flex flex-col h-72 overflow-y-auto border rounded-lg").Key("mixed-list");
        for (int i = 0; i < rows; i++)
            list.Child(w.Div().Class("px-3 py-2 text-sm border-b hover:bg-accent cursor-pointer truncate").Child("List row " + i + ": lorem ipsum dolor sit amet"));
        var table = w.Div().Class("flex flex-col border rounded-lg overflow-hidden");
        for (int r = 0; r < 60; r++)
        {
            var row = w.Div().Class(r % 2 == 0 ? "flex border-t text-sm hover:bg-muted/50" : "flex border-t text-sm bg-muted/20 hover:bg-muted/50");
            for (int c = 0; c < 6; c++) row.Child(w.Div().Class("flex-1 px-3 py-2 truncate").Child($"R{r}C{c} value"));
            table.Child(row);
        }
        return w.Div().Class("size-full bg-background text-foreground overflow-y-auto").Key("mixed-scroll").Child(
            w.Div().Class("flex flex-col gap-6 p-6 max-w-5xl mx-auto")
                .Child(w.Div().Class("text-2xl font-bold tracking-tight").Child("Mixed page"))
                .Child(w.Div().Class("flex gap-3 items-center").Child(w.InputArea(_input)).Child(buttons))
                .Child(grid).Child(list).Child(table));
    }
}
