using GotX;
using GotX.Test;

/// <summary>Assertion: mỗi cái phải đạt khi đúng, và khi sai phải ghi một lỗi có đủ số đo – mà không dừng test.</summary>
static class AssertTests
{
    /// <summary>Hai hàng hộp có khoá; hàng dưới lệch có chủ ý để thử các assertion về vị trí.</summary>
    static Div Layout(Window w) => Fx.Page(w)
        .Child(w.Div().Key("row").Class("flex items-start gap-2")
            .Child(Fx.Box(w, "a", "w-16 h-8 bg-slate-300")).Child(Fx.Box(w, "b", "w-16 h-8 bg-slate-300")).Child(Fx.Box(w, "c", "w-16 h-8 mt-1 bg-slate-300")))
        .Child(w.Div().Key("frame").Class("relative w-40 h-10 bg-slate-100")
            .Child(Fx.Box(w, "inside", "absolute left-2 top-2 w-8 h-6 bg-sky-500"))
            .Child(Fx.Box(w, "poke", "absolute left-36 top-2 w-8 h-6 bg-rose-500")))
        .Child(w.Div().Class("flex items-center gap-3")
            .Child(w.Div().Key("small").Class("text-xs").Child("nhỏ")).Child(w.Div().Key("big").Class("text-xl").Child("Lớn")));

    public static void Register(TestSuite suite)
    {
        suite.Add("assert/passing-cases", t =>
        {
            t.Open(new Fx(Layout));
            bool ok = t.Assert.Exists("a") & t.Assert.NotExists("zzz") & t.Assert.Visible("a") & t.Assert.Hidden("zzz")
                & t.Assert.Size("a", 64, 32) & t.Assert.Size("a", float.NaN, 32) & t.Assert.Position("a", 24, 24)
                & t.Assert.Aligned(Edge.Top, 0, "a", "b") & t.Assert.Aligned(Edge.CenterY, 4, "a", "b", "c") & t.Assert.Aligned(Edge.Left, 0, "row", "frame")
                & t.Assert.Inside("inside", "frame") & t.Assert.NoOverlap("a", "b") & t.Assert.Text("small", "nhỏ") & t.Assert.TextContains("big", "ớn")
                & t.Assert.TextVisible("Lớn") & t.Assert.Pixel(Fmt.Center(t.Box("inside")), Rgba.FromHex(0x0ea5e9), 40)
                & t.Assert.Equal(1 + 1, 2, "phép cộng") & t.Assert.Near(1.04f, 1, 0.05f, "gần bằng") & t.Assert.True(true, "hiển nhiên");
            t.Assert.True(ok && t.Failures.Count == 0, "mọi assertion đúng đều trả true và không ghi lỗi");
        });

        suite.Add("assert/failures-are-recorded-not-thrown", t =>
        {
            var inner = Kit.Probe(p =>
            {
                p.Open(new Fx(Layout));
                p.Assert.Exists("zzz");
                p.Assert.Size("a", 64, 40);
                p.Assert.Text("small", "to");
                p.Assert.True(false, "điều kiện tự do");
            });
            t.Assert.Equal(inner.Failures.Count, 4, "bốn assertion sai → bốn lỗi, test vẫn chạy tới cuối");
            if (inner.Failures.Count != 4) return;
            t.Assert.Equal(inner.Failures[0].Message, "Exists: không có phần tử #zzz", "thông báo Exists");
            t.Assert.Equal(inner.Failures[1].Message, "Size: #a mong đợi 64×40 ±0.5, thực tế 64×32", "thông báo Size có số đo");
            t.Assert.Equal(inner.Failures[1].Path, "Fx >> div#row > div#a .w-16 h-8 bg-slate-300", "lỗi mang đường dẫn phần tử");
            t.Assert.Equal(inner.Failures[1].Bounds, new Bounds(24, 24, 64, 32), "lỗi mang hộp phần tử");
            t.Assert.Equal(inner.Failures[2].Message, "Text: #small mong đợi \"to\", thực tế \"nhỏ\"", "thông báo Text");
        });

        suite.Add("assert/aligned", t =>
        {
            var inner = Kit.Probe(p => { p.Open(new Fx(Layout)); p.Assert.Aligned(Edge.Top, 0.5f, "a", "b", "c"); });
            var f = Kit.ExpectOneFailure(t, inner, "Aligned(Top)", "lệch nhau 4px", "#a=24", "#c=28", "lệch nhất: #c");
            t.Assert.True(f?.Path?.Contains("div#c") == true, "phần tử lệch nhất là phần tử chính của lỗi");
            inner = Kit.Probe(p => { p.Open(new Fx(Layout)); p.Assert.Aligned(Edge.Right, 0, "a", "b"); });
            Kit.ExpectOneFailure(t, inner, "Aligned(Right)", "lệch nhau 72px");
            // Đường gốc: hai cỡ chữ căn giữa theo chiều dọc có baseline khác nhau; hai chữ cùng cỡ thì trùng.
            inner = Kit.Probe(p => { p.Open(new Fx(Layout)); p.Assert.Aligned(Edge.Baseline, 0.5f, "small", "big"); p.Assert.Aligned(Edge.Baseline, 0, "a", "b"); });
            t.Assert.Equal(inner.Failures.Count, 2, "baseline lệch giữa hai cỡ chữ + phần tử không có chữ");
            t.Assert.True(inner.Failures.Count == 2 && inner.Failures[0].Message.Contains("Aligned(Baseline)") && inner.Failures[1].Message.Contains("không có chữ"), "thông báo Baseline");
            t.Open(new Fx(w => Fx.Page(w).Child(w.Div().Class("flex items-center gap-3")
                .Child(w.Div().Key("x").Class("text-sm").Child("một")).Child(w.Div().Key("y").Class("px-2 text-sm font-semibold").Child("hai")))));
            t.Assert.Aligned(Edge.Baseline, 0, "x", "y");
        });

        suite.Add("assert/inside-overlap-visible", t =>
        {
            var inner = Kit.Probe(p => { p.Open(new Fx(Layout)); p.Assert.Inside("poke", "frame"); });
            Kit.ExpectOneFailure(t, inner, "Inside", "#poke", "thò ra ngoài #frame", "16px");
            inner = Kit.Probe(p => { p.Open(new Fx(Layout)); p.Assert.NoOverlap("frame", "inside"); });
            Kit.ExpectOneFailure(t, inner, "NoOverlap", "chồng nhau 32×24px");
            inner = Kit.Probe(p =>
            {
                p.Open(new Fx(w => Fx.Page(w).Child(w.Div().Class("w-20 h-8 overflow-hidden").Child(w.Div().Class("h-8")).Child(Fx.Box(w, "below")))));
                p.Assert.Visible("below");
                p.Assert.Hidden("below");
            });
            Kit.ExpectOneFailure(t, inner, "Visible trên phần tử bị cắt hết", "Visible: #below không nhìn thấy được");
        });

        suite.Add("assert/pixel-explains-itself", t =>
        {
            var inner = Kit.Probe(p => { p.Open(new Fx(Layout)); p.Assert.Pixel(Fmt.Center(p.Box("inside")), Rgba.FromHex(0xff0000)); });
            var f = Kit.ExpectOneFailure(t, inner, "Pixel", "mong đợi #FF0000", "thực tế #");
            t.Assert.True(f?.Path?.Contains("div#inside") == true, "lỗi Pixel chỉ ra phần tử trên cùng tại điểm đó");
            t.Assert.True(f is not null && f.Details.Exists(d => d.Contains("Quad") && d.Contains("div#inside")) && f.Details.Exists(d => d.Contains("div#frame")),
                "lỗi Pixel kèm danh sách các lớp phủ lên pixel (Explain)");
        });

        suite.Add("assert/focus-and-sequence", t =>
        {
            var host = t.Open(new CounterHost());
            t.Assert.NotFocused("c-inc");
            int mark = t.Trace.Mark();
            t.Click("c-inc");
            t.Assert.Focused("c-inc");
            t.Assert.Sequence(mark, (TraceKind.ViewDirty, "CounterView"), (TraceKind.Input, "MouseDown"), (TraceKind.Click, "CounterView"), (TraceKind.ViewRender, "CounterView"));
            var inner = Kit.Probe(p =>
            {
                p.Open(new CounterHost());
                int m = p.Trace.Mark();
                p.Hover("c-value");
                p.Assert.Sequence(m, (TraceKind.Click, null), (TraceKind.ViewRender, "CounterView"));
                p.Assert.Focused("c-inc");
            });
            t.Assert.Equal(inner.Failures.Count, 2, "Sequence thiếu bước + Focused sai");
            if (inner.Failures.Count != 2) return;
            t.Assert.True(inner.Failures[0].Message.Contains("không thấy bước 1/2: Click"), $"Sequence nêu bước thiếu: {inner.Failures[0].Message}");
            t.Assert.True(inner.Failures[1].Message.Contains("#c-inc không giữ focus") && inner.Failures[1].Message.Contains("không có gì"), $"Focused nêu ai đang giữ focus: {inner.Failures[1].Message}");
        });

        suite.Add("assert/no-lint", t =>
        {
            t.Open(new CleanView(), new Size(640, 480));
            t.Assert.NoLint();
            var inner = Kit.Probe(p =>
            {
                p.Open(new Fx(w => Fx.Page(w).Child(w.Div().Key("tiny").Class("size-3 bg-rose-500").OnClick(p, static (_, _) => { }))
                    .Child(w.Div().Class("w-40 h-2 flex").Child(w.Div().Key("bar").Class("w-0 h-2 bg-sky-500")))));
                p.Assert.NoLint();                                   // mặc định: từ warning trở lên
                p.Assert.NoLint(minimum: Severity.Info);             // tính cả info
            });
            t.Assert.Equal(inner.Failures.Count, 3, "1 warning ở lượt đầu, warning + info ở lượt sau");
            t.Assert.True(inner.Failures.Count > 0 && inner.Failures[0].Message.StartsWith("small-target (warning): vùng bấm 12×12") && inner.Failures[0].Path!.Contains("div#tiny"), "lỗi lint mang luật, mức, số đo và đường dẫn");
        });
    }
}
