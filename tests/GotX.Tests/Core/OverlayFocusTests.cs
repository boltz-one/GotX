using GotX;
using GotX.Test;
using GotX.Widgets;

/// <summary>Focus khi có overlay: modal giữ Tab bên trong và trả focus khi đóng; lớp không modal chỉ trả focus nếu focus đang ở trong nó.</summary>
static class OverlayFocusTests
{
    /// <summary>Nội dung overlay: hai nút focus được, nút đầu tự focus.</summary>
    sealed class Panel : View
    {
        public int Picks;
        public override Div Render(Window w) => w.Div().Key("panel").Class("w-56 p-3 flex flex-col gap-2 rounded-lg border border-slate-300 bg-white")
            .Child(Item(w, "first").AutoFocus())
            .Child(Item(w, "second"));

        Div Item(Window w, string key) => w.Div().Key(key).Focusable()
            .Class("h-8 px-2 flex items-center rounded text-sm focus:bg-sky-100")
            .OnClick(this, static (v, _) => v.Picks++).Child(key);
    }

    sealed class Host : View
    {
        public Panel Panel = null!;
        public TextInput Field = null!;
        public Overlay? Open;
        Window? _window;

        protected override void OnCreated()
        {
            Panel = App.New(new Panel());
            Field = App.New(new TextInput { Multiline = false });
        }

        public void Show(bool modal) => Open = _window!.OpenOverlay(Panel, new OverlayOptions { Modal = modal, Anchor = modal ? null : "menu-anchor" });

        public override Div Render(Window w)
        {
            _window = w;
            Div Btn(string key) => w.Div().Key(key).Focusable().Class("h-8 px-3 flex items-center rounded border border-slate-300 text-sm focus:bg-sky-100");
            return Fx.Page(w)
                .Child(Btn("open-modal").OnClick(this, static (v, _) => v.Show(modal: true)).Child("Mở hộp thoại"))
                .Child(Btn("open-menu").Anchor("menu-anchor").OnClick(this, static (v, _) => v.Show(modal: false)).Child("Mở menu"))
                .Child(Btn("other").Child("Nút khác"))
                .Child(w.Div().Key("field").Class("w-48").Child(w.View(Field)));
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/overlay/modal-traps-tab-and-restores-focus", t =>
        {
            var host = t.Open(new Host());
            t.Press(Key.Tab);
            t.Assert.Focused("open-modal");
            int mark = t.Trace.Mark();
            t.Press(Key.Enter);
            t.Assert.True(t.Window.Overlays.Count == 1 && host.Open!.IsOpen, "Enter trên nút mở hộp thoại");
            t.Assert.Focused("first");
            t.Assert.True(t.Window.FocusVisible, "AutoFocus sau thao tác bàn phím: hiện vòng focus");
            t.Assert.Sequence(mark, (TraceKind.Overlay, "mở Panel"), (TraceKind.Focus, null));
            string[] cycle = ["second", "first", "second"];
            foreach (string key in cycle)
            {
                t.Press(Key.Tab);
                t.Assert.Focused(key); // Tab quay vòng trong hộp thoại, không lọt ra các nút bên dưới
            }
            t.Press(Key.Tab, Modifiers.Shift);
            t.Assert.Focused("first");
            t.Press(Key.Tab, Modifiers.Shift);
            t.Assert.Focused("second");
            t.Press(Key.Enter);
            t.Assert.Equal(host.Panel.Picks, 1, "Enter kích hoạt phần tử trong hộp thoại");
            t.Press(Key.Escape);
            t.Assert.True(t.Window.Overlays.Count == 0 && !host.Open!.IsOpen, "Escape đóng hộp thoại");
            t.Assert.Focused("open-modal");
            t.Assert.True(t.Window.FocusVisible, "focus trở về nút đã mở, vẫn là focus bàn phím");
        });

        suite.Add("core/overlay/modal-restores-text-input-focus", t =>
        {
            var host = t.Open(new Host());
            t.Click("field");
            t.Type("xin chào");
            host.Show(modal: true);
            t.Frames();
            t.Assert.True(!host.Field.IsFocused, "mở modal: ô nhập phía sau mất focus");
            t.Assert.Focused("first");
            t.Type("lọt?");
            t.Assert.Equal(host.Field.Text, "xin chào", "gõ phím không lọt xuống ô nhập phía sau modal");
            host.Open!.Close();
            t.Frames();
            t.Assert.True(host.Field.IsFocused && !t.Window.HasElementFocus, "đóng modal: focus trở về ô nhập");
        });

        suite.Add("core/overlay/modal-blocks-mouse-below", t =>
        {
            var host = t.Open(new Host());
            t.Click("open-modal");
            t.Assert.Focused("first");
            t.Assert.True(!t.Window.FocusVisible, "mở bằng chuột: AutoFocus không hiện vòng focus");
            t.Assert.True(t.Lint(new LintOptions().Only("occluded")).Count == 0, "lint: overlay che nút bên dưới là chủ ý, không báo occluded");
            t.Click("other");
            t.Assert.True(t.Window.Overlays.Count == 0, "bấm ra ngoài đóng modal");
            t.Assert.NotFocused("other"); // cú bấm đó không lọt xuống nút bên dưới
            t.Assert.Focused("open-modal");
        });

        suite.Add("core/overlay/non-modal-restores-only-if-focus-inside", t =>
        {
            var host = t.Open(new Host());
            t.Press(Key.Tab);
            t.Press(Key.Tab);
            t.Assert.Focused("open-menu");
            t.Press(Key.Enter);
            t.Assert.Focused("first");
            var panel = t.Box("panel");
            var anchor = t.Box("open-menu");
            t.Assert.True(MathF.Abs(panel.Y - (anchor.Bottom + 4)) < 0.5f && MathF.Abs(panel.X - anchor.X) < 0.5f, $"menu neo ngay dưới nút mở: {Fmt.B(panel)} so với {Fmt.B(anchor)}");
            t.Press(Key.Escape);
            t.Assert.True(t.Window.Overlays.Count == 0, "Escape đóng menu");
            t.Assert.Focused("open-menu"); // focus đang ở trong menu → trả về nút mở

            // Lần này người dùng đã đưa focus ra ngoài menu trước khi nó đóng: không được giật focus về nút mở.
            t.Press(Key.Enter);
            t.Assert.Focused("first");
            t.Window.FocusKey("other");
            t.Frames();
            t.Assert.Focused("other");
            host.Open!.Close();
            t.Frames();
            t.Assert.Focused("other");
        });
    }
}
