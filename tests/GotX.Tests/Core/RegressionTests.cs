using GotX.Text;
using GotX;
using GotX.Test;

/// <summary>Các lỗi Core đã sửa, khoá lại để không tái diễn: Escape/focus dưới lớp modal, hẹn giờ gọi lồng, trace cuộn, bảng tra ảnh.</summary>
static class RegressionTests
{
    sealed class Panel : View
    {
        public override Div Render(Window w) => w.Div().Key("panel").Class("w-56 p-3 flex flex-col gap-2 rounded-lg border border-slate-300 bg-white")
            .Child(w.Div().Key("first").Focusable().AutoFocus().Class("h-8 px-2 rounded text-sm focus:bg-sky-100").Child("first"))
            .Child(w.Div().Key("second").Focusable().Class("h-8 px-2 rounded text-sm focus:bg-sky-100").Child("second"));
    }

    sealed class Host : View
    {
        public Panel Panel = null!;
        public Overlay? Open;
        Window? _window;

        protected override void OnCreated() => Panel = App.New(new Panel());

        public void ShowLocked() => Open = _window!.OpenOverlay(Panel, new OverlayOptions { Modal = true, DismissOnEscape = false, DismissOnOutsideClick = false });

        public override Div Render(Window w)
        {
            _window = w;
            var list = w.Div().Key("list").Class("h-24 w-48 overflow-y-auto border border-slate-300");
            for (int i = 0; i < 12; i++) list.Child(w.Div().Class("h-8 px-2 text-sm").Child("Dòng " + i));
            return Fx.Page(w)
                .Child(w.Div().Key("open").Focusable().Class("h-8 px-3 rounded border border-slate-300 text-sm")
                    .OnClick(this, static (v, _) => v.ShowLocked()).Child("Mở"))
                .Child(w.Div().Key("below").Focusable().Class("h-8 px-3 rounded border border-slate-300 text-sm").Child("Bên dưới"))
                .Child(list);
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/regression/locked-modal-keeps-escape-and-focus", t =>
        {
            var host = t.Open(new Host());
            t.Click("open");
            t.Assert.Focused("first");
            t.Press(Key.Escape);
            t.Assert.True(host.Open is { IsOpen: true }, "modal không cho Escape vẫn mở");
            t.Assert.Focused("first"); // trước đây Escape lọt xuống và làm mất focus bên trong hộp thoại

            t.Window.FocusKey("below");
            t.Frames(2);
            t.Assert.Focused("first"); // FocusKey không được đưa focus xuống dưới lớp modal
            host.Open!.Close();
            t.Frames(2);
            t.Window.FocusKey("below");
            t.Frames(2);
            t.Assert.Focused("below");
        });

        suite.Add("core/regression/timer-reentrancy", t =>
        {
            t.Open(new Host());
            var app = t.Window.App;
            int first = 0, second = 0;
            app.SetTimeout(0.05, () => { first++; app.RunTimers(); }); // callback gọi lại RunTimers trong lúc đang duyệt
            app.SetTimeout(0.05, () => second++);
            t.Advance(0.2);
            t.Assert.True(first == 1 && second == 1, $"mỗi hẹn giờ chạy đúng một lần (first={first}, second={second})");
            t.Assert.Equal(app.PendingTimers, 0, "không còn hẹn giờ treo");
        });

        suite.Add("core/regression/scroll-trace", t =>
        {
            t.Open(new Host());
            int mark = t.Trace.Mark();
            t.Scroll("list", 64);
            t.Assert.True(t.Trace.Since(mark).Any(e => e.Kind == TraceKind.Scroll), "lăn chuột trên vùng cuộn phát sự kiện Scroll trong nhật ký");
        });

        suite.Add("core/text/kerning", t =>
        {
            t.Open(new Host());
            var text = t.Window.Text;
            var style = TextStyle.Default with { FontSize = 48 };
            var font = text.FontFor(style);
            t.Assert.True(font.HasKerning, "font mặc định có bảng kerning");
            float pair = text.Measure("AV", style), apart = text.Measure("A", style) + text.Measure("V", style);
            t.Assert.True(pair < apart - 1, $"cặp AV được kéo sát lại: {pair:0.##} < {apart:0.##}");
            float wrapped = 0;
            var lines = new List<TextLine>();
            text.Wrap("AVATAR", style, float.NaN, lines);
            wrapped = lines[0].Width;
            t.Assert.Near(wrapped, text.Measure("AVATAR", style), 0.01f, "ngắt dòng đo cùng độ rộng có kerning như lúc vẽ");
        });

        suite.Add("core/layout/flex-min-content", t =>
        {
            // Như CSS min-width:auto: phần tử flex-1 không co nhỏ hơn nội dung không xuống dòng được; min-w-0 cho phép co.
            t.Open(new Fx(w => Fx.Page(w)
                .Child(w.Div().Key("tabs").Class("inline-flex w-fit gap-1 p-1 bg-slate-100")
                    .Child(w.Div().Key("t1").Class("flex-1 whitespace-nowrap px-2 text-sm").Child("Outline"))
                    .Child(w.Div().Key("t2").Class("flex-1 whitespace-nowrap px-2 text-sm").Child("Past Performance"))
                    .Child(w.Div().Key("t3").Class("flex-1 whitespace-nowrap px-2 text-sm").Child("Key Personnel")))
                .Child(w.Div().Key("narrow").Class("flex w-40 gap-2")
                    .Child(w.Div().Key("rigid").Class("flex-1 whitespace-nowrap text-sm").Child("Một nhãn rất dài không xuống dòng"))
                    .Child(w.Div().Key("soft").Class("flex-1 min-w-0 truncate text-sm").Child("Một nhãn rất dài bị cắt")))));
            var t2 = t.Get("t2").Screen; var t3 = t.Get("t3").Screen;
            float need = t.Window.Text.Measure("Past Performance", TextStyle.Default with { FontSize = 14, LineHeight = 20 }) + 16;
            t.Assert.True(t2.Width >= need - 0.5f, $"tab dài không bị co dưới nội dung: {t2.Width:0.#} ≥ {need:0.#}");
            t.Assert.True(t2.Right <= t3.X + 0.5f, "các tab không đè lên nhau");
            t.Assert.True(t.Get("rigid").Screen.Width > 150, "phần tử nowrap không có min-w-0 giữ bề rộng nội dung (tràn như CSS)");
            t.Assert.True(t.Get("soft").Screen.Width < 20, "min-w-0 cho phép co hết phần còn lại");
        });
        suite.Add("core/scroll/scrollbar-hover", t =>
        {
            // Như ScrollArea type="hover" của shadcn: thanh cuộn chỉ hiện khi con trỏ ở trên vùng cuộn, ẩn 600ms sau khi rời.
            t.Open(new Fx(w =>
            {
                var list = w.Div().Key("area").Class("h-24 w-48 overflow-y-auto scrollbar-hover scrollbar-w-2.5 scrollbar-thumb-red-600 bg-white");
                for (int i = 0; i < 12; i++) list.Child(w.Div().Class("h-8 px-2 text-sm").Child("Dòng " + i));
                return Fx.Page(w).Child(list);
            }));
            var area = t.Get("area").Screen;
            // Con trượt rộng 10 - 1 - 2 = 7px, cách mép phải 1px: lấy mẫu giữa con trượt, ở đầu rãnh.
            var probe = new Point(area.Right - 1 - 3.5f, area.Y + 6);
            bool Red() { var c = t.PixelAt(probe); return c.R > 0.7f && c.G < 0.35f && c.B < 0.35f; }
            t.Assert.True(!Red(), "chưa rê chuột: thanh cuộn ẩn");
            t.Hover(new Point(area.X + 20, area.Y + 20));
            t.Assert.True(Red(), "con trỏ trên vùng cuộn: thanh cuộn hiện, màu scrollbar-thumb-*");
            t.Hover(new Point(area.Right + 80, area.Bottom + 80));
            t.Advance(0.3);
            t.Assert.True(Red(), "rời ra 300ms: vẫn còn hiện (trễ ẩn 600ms)");
            t.Advance(0.5);
            t.Assert.True(!Red(), "rời ra hơn 600ms: thanh cuộn ẩn");
            t.Assert.True(t.Settle(), "sau khi ẩn thì trang đứng yên, không vẽ lại mãi");
        });
        suite.Add("core/regression/image-registry-prunes", t =>
        {
            byte[] px = new byte[4 * 4 * 4];
            for (int i = 0; i < 300; i++) ImageSource.FromRgba(4, 4, px); // không giữ: mục của chúng phải được dọn dần
            GC.Collect(); GC.WaitForPendingFinalizers();
            var kept = ImageSource.FromRgba(4, 4, px);
            for (int i = 0; i < 300; i++) ImageSource.FromRgba(4, 4, px);
            t.Assert.True(ReferenceEquals(ImageSource.Find(kept.Id), kept), "ảnh còn sống vẫn tra được theo Id sau các lượt dọn");
            t.Assert.True(ImageSource.Find(int.MaxValue) is null, "Id không tồn tại trả về null");
            GC.KeepAlive(kept);
        });
    }
}
