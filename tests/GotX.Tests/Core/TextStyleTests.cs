using GotX;
using GotX.Test;

/// <summary>Kiểu chữ: font-mono, uppercase, gạch chân / gạch ngang, tracking, italic – đo bằng hộp của node chữ và pixel.</summary>
static class TextStyleTests
{
    /// <summary>Mỗi dòng là một div có khoá chứa đúng một node chữ; trả về hộp của node chữ đó.</summary>
    static Bounds TextBox(UiTest t, string key) => t.Tree.First(n => n.Kind == "text" && t.Tree.IsDescendant(n, t.Get(key)))!.Screen;

    static Div Line(Window w, string key, string classes, string text) => w.Div().Key(key).Class("flex").Child(w.Div().Class(classes).Child(text));

    /// <summary>Số pixel "có mực" (tối hơn nền trắng rõ rệt) trên một hàng pixel của hộp.</summary>
    static int InkOnRow(UiTest t, Bounds box, float y)
    {
        int n = 0;
        for (float x = box.X; x < box.Right; x++)
            if (t.PixelAt(new Point(x, y)).G < 0.6f) n++;
        return n;
    }

    /// <summary>Hàng có nhiều mực nhất trong hộp và số pixel mực của nó.</summary>
    static (float Y, int Ink) DensestRow(UiTest t, Bounds box)
    {
        (float, int) best = (0, -1);
        for (float y = box.Y; y < box.Bottom; y++)
        {
            int ink = InkOnRow(t, box, y);
            if (ink > best.Item2) best = (y, ink);
        }
        return best;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/text/font-mono-equal-advance", t =>
        {
            t.Open(new Fx(w => Fx.Page(w)
                .Child(Line(w, "sans-i", "text-base", "iiiiiiii")).Child(Line(w, "sans-w", "text-base", "WWWWWWWW"))
                .Child(Line(w, "mono-i", "text-base font-mono", "iiiiiiii")).Child(Line(w, "mono-w", "text-base font-mono", "WWWWWWWW"))
                .Child(Line(w, "back", "text-base font-mono", "ii").Child(w.Div().Class("font-sans").Child("ii")))));
            t.Assert.True(TextBox(t, "sans-w").Width > TextBox(t, "sans-i").Width * 2.5f, "font thường: W rộng hơn i nhiều");
            t.Assert.Near(TextBox(t, "mono-i").Width, TextBox(t, "mono-w").Width, 0.01f, "font-mono: 8 chữ i rộng đúng bằng 8 chữ W");
            t.Assert.True(TextBox(t, "mono-i").Width > TextBox(t, "sans-i").Width * 1.5f, "và không phải là font thường");
            var mono = t.Tree.First(n => n.Kind == "text" && n.Text == "iiiiiiii" && n.TextStyle.Family == FontFamily.Mono);
            t.Assert.True(mono is not null && TestFonts.Shared.HasFont(FontFamily.Mono, italic: false), "Inspect mang họ font đã tính; có font mono thật");
        });

        suite.Add("core/text/uppercase-and-case", t =>
        {
            t.Open(new Fx(w => Fx.Page(w)
                .Child(Line(w, "plain", "text-base", "xin chào")).Child(Line(w, "upper", "text-base uppercase", "xin chào"))
                .Child(Line(w, "typed", "text-base", "XIN CHÀO")).Child(Line(w, "cap", "text-base capitalize", "xin chào"))
                .Child(Line(w, "typed-cap", "text-base", "Xin Chào")).Child(Line(w, "lower", "text-base lowercase", "XIN CHÀO"))));
            t.Assert.Near(TextBox(t, "upper").Width, TextBox(t, "typed").Width, 0.01f, "uppercase vẽ đúng như gõ chữ hoa (kể cả chữ có dấu)");
            t.Assert.True(TextBox(t, "upper").Width > TextBox(t, "plain").Width + 4, "và rộng hơn chữ thường");
            t.Assert.Near(TextBox(t, "cap").Width, TextBox(t, "typed-cap").Width, 0.01f, "capitalize viết hoa chữ đầu mỗi từ");
            t.Assert.Near(TextBox(t, "lower").Width, TextBox(t, "plain").Width, 0.01f, "lowercase");
            t.Assert.Text("upper", "xin chào"); // Inspect giữ chuỗi gốc; kiểu hoa/thường nằm ở TextStyle.Case
            t.Assert.Equal(t.Tree.FindText("xin chào")!.TextStyle.Case, TextCase.None, "node chữ thường: Case = None");
        });

        suite.Add("core/text/underline-and-line-through", t =>
        {
            // Chữ "o" không có nét ngang dài nào: hàng nhiều mực nhất của chữ trần chỉ là vài pixel.
            t.Open(new Fx(w => Fx.Page(w).Child(Line(w, "plain", "text-xl", "o o o o o"))
                .Child(Line(w, "under", "text-xl underline", "o o o o o")).Child(Line(w, "strike", "text-xl line-through", "o o o o o"))));
            var plain = TextBox(t, "plain"); var under = TextBox(t, "under"); var strike = TextBox(t, "strike");
            t.Assert.Near(under.Width, plain.Width, 0.01f, "gạch chân không đổi bề rộng chữ");
            var (_, bare) = DensestRow(t, plain);
            var (underY, underInk) = DensestRow(t, under);
            var (strikeY, strikeInk) = DensestRow(t, strike);
            t.Assert.True(bare < plain.Width * 0.6f, $"chữ trần: không có hàng nào kín mực ({bare}/{plain.Width})");
            t.Assert.True(underInk >= under.Width - 2, $"underline: một nét ngang chạy hết bề rộng chữ ({underInk}/{under.Width})");
            t.Assert.True(strikeInk >= strike.Width - 2, $"line-through: cũng vậy ({strikeInk}/{strike.Width})");
            float underAt = (underY - under.Y) / under.Height, strikeAt = (strikeY - strike.Y) / strike.Height;
            t.Assert.True(underAt > 0.7f, $"nét gạch chân nằm dưới đường gốc (ở {Fmt.N(underAt * 100)}% chiều cao dòng)");
            t.Assert.True(strikeAt is > 0.4f and < 0.7f, $"nét gạch ngang đi qua thân chữ (ở {Fmt.N(strikeAt * 100)}% chiều cao dòng)");
        });

        suite.Add("core/text/tracking", t =>
        {
            t.Open(new Fx(w => Fx.Page(w).Child(Line(w, "normal", "text-base", "KHOẢNG CÁCH"))
                .Child(Line(w, "widest", "text-base tracking-widest", "KHOẢNG CÁCH")).Child(Line(w, "tight", "text-base tracking-tight", "KHOẢNG CÁCH"))));
            float normal = TextBox(t, "normal").Width, widest = TextBox(t, "widest").Width, tight = TextBox(t, "tight").Width;
            // tracking-widest = 0.1em sau mỗi ký tự: 11 ký tự × 1.6 px = 17.6 px (hộp chữ làm tròn lên pixel nguyên).
            t.Assert.Near(widest - normal, 17.6f, 1.01f, "tracking-widest nới đúng 0.1em mỗi ký tự");
            t.Assert.True(tight < normal, $"tracking-tight thu hẹp ({tight} < {normal})");
            t.Assert.Near(t.Tree.FindText("KHOẢNG CÁCH")!.TextStyle.Tracking, 0, 0.0001f, "node thường: tracking 0");
        });

        suite.Add("core/text/italic", t =>
        {
            t.Open(new Fx(w => Fx.Page(w).Child(Line(w, "upright", "text-2xl", "lllllll")).Child(Line(w, "italic", "text-2xl italic", "lllllll"))
                .Child(Line(w, "mono", "text-2xl font-mono", "lllllll")).Child(Line(w, "faux", "text-2xl font-mono italic", "lllllll"))));
            t.Assert.True(TestFonts.Shared.HasFont(FontFamily.Sans, italic: true), "có font nghiêng thật cho họ sans");
            // Chữ "l" đứng: mực ở hàng trên và hàng dưới thẳng cột. Chữ nghiêng: phần trên lệch sang phải so với phần dưới.
            t.Assert.Near(Lean(t, TextBox(t, "upright")), 0, 0.6f, "chữ đứng không nghiêng");
            t.Assert.True(Lean(t, TextBox(t, "italic")) > 1.5f, $"italic thật: đỉnh nét lệch phải {Fmt.N(Lean(t, TextBox(t, "italic")))}px so với chân");
            t.Assert.True(!TestFonts.Shared.HasFont(FontFamily.Mono, italic: true) && Lean(t, TextBox(t, "faux")) > 1.5f, $"mono không có font nghiêng → nghiêng giả bằng phép trượt ({Fmt.N(Lean(t, TextBox(t, "faux")))}px)");
            t.Assert.Near(Lean(t, TextBox(t, "mono")), 0, 0.6f, "mono đứng không nghiêng");
            t.Assert.True(t.Tree.FindText("lllllll") is { } n && !n.TextStyle.Italic, "Inspect: node đầu không italic");
        });
    }

    /// <summary>Độ nghiêng: trọng tâm mực theo x ở hàng gần đỉnh trừ đi ở hàng gần chân (dương = ngả sang phải).</summary>
    static float Lean(UiTest t, Bounds box)
    {
        float top = -1, bottom = -1;
        for (float y = box.Y; y < box.Bottom && top < 0; y++) if (InkOnRow(t, box.Inflate(4), y) >= 4) top = y + 2;
        for (float y = box.Bottom - 1; y >= box.Y && bottom < 0; y--) if (InkOnRow(t, box.Inflate(4), y) >= 4) bottom = y - 2;
        return Centroid(t, box.Inflate(4), top) - Centroid(t, box.Inflate(4), bottom);
    }

    static float Centroid(UiTest t, Bounds box, float y)
    {
        float sum = 0, weight = 0;
        for (float x = box.X; x < box.Right; x++)
        {
            float ink = 1 - t.PixelAt(new Point(x, y)).G;
            sum += ink * x; weight += ink;
        }
        return weight > 0 ? sum / weight : 0;
    }
}
