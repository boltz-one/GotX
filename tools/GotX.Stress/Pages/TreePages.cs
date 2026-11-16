using GotX.Components;

namespace GotX.Stress;

/// <summary>A card-like leaf view (~12 nodes) whose content changes on <see cref="Bump"/>: the unit of state churn.</summary>
public sealed class Leaf(int id) : View
{
    public int N;
    public void Bump() { N++; Notify(); }
    public override Div Render(Window w) =>
        w.Div().Class("flex flex-col gap-1 p-3 rounded-lg border bg-card text-sm")
            .Child(w.Div().Class("flex items-center justify-between")
                .Child(w.Div().Class("font-medium").Child("Leaf " + id))
                .Child(w.Div().Class("size-3 rounded-full bg-primary")))
            .Child(w.Div().Class("text-muted-foreground").Child("updates: " + N))
            .Child(w.Div().Class("flex gap-1")
                .Child(w.Div().Class("px-2 rounded-full border text-xs").Child("a"))
                .Child(w.Div().Class("px-2 rounded-full border text-xs").Child("b")));
}

/// <summary>~100 nodes: 8 rows x 5 cells. A tree of these gives a huge-but-realistic node count with a view boundary every ~100 nodes.</summary>
public sealed class Block(int id) : View
{
    public override Div Render(Window w)
    {
        var col = w.Div().Class("flex flex-col border-b");
        for (int r = 0; r < 8; r++)
        {
            var row = w.Div().Class("flex gap-2 px-3 py-1 text-sm hover:bg-muted/50");
            for (int c = 0; c < 5; c++) row.Child(w.Div().Class("flex-1 truncate").Child($"b{id} r{r} c{c}"));
            col.Child(row);
        }
        return col;
    }
}

/// <summary>Scrolling column of <paramref name="blocks"/> blocks (nodes ~ 100 x blocks).</summary>
public sealed class HugeTree(int blocks) : View
{
    View[] _kids = [];
    protected override void OnCreated() { _kids = Enumerable.Range(0, blocks).Select(i => (View)App.New(new Block(i))).ToArray(); }
    public override Div Render(Window w)
    {
        var col = w.Div().Class("size-full overflow-y-auto bg-background text-foreground flex flex-col").Key("tree-scroll");
        foreach (var k in _kids) col.Child(w.View(k));
        return col;
    }
}

/// <summary>Grid of <paramref name="count"/> leaves, exposing them so a scenario can Notify random ones.</summary>
public sealed class ChurnPage(int count) : View
{
    public Leaf[] Leaves = [];
    protected override void OnCreated() { Leaves = Enumerable.Range(0, count).Select(i => App.New(new Leaf(i))).ToArray(); }
    public override Div Render(Window w)
    {
        var grid = w.Div().Class("size-full overflow-y-auto bg-background text-foreground grid grid-cols-4 gap-3 p-4").Key("churn-scroll");
        foreach (var l in Leaves) grid.Child(w.View(l));
        return grid;
    }
}

/// <summary>VirtualList with a million rows; labels are cached so frame cost is the library's, not string building.</summary>
public sealed class VListPage(int rows, string cls = "h-[640px]") : View
{
    readonly string?[] _keys = new string?[rows], _labels = new string?[rows];
    public VirtualList List = null!;
    protected override void OnCreated() => List = App.New(new VirtualList
    {
        Count = rows, ItemHeight = 32, Class = cls,
        ItemKey = i => _keys[i] ??= "r" + i,
        RenderItem = (w, i, active) => w.Div().Class(active ? "flex items-center px-3 bg-muted text-sm" : "flex items-center px-3 text-sm border-b")
            .Child(_labels[i] ??= "Row " + i + " - virtualised list item"),
    });
    public override Div Render(Window w) => w.Div().Class("size-full bg-background text-foreground").Child(w.View(List));
}
