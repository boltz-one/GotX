using GotX;
using GotX.Test;

/// <summary>Gộp variant trạng thái qua nhiều lần gọi .Class(), tự cuộn phần tử được focus vào khung nhìn, icon nhiều phần.</summary>
static class StateAndScrollTests
{
    /// <summary>Danh sách cuộn 30 dòng focus được, mỗi dòng cao 32 px trong khung cao 128 px.</summary>
    sealed class LongList : View
    {
        public override Div Render(Window w)
        {
            var list = w.Div().Key("list").Class("w-48 h-32 overflow-y-auto border border-slate-300");
            for (int i = 0; i < 30; i++)
                list.Child(w.Div().Key("item-" + i).Focusable().Anchor("anchor-" + i).Class("h-8 px-2 flex items-center text-sm focus:bg-sky-100").Child("Mục " + i));
            return Fx.Page(w).Child(list);
        }
    }

    static float ScrollY(UiTest t) => t.Window.GetScroll("list")!.Value.Offset.Y;

    public static void Register(TestSuite suite)
    {
        suite.Add("core/class/multiple-calls-merge-state-variants", t =>
        {
            t.Open(new Fx(w => Fx.Page(w).Child(w.Div().Key("both").Focusable()
                .Class("size-10 bg-slate-200 hover:bg-rose-500").Class("focus:ring-2 focus:ring-sky-500"))));
            var box = t.Box("both");
            var ringAt = new Point(box.X - 1.5f, box.Y + 20);
            t.Assert.Pixel(ringAt, Rgba.White, 4);
            t.Press(Key.Tab);
            t.Assert.True(Pixels.Delta(t.PixelAt(ringAt), Rgba.White) > 60, $"focus:ring-2 của lần gọi .Class() thứ hai còn hiệu lực ({Fmt.C(t.PixelAt(ringAt))})");
            t.Hover("both");
            var fill = t.PixelAt(Fmt.Center(box));
            t.Assert.True(fill is { R: > 0.8f, G: < 0.4f }, $"hover:bg-rose-500 của lần gọi đầu không bị lần sau thay mất ({Fmt.C(fill)})");
            t.Assert.True(Pixels.Delta(t.PixelAt(ringAt), Rgba.White) > 60, "cả hai variant cùng bật một lúc");
            t.Assert.Equal(t.Get("both").Classes, "size-10 bg-slate-200 hover:bg-rose-500 focus:ring-2 focus:ring-sky-500", "Inspect nối chuỗi class của các lần gọi");
        });

        suite.Add("core/scroll/tab-scrolls-focused-item-into-view", t =>
        {
            t.Open(new LongList());
            var list = t.Box("list");
            for (int i = 0; i < 12; i++)
            {
                t.Press(Key.Tab);
                t.Settle(60);
                t.Assert.Focused("item-" + i);
                var item = t.Get("item-" + i);
                t.Assert.True(item.Screen.Y >= list.Y - 0.5f && item.Screen.Bottom <= list.Bottom + 0.5f,
                    $"Tab tới mục {i}: mục nằm trọn trong khung cuộn ({Fmt.B(item.Screen)} trong {Fmt.B(list)}), cuộn {Fmt.N(ScrollY(t))}");
            }
            t.Assert.True(ScrollY(t) > 200, $"vị trí cuộn đã đi theo focus ({Fmt.N(ScrollY(t))})");
            for (int i = 0; i < 11; i++) { t.Press(Key.Tab, Modifiers.Shift); t.Settle(60); }
            t.Assert.Focused("item-0");
            t.Assert.Near(ScrollY(t), 0, 0.5f, "Shift+Tab về đầu thì cuộn ngược lại");
        });

        suite.Add("core/scroll/focuskey-and-scroll-into-view", t =>
        {
            t.Open(new LongList());
            var list = t.Box("list");
            t.Window.FocusKey("item-20");
            t.Frames();
            t.Settle(60);
            t.Assert.Visible("item-20");
            t.Assert.Inside("item-20", "list");
            t.Window.ScrollIntoView("anchor-3");
            t.Frames();
            t.Settle(60);
            var item = t.Get("item-3");
            t.Assert.True(item.Screen.Y >= list.Y - 0.5f && item.Screen.Bottom <= list.Bottom + 0.5f, $"ScrollIntoView(anchor) đưa mục 3 vào khung ({Fmt.B(item.Screen)})");
            t.Assert.Focused("item-20"); // cuộn bằng code không đổi focus
        });

        suite.Add("core/vector/icon-parts-start-from-origin", t =>
        {
            // Dấu X của Lucide: phần hai bắt đầu bằng "m" tương đối. Mỗi phần là một <path> riêng nên "m6 6" tính từ gốc (0,0),
            // không nối tiếp từ điểm cuối (6,18) của phần một – hai nét phải cắt nhau ở tâm.
            t.Open(new Fx(w => w.Div().Class("size-full bg-white text-black").Child(w.Path(VectorPath.Icon("M18 6 6 18", "m6 6 12 12")).Key("x").Class("size-24"))), new Size(120, 120));
            float Ink(float x, float y) => 1 - t.PixelAt(new Point(x * 4, y * 4)).R;
            t.Assert.True(Ink(12, 12) > 0.9f, "hai nét cắt nhau ở tâm");
            t.Assert.True(Ink(7, 7) > 0.9f && Ink(17, 17) > 0.9f, "nét thứ hai đi từ (6,6) tới (18,18)");
            t.Assert.True(Ink(17, 7) > 0.9f && Ink(7, 17) > 0.9f, "nét thứ nhất đi từ (18,6) tới (6,18)");
            t.Assert.True(Ink(12, 21) < 0.1f && Ink(18, 22) < 0.1f && Ink(12, 6) < 0.1f, "không có nét nào lạc xuống dưới (lỗi cũ: nét hai bắt đầu từ (12,24))");
        });
    }
}
