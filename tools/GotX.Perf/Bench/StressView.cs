using GotX.Widgets;

namespace GotX.Perf;

/// <summary>
/// Synthetic stress page: thousands of nodes, long wrapped text, a big table, a scrolling list, an input — split into child
/// views the way an app would, so the view cache and the layout/paint costs of a large page both show up.
/// </summary>
public sealed class StressView : View
{
    View _text = null!, _cards = null!, _table = null!, _list = null!;
    TextInput _input = null!;

    protected override void OnCreated()
    {
        _text = App.New(new StressText());
        _cards = App.New(new StressCards());
        _table = App.New(new StressTable());
        _list = App.New(new StressList());
        _input = App.New(new TextInput());
    }

    public override Div Render(Window w) =>
        w.Div().Class("size-full bg-background text-foreground overflow-y-auto").Key("stress-scroll").Child(
            w.Div().Class("flex flex-col gap-8 p-8 max-w-6xl mx-auto")
                .Child(w.Div().Class("text-3xl font-bold tracking-tight").Child("Stress page"))
                .Child(w.Div().Class("flex gap-3 items-center")
                    .Child(w.Div().Class("w-72 h-9 rounded-md border px-3 flex items-center").Child(w.InputArea(_input)))
                    .Child(w.Div().Class("px-3 py-1.5 rounded-md bg-primary text-primary-foreground text-sm cursor-pointer hover:opacity-90").Child("Submit")))
                .Child(w.View(_text))
                .Child(w.View(_cards))
                .Child(w.View(_list))
                .Child(w.View(_table)));
}

sealed class StressText : View
{
    static readonly string[] Words =
    [
        "layout", "engine", "glyph", "atlas", "kerning", "Tiếng", "Việt", "chữ", "đậm", "nghiêng", "wrap", "scene", "primitive", "renderer",
        "hitbox", "replay", "cache", "component", "button", "dialog", "tailwind", "utility", "variant", "dark", "breakpoint", "flexbox",
    ];
    static readonly string[] Paragraphs = Enumerable.Range(0, 14).Select(i =>
        string.Join(' ', Enumerable.Range(0, 70).Select(j => Words[(i * 31 + j * 7) % Words.Length]))).ToArray();

    public override Div Render(Window w)
    {
        var col = w.Div().Class("flex flex-col gap-3");
        foreach (var p in Paragraphs) col.Child(w.Div().Class("text-sm leading-6 text-muted-foreground").Child(p));
        return col;
    }
}

sealed class StressCards : View
{
    static readonly string[] Titles = Enumerable.Range(0, 48).Select(i => $"Card {i + 1}").ToArray();
    static readonly string[] Bodies = Enumerable.Range(0, 48).Select(i => $"Body text of card {i + 1} that is long enough to wrap onto a second line inside the card.").ToArray();
    static readonly string[] Tags = ["alpha", "beta", "gamma", "delta"];

    public override Div Render(Window w)
    {
        var grid = w.Div().Class("grid grid-cols-3 gap-4");
        for (int i = 0; i < Titles.Length; i++)
        {
            var tags = w.Div().Class("flex gap-1");
            foreach (var t in Tags) tags.Child(w.Div().Class("px-2 py-0.5 rounded-full text-xs border bg-secondary text-secondary-foreground").Child(t));
            grid.Child(w.Div().Class("flex flex-col gap-2 p-4 rounded-xl border bg-card shadow-sm hover:shadow-md transition-shadow")
                .Child(w.Div().Class("flex items-center justify-between")
                    .Child(w.Div().Class("font-semibold").Child(Titles[i]))
                    .Child(w.Div().Class("size-6 rounded-full bg-primary")))
                .Child(w.Div().Class("text-sm text-muted-foreground").Child(Bodies[i]))
                .Child(tags));
        }
        return grid;
    }
}

sealed class StressList : View
{
    static readonly string[] Rows = Enumerable.Range(0, 400).Select(i => $"List row {i + 1}: lorem ipsum dolor sit amet").ToArray();

    public override Div Render(Window w)
    {
        var list = w.Div().Class("flex flex-col h-96 overflow-y-auto border rounded-lg").Key("stress-list");
        foreach (var r in Rows)
            list.Child(w.Div().Class("px-3 py-2 text-sm border-b hover:bg-accent cursor-pointer truncate").Child(r));
        return list;
    }
}

sealed class StressTable : View
{
    const int RowCount = 150, ColCount = 8;
    static readonly string[] Headers = ["ID", "Name", "Status", "Owner", "Region", "Created", "Updated", "Notes"];
    static readonly string[][] Cells = Enumerable.Range(0, RowCount).Select(r =>
        Enumerable.Range(0, ColCount).Select(c => c == 0 ? (1000 + r).ToString() : $"R{r}C{c} value").ToArray()).ToArray();

    public override Div Render(Window w)
    {
        var table = w.Div().Class("flex flex-col border rounded-lg overflow-hidden");
        var head = w.Div().Class("flex bg-muted text-xs font-medium uppercase");
        foreach (var h in Headers) head.Child(w.Div().Class("flex-1 px-3 py-2").Child(h));
        table.Child(head);
        for (int r = 0; r < RowCount; r++)
        {
            var row = w.Div().Class(r % 2 == 0 ? "flex border-t text-sm hover:bg-muted/50" : "flex border-t text-sm bg-muted/20 hover:bg-muted/50");
            foreach (var c in Cells[r]) row.Child(w.Div().Class("flex-1 px-3 py-2 truncate").Child(c));
            table.Child(row);
        }
        return table;
    }
}
