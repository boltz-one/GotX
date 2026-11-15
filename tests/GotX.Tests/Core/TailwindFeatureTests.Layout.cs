using GotX;
using GotX.Test;
using GotX.Text;

static partial class TailwindFeatureTests
{
    const string Long = "Beautifully designed components that you can copy and paste into your apps. Accessible. Customizable. Open Source.";

    static UiNode TextOf(UiTest t, string key) => t.Tree.First(n => n.Kind == "text" && t.Tree.IsDescendant(n, t.Get(key)))!;

    static void RegisterLayout(TestSuite suite)
    {
        suite.Add("core/tw/line-clamp", t =>
        {
            t.Open(new Fx(w => Fx.Page(w)
                .Child(w.Div().Key("free").Class("w-[160px] text-sm").Child(Long))
                .Child(w.Div().Key("clamp").Class("w-[160px] text-sm line-clamp-2").Child(Long))
                .Child(w.Div().Key("one").Class("w-[160px] text-sm line-clamp-1").Child(Long))));
            t.Assert.True(t.Box("free").Height > 60, $"không clamp: nhiều dòng ({t.Box("free").Height}px)");
            t.Assert.Near(t.Box("clamp").Height, 40, 0.01f, "line-clamp-2: cao đúng 2 dòng text-sm (2 × 20px)");
            t.Assert.Near(t.Box("one").Height, 20, 0.01f, "line-clamp-1: 1 dòng");
            t.Assert.True(TextOf(t, "clamp").TextStyle.LineClamp == 2 && TextOf(t, "clamp").Ellipsis, "Inspect: node chữ mang LineClamp = 2 và có ellipsis");
            // Có mực ở cuối dòng 2 (dấu "…" sát mép phải hơn so với chữ bị cắt) nhưng không có gì dưới dòng 2
            var b = t.Box("clamp");
            bool inkBelow = false;
            for (float x = b.X; x < b.Right; x++) if (t.PixelAt(new Point(x, b.Bottom + 3)).G < 0.6f) inkBelow = true;
            t.Assert.True(!inkBelow, "không vẽ dòng thứ 3");
        });

        suite.Add("core/tw/text-balance-and-pretty", t =>
        {
            var text = TestFonts.Shared;
            var ts = TextStyle.Default with { FontSize = 16, LineHeight = 24 };
            const string title = "Build your component library with beautiful defaults";
            var plain = new List<TextLine>(); var bal = new List<TextLine>();
            text.Wrap(title, ts, 300, plain);
            text.WrapStyled(title, ts with { WrapStyle = TextWrapStyle.Balance }, 300, bal);
            t.Assert.Equal(bal.Count, plain.Count, "text-balance giữ nguyên số dòng");
            float Spread(List<TextLine> l) => l.Max(x => x.Width) - l.Min(x => x.Width);
            t.Assert.True(plain.Count == 2 && Spread(bal) < Spread(plain) - 20, $"các dòng đều hơn: chênh {Fmt.N(Spread(bal))}px thay vì {Fmt.N(Spread(plain))}px");

            // text-pretty: bề rộng vừa đủ để từ cuối rơi xuống một mình → pretty kéo thêm một từ xuống cùng
            const string para = "Copy and paste the code into your project and customize to your needs";
            float width = text.MeasureRange(para, 0, para.LastIndexOf(' '), ts) + 2;
            var orphan = new List<TextLine>(); var pretty = new List<TextLine>();
            text.Wrap(para, ts, width, orphan);
            text.WrapStyled(para, ts with { WrapStyle = TextWrapStyle.Pretty }, width, pretty);
            string LastLine(List<TextLine> l) => para.Substring(l[^1].Start, l[^1].Length).Trim();
            t.Assert.True(orphan.Count == 2 && !LastLine(orphan).Contains(' '), $"không pretty: dòng cuối chỉ một từ (\"{LastLine(orphan)}\")");
            t.Assert.True(pretty.Count == 2 && LastLine(pretty).Contains(' '), $"text-pretty: dòng cuối có hơn một từ (\"{LastLine(pretty)}\")");

            t.Open(new Fx(w => Fx.Page(w).Child(w.Div().Key("h").Class("w-[300px] text-balance").Child(title))));
            t.Assert.Near(t.Box("h").Height, 48, 0.01f, "trong cây: text-balance vẫn 2 dòng");
        });

        suite.Add("core/tw/tabular-nums", t =>
        {
            var text = TestFonts.Shared;
            var semi = TextStyle.Default with { Weight = FontWeight.Semibold };
            var font = text.FontFor(semi);
            t.Assert.True(font.HasTabularNums, "font semibold có feature tnum trong GSUB");
            float p1 = text.Measure("1111", semi), p0 = text.Measure("0000", semi);
            float t1 = text.Measure("1111", semi with { TabularNums = true }), t0 = text.Measure("0000", semi with { TabularNums = true });
            t.Assert.True(p1 < p0 - 2, $"chữ số tỉ lệ: 1111 hẹp hơn 0000 ({Fmt.N(p1)} < {Fmt.N(p0)})");
            t.Assert.Near(t1, t0, 0.01f, "tabular-nums: 1111 rộng bằng 0000");
            t.Open(new Fx(w => Fx.Page(w)
                .Child(w.Div().Class("flex").Child(w.Div().Key("a").Class("font-semibold tabular-nums").Child("1111")))
                .Child(w.Div().Class("flex").Child(w.Div().Key("b").Class("font-semibold tabular-nums").Child("0000")))));
            t.Assert.Near(t.Box("a").Width, t.Box("b").Width, 0.01f, "trong cây: hộp hai số bằng nhau");
        });

        suite.Add("core/tw/aspect-ratio", t =>
        {
            t.Open(new Fx(w => Fx.Page(w)
                .Child(Box(w, "video", "w-40 aspect-video bg-slate-300"))
                .Child(w.Div().Class("flex items-start").Child(Box(w, "sq", "h-10 aspect-square bg-slate-300")))
                .Child(w.Div().Key("col").Class("w-[300px] flex flex-col").Child(Box(w, "stretch", "aspect-[3/1] bg-slate-300")))
                .Child(w.Div().Class("grid grid-cols-2 w-[400px]").Child(Box(w, "cell", "aspect-square bg-slate-300")).Child(Box(w, "tall", "h-[300px] bg-slate-200")))
                .Child(Box(w, "max", "w-40 aspect-square max-h-20 bg-slate-300"))), size: new Size(900, 1200));
            t.Assert.Size("video", 160, 90);
            t.Assert.Size("sq", 40, 40);
            t.Assert.Size("stretch", 300, 100);
            t.Assert.Size("cell", 200, 200);
            t.Assert.Size("max", 160, 80);
        });

        suite.Add("core/tw/grid-placement", t =>
        {
            Div Cell(Window w, string key, string classes) => w.Div().Key(key).Class("h-10 bg-slate-300 " + classes);
            t.Open(new Fx(w => Fx.Page(w)
                .Child(w.Div().Key("g").Class("grid grid-cols-4 w-[400px]")
                    .Child(Cell(w, "a", "col-start-2")).Child(Cell(w, "b", "")).Child(Cell(w, "c", "col-[1_/_-1]")).Child(Cell(w, "d", "row-start-1 col-start-1"))
                    .Child(Cell(w, "e", "col-span-2 col-end-5")))
                .Child(w.Div().Key("fc").Class("grid grid-flow-col grid-rows-2")
                    .Child(Cell(w, "f0", "w-10")).Child(Cell(w, "f1", "w-10")).Child(Cell(w, "f2", "w-10")).Child(Cell(w, "f3", "w-10")))
                .Child(w.Div().Key("dense").Class("grid grid-cols-3 grid-flow-dense w-[300px]")
                    .Child(Cell(w, "d0", "col-span-2")).Child(Cell(w, "d1", "col-span-2")).Child(Cell(w, "d2", "")))
                .Child(w.Div().Key("sparse").Class("grid grid-cols-3 w-[300px]")
                    .Child(Cell(w, "s0", "col-span-2")).Child(Cell(w, "s1", "col-span-2")).Child(Cell(w, "s2", "")))));
            var g = t.Box("g");
            t.Assert.True(t.Box("d").X == g.X && t.Box("d").Y == g.Y, "row-start-1 col-start-1 đặt trước ở ô (1,1)");
            t.Assert.True(t.Box("a").X == g.X + 100 && t.Box("a").Y == g.Y, "col-start-2: cột 2 hàng 1");
            t.Assert.True(t.Box("b").X == g.X + 200 && t.Box("b").Y == g.Y, "phần tử tự động đi tiếp sau con trỏ");
            t.Assert.True(t.Box("c").X == g.X && t.Box("c").Width == 400 && t.Box("c").Y == g.Y + 40, "col-[1_/_-1]: trọn hàng, xuống hàng 2");
            t.Assert.True(t.Box("e").X == g.X + 200 && t.Box("e").Width == 200 && t.Box("e").Y == g.Y + 80, "col-span-2 col-end-5: hai cột cuối");
            var fc = t.Box("fc");
            t.Assert.True(t.Box("f1").X == fc.X && t.Box("f1").Y == fc.Y + 40 && t.Box("f2").X == fc.X + 40 && t.Box("f2").Y == fc.Y,
                "grid-flow-col: đi xuống trước rồi sang cột ngầm kế tiếp");
            t.Assert.True(t.Box("d2").Y == t.Box("dense").Y && t.Box("s2").Y == t.Box("sparse").Y + 40, "grid-flow-dense lấp ô trống phía trước; mặc định thì không");
        });

        suite.Add("core/tw/decoration-breakpoint-calc", t =>
        {
            t.Open(new Fx(w => Fx.Page(w)
                .Child(w.Div().Class("flex").Child(w.Div().Key("u0").Class("text-xl underline decoration-[#ff0000]").Child("xoxoxo")))
                .Child(w.Div().Class("flex").Child(w.Div().Key("u8").Class("text-xl underline underline-offset-8 decoration-4 decoration-[#ff0000]").Child("xoxoxo")))
                .Child(Box(w, "bp", "size-10 bg-[#0000ff] min-[950px]:bg-[#ff0000] max-[900px]:bg-[#00ff00]"))
                .Child(w.Div().Class("w-[300px]").Child(Box(w, "calc", "h-4 w-full max-w-[calc(100%-2rem)] bg-slate-300")))
                .Child(Box(w, "vw", "h-4 w-[calc(100vw-100px)]"))), size: new Size(1000, 500));
            (int rows, float top) Red(string key)
            {
                var b = t.Box(key); int n = 0; float first = -1;
                for (float y = b.Y; y < b.Bottom + 12; y++)
                    if (t.PixelAt(new Point(b.X + b.Width / 2, y)) is { R: > 0.8f, G: < 0.3f })
                    { n++; if (first < 0) first = y; }
                return (n, first);
            }
            var (n0, y0) = Red("u0"); var (n8, y8) = Red("u8");
            t.Assert.True(n0 >= 1 && n8 == 4, $"decoration-[màu] tô nét gạch; decoration-4 dày 4px (được {n8})");
            t.Assert.True(y8 >= y0 + 5, $"underline-offset-8 đẩy nét xuống ({Fmt.N(y8 - t.Box("u8").Y)} so với {Fmt.N(y0 - t.Box("u0").Y)})");
            t.Assert.True(Close(At(t, "bp", 20, 20), (1, 0, 0)), "min-[950px]: áp ở cửa sổ rộng 1000");
            t.Assert.True(new VariantContext(false, 1000, 500, 16).BreakpointBucket != new VariantContext(false, 920, 500, 16).BreakpointBucket,
                "breakpoint tuỳ ý tính vào BreakpointBucket (cache view bị xoá khi đi qua nó)");
            t.Assert.Near(t.Box("calc").Width, 268, 0.01f, "max-w-[calc(100%-2rem)] = 300 - 32");
            t.Assert.Near(t.Box("vw").Width, 900, 0.01f, "w-[calc(100vw-100px)] = 1000 - 100");
        });
    }
}
