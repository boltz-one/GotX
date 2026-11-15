using GotX;
using GotX.Test;
using GotX.Widgets;

/// <summary>TextInput: undo/redo, chọn từ / dòng bằng nhấp đúp / ba, nhấp nháy con trỏ, cuộn ngang, kéo chọn, clipboard.</summary>
static partial class TextInputEditTests
{
    const string Box = "h-8 w-40 px-3 border border-white bg-white text-slate-900";
    static readonly Point InField = new(40, 40);

    sealed class Host(string text, bool multiline, bool? blink = null, string boxClass = Box) : View
    {
        public TextInput Field = null!;
        protected override void OnCreated() =>
            Field = App.New(new TextInput { Multiline = multiline, Text = text, BoxClass = boxClass, Blink = blink });
        public override Div Render(Window w) => Fx.Page(w).Child(w.Div().Key("field").Child(w.View(Field)));
    }

    static int LastInk(UiTest t, int x0, int x1, int y0, int y1)
    {
        int last = -1;
        for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
                if (t.PixelAt(new Point(x + 0.5f, y + 0.5f)).R < 0.6f) { last = x; break; }
        return last;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/text-input/undo-groups-typing-runs-and-redo", t =>
        {
            var h = t.Open(new Host("", false));
            t.Click("field");
            t.Type("a"); t.Type("b"); t.Type("c");
            t.Press(Key.Z, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "", "Ctrl+Z hoàn tác cả chuỗi gõ liền nhau trong một bước");
            t.Press(Key.Y, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "abc", "Ctrl+Y làm lại");
            t.Press(Key.Z, Modifiers.Control);
            t.Press(Key.Z, Modifiers.Control | Modifiers.Shift);
            t.Assert.Equal(h.Field.Text, "abc", "Ctrl+Shift+Z cũng làm lại");
            t.Type("d");
            t.Press(Key.Left);
            t.Type("X"); // di chuyển con trỏ ngắt nhóm
            t.Press(Key.Z, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "abcd", "di chuyển con trỏ ngắt nhóm gõ");
            t.Press(Key.Z, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "abc", "bước trước");
            t.Type("Q"); // sửa mới xoá nhánh redo
            t.Assert.True(!h.Field.CanRedo, "gõ mới xoá lịch sử redo");
        });

        suite.Add("core/text-input/undo-backspace-selection-and-paste", t =>
        {
            var h = t.Open(new Host("hello world", false));
            t.Click("field");
            t.Press(Key.End);
            t.Press(Key.Backspace); t.Press(Key.Backspace); t.Press(Key.Backspace);
            t.Assert.Equal(h.Field.Text, "hello wo", "xoá 3 lần");
            t.Press(Key.Z, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "hello world", "một lần Ctrl+Z hoàn tác cả chuỗi Backspace");
            t.Assert.Equal(h.Field.Caret, 11, "con trỏ về chỗ cũ");
            t.Press(Key.A, Modifiers.Control);
            t.Type("new");
            t.Press(Key.Z, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "hello world", "thay vùng chọn là một bước riêng");
            t.App.Clipboard.SetText("PASTE");
            t.Press(Key.V, Modifiers.Control);
            t.Press(Key.V, Modifiers.Control);
            t.Press(Key.Z, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "hello worldPASTE", "mỗi lần dán là một bước");
        });

        suite.Add("core/text-input/undo-history-is-bounded", t =>
        {
            var h = t.Open(new Host("", false));
            t.Click("field");
            for (int i = 0; i < 260; i++) { h.Field.Insert("x\n"); } // xuống dòng ngắt nhóm: mỗi lần một mục
            int steps = 0;
            while (h.Field.CanUndo && steps < 1000) { h.Field.Undo(); steps++; }
            t.Assert.True(steps <= 200, $"lịch sử bị chặn trên (đã hoàn tác {steps} bước)");
            h.Field.Text = "reset";
            t.Assert.True(!h.Field.CanUndo, "đặt Text từ code xoá lịch sử");
        });

        suite.Add("core/text-input/double-click-selects-word-triple-selects-all", t =>
        {
            var h = t.Open(new Host("hello big-world", false));
            t.Click(new Point(50, 40));
            t.Click(new Point(50, 40));
            t.Assert.Equal(h.Field.Anchor, 0, "nhấp đúp: đầu từ");
            t.Assert.Equal(h.Field.Caret, 5, "nhấp đúp: cuối từ \"hello\"");
            t.Click(new Point(50, 40));
            t.Assert.True(h.Field.Anchor == 0 && h.Field.Caret == 15, "nhấp ba trên ô một dòng chọn tất cả");
            t.Advance(1);
            t.Click(new Point(50, 40));
            t.Assert.True(!h.Field.HasSelection, "nhấp lại sau khoảng nghỉ: nhấp đơn, bỏ chọn");
        });

        suite.Add("core/text-input/triple-click-selects-line-multiline", t =>
        {
            var h = t.Open(new Host("one\ntwo words\nthree", true, boxClass: "w-40 px-3 py-1 border border-white bg-white text-slate-900"));
            var line2 = new Point(50, 24 + 4 + 1 + 24); // dòng thứ hai (mỗi dòng ~24px)
            t.Click(line2); t.Click(line2); t.Click(line2);
            t.Assert.True(h.Field.Anchor == 4 && h.Field.Caret == 13, $"chọn đúng dòng hai ({h.Field.Anchor}..{h.Field.Caret})");
        });

        suite.Add("core/text-input/double-click-drag-extends-by-word", t =>
        {
            var h = t.Open(new Host("aaa bbb ccc", false));
            t.Click(new Point(42, 40));
            t.Drag(new Point(42, 40), new Point(84, 40));
            t.Assert.True(h.Field.Anchor == 0 && h.Field.Caret >= 7, $"kéo sau nhấp đúp chọn theo từ ({h.Field.Anchor}..{h.Field.Caret})");
            t.Assert.True(h.Field.Caret == 7 || h.Field.Caret == 11 || h.Field.Caret == 8, "caret ở ranh giới từ");
        });

        suite.Add("core/text-input/caret-blinks-only-when-enabled", t =>
        {
            var h = t.Open(new Host("abc", false, blink: true));
            t.Click("field");
            t.Assert.True(h.Field.CaretVisible, "vừa focus: con trỏ hiện");
            t.Advance(0.6);
            t.Assert.True(!h.Field.CaretVisible, "sau một nhịp: tắt");
            t.Advance(0.55);
            t.Assert.True(h.Field.CaretVisible, "nhịp tiếp: hiện");
            t.Advance(0.6);
            t.Type("z");
            t.Assert.True(h.Field.CaretVisible, "gõ chữ đặt lại pha hiện");
            t.Advance(0.3);
            t.Assert.True(h.Field.CaretVisible, "chưa đủ nhịp sau khi đặt lại");
            t.Click(new Point(600, 600));
            t.Assert.True(h.Field.CaretVisible, "mất focus: dừng ở trạng thái hiện");
            int pending = t.App.PendingTimers;
            t.Advance(2);
            t.Assert.Equal(t.App.PendingTimers, pending, "không còn timer nhấp nháy sau khi mất focus");
            t.Assert.True(h.Field.CaretVisible, "mất focus: không đổi pha nữa");
        });

        suite.Add("core/text-input/caret-static-in-headless-by-default", t =>
        {
            var h = t.Open(new Host("abc", false));
            t.Click("field");
            t.Advance(3);
            t.Assert.True(h.Field.CaretVisible, "mặc định headless không nhấp nháy");
            t.Perf.ExpectIdle();
        });

        suite.Add("core/text-input/single-line-scrolls-to-keep-caret-visible", t =>
        {
            var h = t.Open(new Host("abcdefghijklmnopqrstuvwxyz0123456789 ABCDEFGHIJKLMNOPQRSTUVWXYZ", false));
            t.Click("field");
            t.Press(Key.End);
            int last = LastInk(t, 38, 190, 30, 50);
            t.Assert.True(last >= 150 && last <= 172, $"cuối chữ hiện trong khung (mực tới {last})");
            t.Press(Key.Home);
            int first = -1;
            for (int x = 36; x < 60 && first < 0; x++)
                for (int y = 30; y < 50; y++)
                    if (t.PixelAt(new Point(x + 0.5f, y + 0.5f)).R < 0.6f) { first = x; break; }
            t.Assert.True(first >= 36 && first < 46, $"Home cuộn về đầu chữ (mực từ {first})");
            for (int i = 0; i < 12; i++) t.Press(Key.Right);
            t.Type("!");
            t.Assert.Equal(h.Field.Text[..13], "abcdefghijkl!", "gõ giữa chữ dài vẫn đúng chỗ");
            // NoLint chưa dùng được: LayoutEngine.MeasureInput vẫn xuống dòng ô một dòng (cần Wrap với NaN khi !Multiline)
        });

        suite.Add("core/text-input/drag-select-past-edges", t =>
        {
            var h = t.Open(new Host("line one\nline two\nline three", true, boxClass: "w-40 px-3 py-1 border border-white bg-white text-slate-900"));
            t.Drag(new Point(60, 56), new Point(60, 400));
            t.Assert.Equal(h.Field.Caret, h.Field.Text.Length, "kéo xuống dưới ô: chọn tới cuối văn bản");
            t.Advance(1);
            t.Drag(new Point(60, 56), new Point(60, 2));
            t.Assert.Equal(h.Field.Caret, 0, "kéo lên trên ô: chọn tới đầu văn bản");
        });
        RegisterMore(suite);
    }
}
