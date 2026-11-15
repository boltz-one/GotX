using System.Text.Json;
using GotX;
using GotX.Test;
using GotX.Widgets;

/// <summary>Harness: mở cửa sổ, đồng hồ ảo, input mô phỏng, truy vấn cây, đường dẫn phần tử, bản in cây.</summary>
static class HarnessTests
{
    /// <summary>Một form nhỏ có đủ loại phần tử để thử input.</summary>
    sealed class FormView : View
    {
        public TextInput Name = null!;
        public int Clicks, RightClicks;
        public string Hover = "", Drag = "";
        public ScrollInfo LastScroll;

        protected override void OnCreated() => Name = App.New(new TextInput { Multiline = false, Placeholder = "Tên" });

        public override Div Render(Window w)
        {
            var list = w.Div().Key("list").Class("w-48 h-24 overflow-y-auto border border-slate-300").OnScroll(this, static (v, info) => v.LastScroll = info);
            for (int i = 0; i < 20; i++) list.Child(w.Div().Key("row-" + i).Class("h-8 px-2 flex items-center text-sm").Child("Dòng " + i));
            return Fx.Page(w)
                .Child(w.Div().Key("name").Class("w-48").Child(w.View(Name)))
                .Child(w.Div().Key("btn").Focusable().Class("h-8 px-3 flex items-center rounded bg-slate-900 text-white text-sm")
                    .OnClick(this, static (v, _) => { v.Clicks++; v.Notify(); })
                    .OnAuxClick(this, static (v, _) => v.RightClicks++)
                    .OnHover(this, static (v, inside) => v.Hover += inside ? "in," : "out,").Child($"Bấm ({Clicks})"))
                .Child(w.Div().Key("track").Class("w-[200px] h-4 bg-slate-200").OnDrag(this, static (v, e) => v.Drag += $"{e.Phase}:{e.FractionX:0.00},"))
                .Child(list);
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("harness/open-and-clock", t =>
        {
            var host = t.Open(new CounterHost(), new Size(500, 300), scale: 2, dark: true);
            t.Assert.True(t.Window.DarkMode && t.Scale == 2 && t.Window.CacheEnabled, "Open áp kích thước, scale, dark; cache bật mặc định");
            t.Assert.Equal((t.PixelWidth, t.PixelHeight), (1000, 600), "kích thước ảnh = logical × scale");
            double before = t.Now;
            int fired = 0;
            t.App.SetTimeout(0.1, () => fired++);
            t.Advance(0.05);
            t.Assert.Equal(fired, 0, "chưa tới hạn sau 50 ms");
            t.Advance(0.1);
            t.Assert.Equal(fired, 1, "Advance đẩy cả đồng hồ của App và chạy hẹn giờ");
            t.Assert.Near((float)(t.Now - before), 0.144f, 0.001f, "Advance đi theo bước 16 ms (3 + 6 frame)");
            t.Assert.True(t.Settle() && host.Renders >= 1, "giao diện tĩnh thì Settle trả true");
        });

        suite.Add("harness/input-simulation", t =>
        {
            var v = t.Open(new FormView());
            t.Click("btn");
            t.Click("btn");
            t.Assert.Equal(v.Clicks, 2, "Click theo khoá gọi OnClick");
            t.Assert.Text("btn", "Bấm (2)");
            t.RightClick("btn");
            t.Assert.Equal(v.RightClicks, 1, "RightClick gọi OnAuxClick, không gọi OnClick");
            t.Hover(new Point(480, 380));
            v.Hover = "";
            t.Hover("btn");
            t.Hover(new Point(480, 380));
            t.Assert.Equal(v.Hover, "in,out,", "Hover vào rồi ra");
            var track = t.Box("track");
            t.Drag(new Point(track.X + 20, track.Y + 8), new Point(track.X + 150, track.Y + 8));
            t.Assert.True(v.Drag.StartsWith("Start:0.10,") && v.Drag.EndsWith("End:0.75,"), $"Drag: Start → Move… → End ({v.Drag})");
            t.Click("name");
            t.Type("GotX");
            t.Press(Key.Backspace);
            t.Assert.Equal(v.Name.Text, "Got", "Click focus ô nhập, Type chèn chữ, Press xoá");
            t.ClickText("Dòng 1");
            t.Assert.True(!v.Name.IsFocused, "ClickText bấm vào chữ → ô nhập mất focus");
        });

        suite.Add("harness/scroll-and-screen-bounds", t =>
        {
            var v = t.Open(new FormView());
            float before = t.Box("row-3").Y;
            t.Scroll("list", 64);
            t.Assert.Near(t.Window.GetScroll("list")!.Value.Offset.Y, 64, 0.5f, "Scroll lăn chuột và chờ cuộn mượt xong");
            t.Assert.Near(v.LastScroll.Offset.Y, 64, 0.5f, "OnScroll nhận vị trí mới");
            t.Assert.Near(t.Box("row-3").Y, before - 64, 0.5f, "Box trả hộp trên màn hình (đã trừ cuộn), không phải hộp layout");
            t.Assert.Hidden("row-0");
            t.Assert.Visible("row-3");
            // Bấm theo khoá nhắm vào phần còn nhìn thấy, kể cả khi phần tử bị cuộn che một nửa.
            var inner = Kit.Probe(p => { p.Open(new FormView()); p.Click("row-15"); });
            Kit.ExpectOneFailure(t, inner, "bấm phần tử bị cuộn che hết", "#row-15", "không nhìn thấy");
        });

        suite.Add("harness/queries-and-path", t =>
        {
            t.Open(new CounterHost());
            var tree = t.Tree;
            var value = t.Get("c-value");
            t.Assert.Equal(tree.Path(value), "CounterHost >> CounterView > div[1] > div#c-value .text-sm", "đường dẫn: view + khoá + hai mức cuối, kèm class");
            t.Assert.Equal(tree.Label(tree.FindText("Giá trị")!), "text \"Giá trị: 0\"", "nhãn node chữ");
            t.Assert.True(tree.FindText("Tăng", exact: true) is { Kind: "text" } && tree.FindText("tăng", exact: true) is null, "FindText chính xác / không thấy");
            t.Assert.Equal(tree.TextOf(tree.FindView("CounterView")!), "Tăng Giá trị: 0", "TextOf gộp chữ của cây con");
            t.Assert.Equal(tree.OwnerView(value)?.View, "CounterView", "OwnerView");
            var chain = new List<string>();
            foreach (var a in tree.Ancestors(value)) chain.Add(a.Kind);
            t.Assert.Equal(string.Join(">", chain), "div>view>div>view", "Ancestors từ cha lên gốc");
            t.Assert.Equal(tree.FindAll(n => n.Kind == "view").Count(), 3, "FindAll theo điều kiện: 3 view");
            t.Assert.True(tree.TopAt(Fmt.Center(value.Screen))?.Kind == "text", "TopAt trả node trên cùng tại một điểm");
            var inner = Kit.Probe(p => { p.Open(new CounterHost()); p.Click("khong-co"); p.Assert.True(false, "không được chạy tới đây"); });
            Kit.ExpectOneFailure(t, inner, "Get khoá không tồn tại dừng test", "không tìm thấy phần tử #khong-co");
        });

        suite.Add("harness/tree-dump", t =>
        {
            t.Open(new CleanView(), new Size(640, 480));
            t.Press(Key.Tab);
            string dump = t.Dump();
            t.Assert.True(dump.Contains("div#save .h-8 px-3 flex items-center rounded-md text-sm cu… [24,24 47×32] click focusable focused"), $"dòng của nút có khoá, class rút gọn, hộp:\n{Fmt.Cut(dump, 900)}");
            t.Assert.True(dump.Contains("click focusable focused"), "cờ click/focusable/focused");
            t.Assert.True(dump.Contains("div#list") && dump.Contains("] scroll"), "cờ scroll");
            t.Assert.True(dump.Contains("text [") && dump.Contains("\"Lưu\""), "node chữ kèm trích đoạn");
            t.Assert.True(dump.Contains(" hidden "), "dòng bị cuộn che hết có cờ hidden");
            string sub = t.Dump(new DumpOptions { RootKey = "toolbar", MaxDepth = 1 });
            t.Assert.Equal(sub.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length, 4, "RootKey + MaxDepth: thanh công cụ và 3 con trực tiếp");
            string visible = t.Dump(new DumpOptions { RootKey = "list", OnlyVisible = true });
            t.Assert.True(!visible.Contains(" hidden") && visible.Contains("Mục 1") && !visible.Contains("Mục 12"), "OnlyVisible bỏ các dòng bị che");
            string marked = TreeDump.Dump(t.Tree, new DumpOptions { Mark = [t.Get("cancel").Index] });
            t.Assert.True(marked.Contains(">>") && marked.Split(">>")[1].TrimStart().StartsWith("div#cancel"), "Mark đánh dấu >> ở node gây lỗi");
            using var json = JsonDocument.Parse(TreeDump.ToJson(t.Tree));
            var nodes = json.RootElement.GetProperty("nodes");
            t.Assert.Equal(nodes.GetArrayLength(), t.Tree.Nodes.Count, "JSON có đủ node");
            t.Assert.Equal(nodes[0].GetProperty("view").GetString(), "CleanView", "JSON: node gốc là view");
        });
    }
}
