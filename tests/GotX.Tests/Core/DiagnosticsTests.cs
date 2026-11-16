using GotX;
using GotX.Test;

/// <summary>Các móc chẩn đoán của Core mà GotX.Test dựa vào: Window.Inspect, Window.Trace, Window.LastFrame.</summary>
static class DiagnosticsTests
{
    /// <summary>Một trang có đủ loại node và trạng thái để soi.</summary>
    sealed class Sample : View
    {
        public override Div Render(Window w)
        {
            var scroller = w.Div().Key("scroller").Class("w-40 h-20 overflow-y-auto");
            for (int i = 0; i < 6; i++) scroller.Child(w.Div().Key("line-" + i).Class("h-8 text-sm").Child("Dòng " + i));
            return Fx.Page(w)
                .Child(w.Div().Key("styled").Focusable().Class("w-32 h-10 px-3 py-1 rounded-md border-2 border-sky-600 bg-sky-100 text-sky-900 text-lg font-semibold opacity-80 truncate")
                    .OnClick(this, static (_, _) => { }).Child("Kiểu"))
                .Child(w.Div().Key("off").Disabled().Focusable().Class("w-32 h-8 bg-slate-200").OnClick(this, static (_, _) => { }))
                .Child(w.Div().Key("holder").Class("relative w-32 h-8 bg-slate-100").Child(w.Div().Key("pinned").Class("absolute right-0 top-0 size-4 bg-rose-500")))
                .Child(w.Div().Key("turned").Class("size-8 bg-emerald-500 rotate-45"))
                .Child(w.Div().Key("gone").Class("hidden w-10 h-10"))
                .Child(scroller);
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/inspect/resolved-style-and-flags", t =>
        {
            t.Open(new Sample());
            var n = t.Get("styled");
            t.Assert.Equal((n.Kind, n.Key, n.Depth, t.Tree.Nodes[n.Parent].Kind), ("div", "styled", 2, "div"), "loại, khoá, độ sâu, cha");
            t.Assert.True(n.Classes!.StartsWith("w-32 h-10 px-3") && n.Classes.EndsWith("truncate"), "chuỗi class gốc");
            t.Assert.Equal(n.Screen, new Bounds(24, 24, 128, 40), "hộp trên màn hình");
            t.Assert.Equal((n.Padding, n.Border, n.Radius), (new Edges(4, 12, 4, 12), new Edges(2, 2, 2, 2), Corners.All(8)), "padding / viền / bo góc đã tính");
            t.Assert.True(Pixels.Delta(n.Background, Rgba.FromHex(0xe0f2fe)) < 12 && n.Color.B > n.Color.R, $"nền và màu chữ đã tính ({Fmt.C(n.Background)}, {Fmt.C(n.Color)})");
            t.Assert.Equal((n.FontSize, n.LineHeight, n.Weight, n.Opacity), (18f, 28f, FontWeight.Semibold, 0.8f), "cỡ chữ, line-height, độ đậm, opacity");
            t.Assert.True(n is { Clickable: true, Focusable: true, Focused: false, Disabled: false, Ellipsis: true, NoWrap: true, Display: Display.Block }, "cờ tương tác và kiểu chữ");
            var text = t.Tree.Nodes[n.Children[0]];
            t.Assert.True(text is { Kind: "text", Text: "Kiểu", FontSize: 18, Ellipsis: true } && text.TextStyle.Weight == FontWeight.Semibold && text.Opacity == 0.8f, "node chữ thừa kế kiểu chữ + opacity tích luỹ");
            t.Assert.True(t.Get("off") is { Disabled: true, Clickable: false, Focusable: false }, "phần tử disabled: không click, không focus");
            t.Assert.True(t.Get("pinned") is { Position: Position.Absolute } p && p.Screen == new Bounds(136, 24 + 40 + 16 + 32 + 16, 16, 16), "absolute: vị trí theo cha relative");
            t.Assert.True(t.Get("turned").Transformed && !n.Transformed, "cờ Transformed cho phần tử bị xoay");
            t.Assert.True(MathF.Abs(t.Get("turned").Screen.Width - 32 * MathF.Sqrt(2)) < 0.1f, "Screen của phần tử xoay là hộp bao");
            t.Assert.NotExists("gone"); // display:none không có trong ảnh chụp
            t.Press(Key.Tab);
            t.Assert.True(t.Get("styled").Focused, "cờ Focused sau khi Tab");
            t.Hover("styled");
            t.Assert.True(t.Get("styled").Hovered && !t.Get("off").Hovered, "cờ Hovered theo vị trí chuột");
        });

        suite.Add("core/inspect/scroll-clip-and-primitive-ranges", t =>
        {
            t.Open(new Sample());
            var scroller = t.Get("scroller");
            t.Assert.True(scroller is { Scrollable: true, Overflow: Overflow.Hidden } && scroller.ContentSize.Height == 6 * 32, $"vùng cuộn: cờ Scrollable, kích thước nội dung {scroller.ContentSize.Height}");
            var line5 = t.Get("line-5");
            t.Assert.True(line5.Visible.IsEmpty && line5.Clip == scroller.Screen && line5.Screen.Y == scroller.Screen.Y + 160, "dòng ngoài khung: Clip là hộp vùng cuộn, Visible rỗng");
            t.Window.ScrollTo("scroller", null, 48, animate: false);
            t.Frames();
            var line1 = t.Get("line-1");
            t.Assert.True(line1.Screen.Y == scroller.Screen.Y - 16 && line1.Layout.Y == scroller.Layout.Y + 32, "sau khi cuộn: Screen đã trừ cuộn, Layout thì không");
            t.Assert.Equal(line1.Visible, new Bounds(scroller.Screen.X, scroller.Screen.Y, 160, 16), "Visible = Screen ∩ Clip (nửa dưới của dòng)");

            // Đoạn primitive: của cha bao trùm của con; của anh em không chồng nhau; tổng khớp với Scene.
            var tree = t.Tree;
            var scene = tree.Snapshot.Scene;
            t.Assert.True(tree.Nodes[0].PrimStart == 0 && tree.Nodes[0].PrimEnd == scene.Count, "node gốc sở hữu toàn bộ Scene");
            int broken = 0;
            foreach (var node in tree.Nodes)
            {
                int last = node.PrimStart;
                foreach (int c in node.Children)
                {
                    var child = tree.Nodes[c];
                    if (child.PrimStart < last || child.PrimEnd > node.PrimEnd || child.PrimEnd < child.PrimStart) broken++;
                    last = child.PrimEnd;
                }
            }
            t.Assert.Equal(broken, 0, "đoạn primitive lồng nhau đúng theo cây");
            var styled = t.Get("styled");
            var quad = scene.Primitives[styled.PrimStart];
            t.Assert.True(quad.Kind == PrimitiveKind.Quad && quad.Bounds == styled.Screen && quad.BorderWidths.Top == 2, "primitive đầu của phần tử là nền + viền của nó");
            t.Assert.True(scene.Primitives[styled.PrimEnd - 1].Kind == PrimitiveKind.Glyph && tree.OwnerOfPrimitive(styled.PrimEnd - 1)!.Kind == "text", "các glyph thuộc node chữ con");
        });

        suite.Add("core/inspect/works-with-cache-and-has-no-lasting-effect", t =>
        {
            var host = t.Open(new CounterHost());
            t.Frames(3);
            int panel = host.Panel.Renders;
            t.Assert.True(t.Window.FindBounds("c-value") is null, "sau frame phát lại, FindBounds không thấy phần tử trong view đã cache…");
            t.Assert.True(t.Find("c-value") is not null, "…còn Inspect thì thấy (frame soi tắt cache)");
            t.Assert.Equal(host.Panel.Renders, panel + 1, "frame soi render lại mọi view đúng một lần");
            t.Assert.True(t.Window.CacheEnabled, "Inspect trả lại CacheEnabled như cũ");
            t.Draw();
            t.Assert.True(t.Window.LastFrame is { RenderedViews: 0, ReplayedViews: 1 }, "frame kế tiếp lại phát lại hoàn toàn từ cache");
            t.Assert.Equal(host.Panel.Renders, panel + 1, "không render thêm");
        });

        suite.Add("core/trace/event-kinds", t =>
        {
            var host = t.Open(new CounterHost());
            int mark = t.Trace.Mark();
            t.Click("c-inc");
            t.Press(Key.Tab);
            t.Scroll(new Point(100, 100), 40);
            var kinds = new HashSet<TraceKind>();
            foreach (var e in t.Trace.Since(mark)) kinds.Add(e.Kind);
            foreach (var kind in new[] { TraceKind.Input, TraceKind.Click, TraceKind.Key, TraceKind.Focus, TraceKind.ViewDirty, TraceKind.ViewRender, TraceKind.Frame })
                t.Assert.True(kinds.Contains(kind), $"có sự kiện {kind}");
            var click = t.Trace.Since(mark).First(e => e.Kind == TraceKind.Click);
            t.Assert.True(click.Message == "CounterView" && click.Bounds == t.Box("c-inc") && click.ViewId == host.Counter.Id.Value, $"Click mang kiểu state, hộp phần tử và id view: {click}");
            t.Assert.True(t.Trace.Since(mark).Any(e => e.Kind == TraceKind.Key && e.Message == "KeyDown Tab"), "Key mang tên phím");
            t.Assert.Equal(t.Trace.Names(TraceKind.ViewRender, mark).Count > 0 ? t.Trace.Names(TraceKind.ViewRender, mark)[0] : "", "CounterView ×2", "Names gộp tên trùng (2 frame render do click rồi focus; view cha không render lại)");
            t.Assert.True(t.Trace.Tail(50).TrueForAll(line => !line.Contains("Frame: render 0,")), "Tail lược bỏ các frame không render gì");
            int frames = t.Trace.Count(TraceKind.Frame, mark);
            var _ = t.Tree;
            t.Assert.Equal(t.Trace.Count(TraceKind.Frame, mark), frames, "frame soi của harness không lẫn vào nhật ký");
        });

        suite.Add("core/frame-stats", t =>
        {
            var host = t.Open(new CounterHost());
            t.Draw();
            var idle = t.Window.LastFrame;
            t.Assert.True(idle is { RenderedViews: 0, ReplayedViews: 1, AllocatedBytes: 0, Animating: false } && idle.Primitives > 20 && idle.Hitboxes >= 2, $"frame rảnh: {idle}");
            host.Counter.Notify();
            t.App.Flush();
            t.Draw();
            var busy = t.Window.LastFrame;
            t.Assert.True(busy is { RenderedViews: 1, ReplayedViews: 0 } && busy.Frame == idle.Frame + 1 && busy.Nodes > 5 && t.Window.LastFramePartial, $"sau Notify bộ đếm (khung một phần): chỉ bộ đếm render, khối tĩnh và view cha không bị chạm: {busy}");
            t.Assert.True(busy.TotalMs > 0 && busy.TotalMs >= busy.LayoutMs && busy.PaintMs >= 0 && busy.AllocatedBytes > 0, "thời gian và cấp phát của frame có render");
            t.Assert.Equal(busy.Primitives, idle.Primitives, "phát lại cho ra đúng số primitive như render");
            t.Assert.True(t.Window.LastFrameRenderedViews == 1 && t.Window.HitboxCount == busy.Hitboxes, "khớp với các thuộc tính cũ của Window");
        });
    }
}
