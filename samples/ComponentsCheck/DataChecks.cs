using GotX;
using GotX.Components;

/// <summary>Kiểm tra nhóm "Dữ liệu": Typography, DatePicker; gọi tiếp DataTable và SidebarNav (file riêng).</summary>
static class DataChecks
{
    // Tấm lịch không có đệm ngoài; bố cục bên trong như FormsPickerChecks: p-3, tiêu đề h-8, gap-2, hàng thứ h-8, gap-2, ô 32px.
    static Point Cell(Bounds cal, int index) =>
        new(cal.X + 12 + (index % 7) * 32 + 16, cal.Y + 12 + 32 + 8 + 32 + 8 + (index / 7) * 32 + 16);

    public static void Run(CheckHarness h)
    {
        Typography(h);
        DatePickerChecks(h);
        DataTableChecks.Run(h);
        DataSidebarChecks.Run(h);
    }

    static void Typography(CheckHarness h)
    {
        var size = h.Size;
        h.Size = new Size(1000, 1400);
        var w = h.Open(app => app.New(new DataPage()));
        var h1 = h.Box(w, "d-h1");
        var h2 = h.Box(w, "d-h2");
        h.Check($"typography: H1 cỡ 4xl cao một dòng 40px ({h1.Height})", Math.Abs(h1.Height - 40) < 0.5f);
        var line = h.Pixel(w, new Point(h2.X + 40, h2.Bottom - 0.5f));
        h.Check($"typography: H2 có đường kẻ dưới màu border ({line})", line.R is > 215 and < 240);
        var quote = h.Box(w, "d-quote");
        var bar = h.Pixel(w, new Point(quote.X + 1, quote.Y + quote.Height / 2));
        var beside = h.Pixel(w, new Point(quote.X + 8, quote.Y + quote.Height / 2));
        h.Check($"typography: blockquote có gạch dọc 2px bên trái ({bar} / {beside})", bar.R < 240 && beside.R == 255);
        var code = h.Box(w, "d-code");
        var codeBg = h.Pixel(w, new Point(code.X + 2, code.Y + code.Height / 2));
        h.Check($"typography: inline code có nền muted ({codeBg})", codeBg.R is > 235 and < 250);
        var list = h.Box(w, "d-list");
        var bullet = h.Pixel(w, new Point(list.X + 8 + 3, list.Y + 11 + 3));
        h.Check($"typography: mục danh sách có chấm tròn ({bullet})", bullet.R < 60);
        h.Check($"typography: các khối của Prose cách nhau 24px ({h2.Y - h.Box(w, "d-h1").Bottom})",
            h2.Y > h1.Bottom + 24); // giữa H1 và H2 còn đoạn Lead
        h.Size = size;
    }

    static void DatePickerChecks(CheckHarness h)
    {
        DatePicker picker = null!;
        int changes = 0, opens = 0;
        var w = h.Open(app =>
        {
            picker = app.New(new DatePicker());
            picker.Calendar.Today = new DateOnly(2026, 11, 15);
            picker.Calendar.Month = new DateOnly(2026, 11, 1);
            app.Subscribe<DatePicker.ValueChanged>(picker, _ => changes++);
            app.Subscribe<DatePicker.OpenChanged>(picker, e => { if (e.Open) opens++; });
            return app.New(new FragmentView(w => w.Div().Class("size-full bg-background p-6 text-foreground").Child(w.View(picker))));
        });
        var trigger = h.Box(w, picker.TriggerKey);
        h.Check($"date-picker: nút rộng 240px, cao 32px ({trigger.Width}×{trigger.Height})", trigger.Width == 240 && trigger.Height == 32);

        h.Click(w, picker.TriggerKey);
        var panel = picker.Overlay?.ContentBounds ?? default;
        h.Check($"date-picker: bấm nút mở lịch ngay dưới nút, thẳng mép trái ({panel})",
            picker.IsOpen && w.Overlays.Count == 1 && opens == 1 && Math.Abs(panel.X - trigger.X) < 1 && Math.Abs(panel.Y - trigger.Bottom - 4) < 1);

        // 1/11/2026 là Chủ nhật → ô đầu là thứ Hai 26/10; 20/11 ở ô 25.
        h.Click(w, Cell(panel, 25));
        h.Check($"date-picker: bấm ngày thì chọn, đóng và phát ValueChanged một lần ({picker.Value}, {changes})",
            picker.Value == new DateOnly(2026, 11, 20) && !picker.IsOpen && changes == 1);
        h.Check("date-picker: chọn xong focus trở về nút", w.IsFocused(picker.TriggerKey));

        h.Press(w, Key.Down);
        h.Check("date-picker: ↓ trên nút mở lịch và tấm lịch nhận focus", picker.IsOpen && w.IsFocused(picker.ContentKey));
        h.Press(w, Key.Escape);
        h.Check($"date-picker: Escape đóng, giữ nguyên giá trị, focus về nút ({picker.Value})",
            !picker.IsOpen && picker.Value == new DateOnly(2026, 11, 20) && w.IsFocused(picker.TriggerKey) && changes == 1);

        // Enter mở; Tab qua hai nút đổi tháng vào lưới (rơi vào ngày đang chọn), → sang ngày kế, Enter chọn.
        h.Press(w, Key.Enter);
        h.Check("date-picker: Enter trên nút mở lịch", picker.IsOpen);
        h.Press(w, Key.Tab);
        h.Press(w, Key.Tab);
        h.Press(w, Key.Tab);
        h.Press(w, Key.Right);
        h.Press(w, Key.Enter);
        h.Check($"date-picker: chọn bằng bàn phím (Tab vào lưới, →, Enter) được 21/11 và đóng ({picker.Value})",
            picker.Value == new DateOnly(2026, 11, 21) && !picker.IsOpen && w.IsFocused(picker.TriggerKey));

        picker.Value = new DateOnly(2027, 1, 5);
        h.Frames(w);
        h.Click(w, picker.TriggerKey);
        h.Check($"date-picker: mở ra ở tháng của ngày đang chọn ({picker.Calendar.Month})", picker.IsOpen && picker.Calendar.Month == new DateOnly(2027, 1, 1));
        h.Click(w, new Point(700, 600));
        h.Check("date-picker: bấm ra ngoài thì đóng", !picker.IsOpen);

        int before = changes;
        picker.Value = null;
        h.Frames(w);
        h.Check($"date-picker: gán null phát ValueChanged ({changes - before})", picker.Value is null && changes == before + 1);

        picker.Disabled = true;
        h.Frames(w);
        h.Click(w, picker.TriggerKey);
        h.Check("date-picker: Disabled thì không mở", !picker.IsOpen);
    }
}
