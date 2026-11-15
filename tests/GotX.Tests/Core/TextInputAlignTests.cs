using GotX;
using GotX.Test;
using GotX.Widgets;

/// <summary>TextInput theo text-right / text-center: chữ, con trỏ, bấm chuột và di chuyển lên/xuống dùng cùng độ lệch căn lề.</summary>
static class TextInputAlignTests
{
    sealed class Host(string boxClass, bool multiline, string text, string placeholder = "") : View
    {
        public TextInput Field = null!;
        protected override void OnCreated() =>
            Field = App.New(new TextInput { Multiline = multiline, Text = text, Placeholder = placeholder, BoxClass = boxClass });
        public override Div Render(Window w) => Fx.Page(w).Child(w.Div().Key("field").Child(w.View(Field)));
    }

    /// <summary>Cột đầu / cuối có pixel tối trong dải y đã cho (trong khoảng [x0, x1)).</summary>
    static (int First, int Last) Ink(UiTest t, int x0, int x1, int y0, int y1)
    {
        int first = -1, last = -1;
        for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
                if (t.PixelAt(new Point(x + 0.5f, y + 0.5f)).R < 0.6f) { if (first < 0) first = x; last = x; break; }
        return (first, last);
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/text-input/right-aligned-text-and-caret", t =>
        {
            var h = t.Open(new Host("h-8 w-40 px-3 text-right border border-white bg-white text-slate-900", false, "18"));
            // hộp (24,24) 160×32, nội dung x ∈ [36, 172] (viền 1 + padding 12)
            var (first, last) = Ink(t, 38, 171, 30, 50);
            t.Assert.True(first > 120 && last >= 168 && last < 173, $"chữ \"18\" nằm sát mép phải hộp nội dung (mực {first}..{last})");
            t.Click(new Point(170, 40)); // bấm bên phải chữ căn phải → con trỏ ở cuối
            t.Type("9");
            t.Assert.Equal(h.Field.Text, "189", "gõ tiếp ở cuối");
            var (f2, l2) = Ink(t, 38, 171, 30, 50);
            t.Assert.True(l2 >= 168 && f2 > 100, $"vẫn căn phải sau khi gõ (mực {f2}..{l2})");
        });

        suite.Add("core/text-input/click-maps-through-alignment-offset", t =>
        {
            var h = t.Open(new Host("h-8 w-40 px-3 text-right border border-white bg-white text-slate-900", false, "18"));
            t.Click(new Point(40, 40));      // vùng trống bên trái chữ → con trỏ về đầu
            t.Type("7");
            t.Assert.Equal(h.Field.Text, "718", "bấm khoảng trống bên trái chữ căn phải: con trỏ ở đầu chuỗi");
            t.Click(new Point(170, 40));     // sát mép phải → sau ký tự cuối
            t.Type("5");
            t.Assert.Equal(h.Field.Text, "7185", "bấm bên phải chữ: con trỏ ở cuối");
        });

        suite.Add("core/text-input/centered-multiline-lines-are-centered", t =>
        {
            t.Open(new Host("w-40 px-3 py-1 text-center border border-white bg-white text-slate-900 text-sm", true, "aaaa bbbb cccc dddd eeee ffff"));
            // hộp rộng 160, nội dung [37, 171] = 134px: dòng đầu căn giữa ⇒ khoảng trắng trái ≈ phải
            var (f, l) = Ink(t, 37, 172, 30, 44);
            int left = f - 37, right = 171 - l;
            t.Assert.True(f >= 0 && Math.Abs(left - right) <= 3, $"dòng đầu căn giữa: lề trái {left}, lề phải {right}");
        });

        suite.Add("core/text-input/left-alignment-unchanged", t =>
        {
            t.Open(new Host("h-8 w-40 px-3 border border-white bg-white text-slate-900", false, "18"));
            var (f, l) = Ink(t, 38, 171, 30, 50);
            t.Assert.True(f >= 36 && f < 42 && l < 70, $"mặc định căn trái (mực {f}..{l})");
        });

        suite.Add("core/text-input/placeholder-wraps-and-aligns", t =>
        {
            var h = t.Open(new Host("w-40 px-3 py-1 text-right border border-white bg-white text-slate-900 text-sm", true, "",
                "một chú thích khá dài phải xuống dòng"));
            var first = Ink(t, 37, 172, 30, 44);
            var second = Ink(t, 37, 172, 44, 60);
            t.Assert.True(second.First >= 0, "placeholder xuống dòng thứ hai (không bị cắt bằng dấu …)");
            t.Assert.True(first.Last >= 165, $"placeholder căn phải (mực dòng đầu đến {first.Last})");
        });
    }
}
