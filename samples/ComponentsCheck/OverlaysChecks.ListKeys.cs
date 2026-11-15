using GotX;
using GotX.Components;

/// <summary>Kiểm tra Overlays phần bàn phím của Select / Combobox / Command: dòng đang chọn theo phím và chuột, focus trả về nút.</summary>
static partial class OverlaysChecks
{
    static void ListKeys(CheckHarness h)
    {
        Select select = null!;
        Combobox combo = null!;
        Command cmd = null!;
        var cmdPicked = new List<string>();
        var w = Host(h, app =>
        {
            select = app.New(new Select { Items = ["Táo", "Chuối", "Việt quất", "Nho"], Placeholder = "Chọn quả" });
            combo = app.New(new Combobox { Items = ["Next.js", "SvelteKit", "Nuxt.js", "Remix", "Astro"], Placeholder = "Chọn framework…" });
            cmd = app.New(new Command
            {
                PanelClass = "w-80 border border-border",
                Groups =
                [
                    new("Gợi ý", [new("Lịch", Key: "ck-cal"), new("Tìm emoji", Key: "ck-emoji"), new("Máy tính", Disabled: true, Key: "ck-calc")]),
                    new("Cài đặt", [new("Hồ sơ", "Ctrl+P", Key: "ck-profile"), new("Cài đặt", "Ctrl+S", Key: "ck-settings")]),
                ],
            });
            app.Subscribe<Command.ItemSelected>(cmd, e => cmdPicked.Add($"{e.Group}.{e.Index}:{e.Item.Text}"));
        }, w => w.Div().Class("flex items-start gap-8").Child(w.View(select)).Child(w.View(combo)).Child(w.View(cmd)));
        var trail = new List<int>();

        // Select
        h.Press(w, Key.Tab);
        h.Check("select: nút nhận focus bằng Tab", w.IsFocused(select.TriggerKey) && w.FocusVisible);
        h.Press(w, Key.Down);
        h.Check("select: ↓ trên nút → mở; danh sách giữ focus; chưa chọn gì nên chưa mục nào được tô",
            select.IsOpen && select.ActiveIndex == -1 && w.HasElementFocus && !w.IsFocused(select.TriggerKey));
        foreach (var key in (Key[])[Key.Down, Key.Down, Key.End, Key.Down, Key.Home, Key.Up])
        {
            h.Press(w, key);
            trail.Add(select.ActiveIndex);
        }
        h.Check($"select: ↑/↓/Home/End dời mục đang tô, dừng ở hai đầu ({string.Join(",", trail)})", trail is [0, 1, 3, 3, 0, 0]);
        h.Press(w, Key.Tab);
        h.Check("select: Tab không rời danh sách đang mở", select.IsOpen && !w.IsFocused(select.TriggerKey));
        h.Move(w, CheckHarness.Center(h.Box(w, select.ItemKey(2))));
        bool hovered = select.ActiveIndex == 2;
        h.Press(w, Key.Up);
        h.Check("select: rê chuột đổi mục đang tô, phím đi tiếp từ đó (một vệt sáng)", hovered && select.ActiveIndex == 1);
        h.Save(w, "overlay-select-keys.png");
        h.Press(w, Key.Enter);
        h.Check($"select: Enter chọn mục đang tô, đóng, focus về nút ({select.Value})",
            select.Value == "Chuối" && !select.IsOpen && w.Overlays.Count == 0 && w.IsFocused(select.TriggerKey));
        h.Press(w, Key.Enter);
        bool reopened = select.IsOpen && select.ActiveIndex == 1;
        h.Press(w, Key.Down); h.Press(w, Key.Escape);
        h.Check("select: mở lại thì mục đang chọn được tô; Escape đóng, giữ giá trị, focus về nút",
            reopened && !select.IsOpen && select.Value == "Chuối" && w.IsFocused(select.TriggerKey));
        h.Press(w, Key.Up); h.Press(w, Key.End); h.Press(w, Key.Space);
        h.Check("select: ↑ cũng mở; Space chọn", select.Value == "Nho" && !select.IsOpen);

        // Combobox: focus ở ô gõ, ↑/↓/Enter điều khiển danh sách
        h.Press(w, Key.Tab);
        h.Check("combobox: nút là điểm dừng Tab kế tiếp", w.IsFocused(combo.TriggerKey));
        h.Press(w, Key.Enter);
        h.Check("combobox: Enter mở, focus vào ô gõ, dòng đầu được chọn sẵn", combo.IsOpen && w.Focused == combo.Panel.Input && combo.Panel.ActiveIndex == 0);
        h.Type(w, "s");
        trail.Clear();
        foreach (var key in (Key[])[Key.Down, Key.Down, Key.Down, Key.Down, Key.Up])
        {
            h.Press(w, key);
            trail.Add(combo.Panel.ActiveIndex);
        }
        h.Check($"combobox: gõ vẫn vào ô lọc; ↑/↓ chỉ đi qua các dòng còn khớp, dừng ở cuối ({string.Join(",", trail)})",
            combo.Panel.Filter == "s" && w.Focused == combo.Panel.Input && trail is [1, 2, 4, 4, 2]);
        h.Press(w, Key.Enter);
        h.Check($"combobox: Enter chọn dòng đang tô, đóng, focus về nút ({combo.Value})",
            combo.Value == "Nuxt.js" && !combo.IsOpen && w.Overlays.Count == 0 && w.IsFocused(combo.TriggerKey));
        h.Press(w, Key.Down);
        h.Press(w, Key.Escape);
        h.Check("combobox: ↓ trên nút cũng mở; Escape đóng và trả focus về nút", !combo.IsOpen && w.IsFocused(combo.TriggerKey) && combo.Value == "Nuxt.js");

        // Command nhúng thẳng
        w.Focus(cmd.Input);
        h.Frames(w);
        trail.Clear();
        foreach (var key in (Key[])[Key.Down, Key.Down, Key.Down, Key.Down, Key.Up, Key.Up, Key.Up, Key.Up])
        {
            h.Press(w, key);
            trail.Add(cmd.ActiveIndex);
        }
        h.Check($"command: dòng đầu chọn sẵn; ↑/↓ bỏ qua dòng tắt, không quay vòng ({string.Join(",", trail)})", trail is [1, 3, 4, 4, 3, 1, 0, 0]);
        h.Move(w, CheckHarness.Center(h.Box(w, "ck-profile")));
        bool overProfile = cmd.ActiveIndex == 3;
        h.Move(w, CheckHarness.Center(h.Box(w, "ck-calc")));
        h.Check("command: rê chuột đổi dòng đang chọn; dòng tắt không nhận, dòng cũ vẫn giữ", overProfile && cmd.ActiveIndex == 3);
        h.Press(w, Key.Down);
        var (lit, dim) = (RowTint(h, w, "ck-settings"), RowTint(h, w, "ck-profile"));
        h.Check($"command: chỉ một dòng sáng – dòng theo phím, không phải dòng dưới chuột cũ (sáng {lit}, tắt {dim})", cmd.ActiveIndex == 4 && lit > 8 && dim <= 2);
        h.Save(w, "overlay-command-keys.png");
        h.Press(w, Key.Enter);
        h.Check($"command: Enter chọn dòng đang chọn (không phải dòng đầu), kèm nhóm và chỉ số trong nhóm ({string.Join(",", cmdPicked)})",
            cmdPicked is ["1.1:Cài đặt"]);
        h.Type(w, "hồ");
        h.Check("command: lọc lại thì dòng đang chọn về dòng khớp đầu tiên", cmd.ActiveIndex == 3 && cmd.ActiveItem?.Text == "Hồ sơ");
        h.Type(w, "zz");
        h.Press(w, Key.Enter);
        h.Check("command: không dòng nào khớp thì không có dòng chọn, Enter không làm gì", cmd.ActiveIndex == -1 && cmdPicked.Count == 1);
    }

    /// <summary>Danh sách dài hơn khung (max-h-72): mở ra thì mục đang chọn đã được cuộn vào tầm nhìn, phím đi tiếp vẫn giữ nó trong khung.</summary>
    static void LongSelect(CheckHarness h)
    {
        Select select = null!;
        var w = Host(h, app => select = app.New(new Select { Items = Enumerable.Range(1, 30).Select(static i => "Mục " + i).ToArray(), SelectedIndex = 25 }),
            w => w.View(select));
        bool InView(int index)
        {
            var list = w.Overlays[0].ContentBounds;
            return w.AnchorBounds(select.ItemKey(index)) is { } row && row.Y >= list.Y - 0.5f && row.Bottom <= list.Bottom + 0.5f;
        }
        h.Click(w, select.TriggerKey);
        h.Wait(w, 0.5); // cuộn tới mục đang chọn có hiệu ứng
        h.Check("select dài: mở ra thì mục đang chọn (26/30) nằm trong khung, mục đầu đã cuộn khuất", select.IsOpen && select.ActiveIndex == 25 && InView(25) && !InView(0));
        h.Save(w, "overlay-select-long.png");
        h.Press(w, Key.Home);
        h.Wait(w, 0.4);
        h.Check("select dài: Home cuộn về mục đầu", select.ActiveIndex == 0 && InView(0) && !InView(25));
        h.Press(w, Key.Escape);
    }
}
