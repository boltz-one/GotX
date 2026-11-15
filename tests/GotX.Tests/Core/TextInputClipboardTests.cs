using GotX;
using GotX.Test;
using GotX.Widgets;

/// <summary>TextInput: clipboard và IME (phần của TextInputEditTests).</summary>
static partial class TextInputEditTests
{
    static void RegisterMore(TestSuite suite)
    {
        suite.Add("core/text-input/clipboard-edge-cases", t =>
        {
            var h = t.Open(new Host("abc", false));
            t.Click("field");
            t.App.Clipboard.SetText("keep");
            t.Press(Key.C, Modifiers.Control);
            t.Press(Key.X, Modifiers.Control);
            t.Assert.Equal(t.App.Clipboard.GetText() ?? "", "keep", "copy/cut không có vùng chọn không đụng clipboard");
            t.Assert.Equal(h.Field.Text, "abc", "cut không có vùng chọn không xoá gì");
            t.Assert.True(!h.Field.CanUndo, "cut rỗng không tạo mục hoàn tác");
            t.App.Clipboard.SetText("x\r\ny\nz\n");
            t.Press(Key.End);
            t.Press(Key.V, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "abcx y z", "dán nhiều dòng vào ô một dòng: xuống dòng giữa thành khoảng trắng, cuối bị bỏ");
            t.App.Clipboard.SetText("");
            t.Press(Key.V, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "abcx y z", "clipboard rỗng: không đổi");
            t.Press(Key.Z, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "abc", "dán nhiều dòng hoàn tác một bước");
        });

        suite.Add("core/text-input/ime-composition-blocks-undo-and-commit-replaces", t =>
        {
            var h = t.Open(new Host("ab", false));
            t.Click("field");
            t.Press(Key.End);
            t.Type("c");
            h.Field.SetComposition("ngu");
            t.Frames();
            t.Press(Key.Z, Modifiers.Control);
            t.Assert.Equal(h.Field.Text, "abc", "đang soạn IME: Ctrl+Z bị chặn");
            t.Assert.Equal(h.Field.Composition, "ngu", "chuỗi soạn còn nguyên");
            t.Type("ngư");
            t.Assert.True(h.Field.Composition.Length == 0 && h.Field.Text == "abcngư", "commit thay chuỗi đang soạn");
        });
    }
}
