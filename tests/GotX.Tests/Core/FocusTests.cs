using GotX;
using GotX.Test;
using GotX.Widgets;

/// <summary>Focus bàn phím của Core: thứ tự Tab, kích hoạt bằng phím, phím nổi lên cha, focus-visible, AutoFocus.</summary>
static class FocusTests
{
    /// <summary>Ba nút focus được trong một vùng có OnKey, kèm một ô nhập một dòng và một nút bị vô hiệu.</summary>
    sealed class Pad : View
    {
        public TextInput Field = null!;
        public int Clicks, Downs, Ups, Enters;
        public string Last = "";
        public bool WithAuto, WithLate;

        protected override void OnCreated() => Field = App.New(new TextInput { Multiline = false });

        public override Div Render(Window w)
        {
            Div Btn(string key, bool disabled = false) => w.Div().Key(key).Focusable().Disabled(disabled)
                .Class("h-8 px-3 flex items-center rounded border border-slate-300 text-sm focus:bg-sky-100 focus-visible:ring-2 focus-visible:ring-sky-500")
                .OnClick(this, static (v, _) => { v.Clicks++; v.Notify(); }).Child(key);
            var row = w.Div().Class("flex items-center gap-2").Child(Btn("b1")).Child(Btn("b2")).Child(Btn("dead", disabled: true)).Child(Btn("b3"));
            if (WithAuto) row.Child(w.Div().Key("auto").Focusable().AutoFocus().Class("size-8 bg-slate-200 focus:bg-sky-300"));
            if (WithLate) row.Child(w.Div().Key("late").Focusable().Class("size-8 bg-slate-200 focus:bg-sky-300"));
            return Fx.Page(w)
                .OnKey(this, static (v, e) =>
                {
                    v.Last = e.Key + (e.Shift ? "+Shift" : "");
                    if (e.Key == Key.Down) { v.Downs++; return true; }
                    if (e.Key == Key.Up) { v.Ups++; return true; }
                    if (e.Key == Key.Enter) v.Enters++; // không xử lý: để phần tử focus tự kích hoạt
                    return false;
                })
                .Child(row)
                .Child(w.Div().Key("field").Class("w-48").Child(w.View(Field)));
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/focus/tab-order-is-paint-order", t =>
        {
            var v = t.Open(new Pad());
            t.Assert.True(!t.Window.HasElementFocus && t.Window.FocusedBounds is null, "ban đầu chưa có focus");
            string[] order = ["b1", "b2", "b3"];
            foreach (string key in order)
            {
                t.Press(Key.Tab);
                t.Assert.Focused(key);
                t.Assert.True(t.Window.FocusVisible, $"Tab tới #{key}: focus-visible");
                t.Assert.Equal(t.Window.FocusedBounds, t.Box(key), $"FocusedBounds là hộp của #{key}");
            }
            t.Press(Key.Tab);
            t.Assert.True(v.Field.IsFocused && !t.Window.HasElementFocus, "Tab kế tiếp vào ô nhập (bỏ qua nút disabled)");
            t.Press(Key.Tab);
            t.Assert.Focused("b1"); // quay vòng về đầu
            t.Press(Key.Tab, Modifiers.Shift);
            t.Assert.True(v.Field.IsFocused, "Shift+Tab từ phần tử đầu quay vòng về cuối");
            t.Press(Key.Tab, Modifiers.Shift);
            t.Assert.Focused("b3");
            t.Press(Key.Tab, Modifiers.Shift);
            t.Assert.Focused("b2");
            t.Assert.NotFocused("dead");
        });

        suite.Add("core/focus/enter-space-activate", t =>
        {
            var v = t.Open(new Pad());
            t.Press(Key.Tab);
            int mark = t.Trace.Mark();
            t.Press(Key.Enter);
            t.Assert.Equal(v.Clicks, 1, "Enter gọi OnClick của phần tử focus");
            t.Press(Key.Space);
            t.Assert.Equal(v.Clicks, 2, "Space cũng vậy");
            t.Assert.Equal(v.Enters, 0, "Enter đã được nút dùng thì không nổi lên OnKey của cha");
            t.Assert.Sequence(mark, (TraceKind.Key, "Enter"), (TraceKind.Click, "(phím)"), (TraceKind.ViewDirty, "Pad"));
        });

        suite.Add("core/focus/keys-bubble-to-ancestor", t =>
        {
            var v = t.Open(new Pad());
            t.Press(Key.Down);
            t.Assert.Equal(v.Downs, 0, "không có gì focus thì OnKey của vùng chứa không nhận phím");
            t.Press(Key.Tab);
            t.Press(Key.Down);
            t.Press(Key.Up, Modifiers.Shift);
            t.Assert.True(v.Downs == 1 && v.Ups == 1 && v.Last == "Up+Shift", $"phím nổi từ nút đang focus lên OnKey của cha, kèm modifier ({v.Last})");
            t.Press(Key.Escape);
            t.Assert.True(!t.Window.HasElementFocus && v.Last == "Escape", "Escape không ai xử lý → bỏ focus (cha vẫn thấy phím trước)");
        });

        suite.Add("core/focus/single-line-input-lets-up-down-bubble", t =>
        {
            var v = t.Open(new Pad());
            t.Click("field");
            t.Assert.True(v.Field.IsFocused, "bấm vào ô nhập để focus");
            t.Type("abc");
            t.Press(Key.Down);
            t.Press(Key.Up);
            t.Assert.True(v.Downs == 1 && v.Ups == 1, "ô một dòng không dùng ↑/↓ → nổi lên OnKey của cha");
            t.Press(Key.Left);
            t.Assert.True(v.Last == "Up" && v.Field.Caret == 2, "←/→ thì ô nhập tự dùng, không nổi lên");
            v.Field.Multiline = true;
            t.Press(Key.Down);
            t.Assert.Equal(v.Downs, 1, "ô nhiều dòng tự dùng ↑/↓");
        });

        suite.Add("core/focus/mouse-focus-is-not-focus-visible", t =>
        {
            var v = t.Open(new Pad());
            t.Click("b2");
            t.Assert.Focused("b2");
            t.Assert.True(!t.Window.FocusVisible && v.Clicks == 1, "bấm chuột: có focus nhưng không focus-visible");
            var mouse = t.PixelAt(new Point(t.Box("b2").X + 3, t.Box("b2").Y + 3));
            t.Assert.True(Pixels.Delta(mouse, Rgba.FromHex(0xe0f2fe)) < 12, $"variant focus: áp dụng (nền sky-100), thực tế {Fmt.C(mouse)}");
            var ringAt = new Point(t.Box("b2").X - 1.5f, t.Box("b2").Y + 16);
            t.Assert.Pixel(ringAt, Rgba.White, 4); // chưa có vòng focus
            t.Press(Key.Tab, Modifiers.Shift);
            t.Press(Key.Tab);
            t.Assert.True(t.Window.FocusVisible && t.Window.IsFocused("b2"), "quay lại bằng bàn phím: focus-visible");
            t.Assert.True(Pixels.Delta(t.PixelAt(ringAt), Rgba.White) > 60, $"vòng focus-visible:ring-2 hiện ra, thực tế {Fmt.C(t.PixelAt(ringAt))}");
            t.Click(new Point(600, 600));
            t.Assert.True(!t.Window.HasElementFocus, "bấm ra chỗ trống bỏ focus");
            t.Click("dead");
            t.Assert.True(!t.Window.HasElementFocus && v.Clicks == 1, "nút disabled không nhận focus lẫn click");
        });

        suite.Add("core/focus/autofocus-and-focuskey", t =>
        {
            var v = t.Open(new Pad { WithAuto = true });
            t.Assert.Focused("auto");
            t.Assert.True(!t.Window.FocusVisible, "AutoFocus lúc chưa dùng bàn phím: không hiện vòng focus");
            t.Window.FocusKey("b3");
            t.Frames();
            t.Assert.Focused("b3");
            t.Assert.True(t.Window.FocusVisible, "FocusKey bằng code: focus-visible");
            t.Window.MoveFocus(backward: true);
            t.Frames();
            t.Assert.Focused("b2");
            t.Window.Focus(v.Field);
            t.Frames();
            t.Assert.True(v.Field.IsFocused && !t.Window.HasElementFocus && t.Tree.First(n => n.Focused) is { } box && t.Tree.IsDescendant(box, t.Get("field")),
                "Focus(ô nhập) bỏ focus phần tử; Inspect đánh dấu khung ô nhập là Focused");
            t.Window.Blur();
            t.Frames();
            t.Assert.True(!v.Field.IsFocused && t.Tree.First(n => n.Focused) is null, "Blur bỏ mọi focus");
            // FocusKey trước khi phần tử tồn tại: focus ở frame nó xuất hiện.
            var pad = t.Open(new Pad());
            t.Window.FocusKey("late");
            t.Frames();
            t.Assert.True(!t.Window.HasElementFocus, "phần tử chưa có thì chưa focus");
            pad.WithLate = true;
            pad.Notify();
            t.Frames();
            t.Assert.Focused("late");
        });
    }
}
