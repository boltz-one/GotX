using GotX;
using GotX.Components;
using GotX.Raster;

/// <summary>
/// Kiểm tra phần mới của nhóm Display trên <see cref="DisplayInteractionProbe"/>: icon vector vẽ đúng chỗ, ảnh avatar
/// được cắt tròn, nút nhận focus/phím và hiện vòng focus. Mọi phép đo là pixel của frame vẽ bằng renderer CPU.
/// </summary>
static class DisplayInteractionChecks
{
    static int Lum(uint v) => (int)(((v >> 16) & 255) + ((v >> 8) & 255) + (v & 255)) / 3;
    static int Lum((int R, int G, int B) c) => (c.R + c.G + c.B) / 3;

    /// <summary>Pixel tại toạ độ tỉ lệ (0..1) trong hộp có mực (sẫm hơn nền trắng rõ rệt).</summary>
    static bool Ink(Canvas c, Bounds b, float fx, float fy) =>
        Lum(c.Pixels[(int)(b.Y + b.Height * fy) * c.Width + (int)(b.X + b.Width * fx)]) < 140;

    /// <summary>Số pixel có mực trong hộp, khung bao của mực (theo tỉ lệ hộp) và số pixel mực nằm trên viền ngoài 1px.</summary>
    static (int Count, float CenterX, float CenterY, int OnEdge) Measure(Canvas c, Bounds b)
    {
        int x0 = (int)b.X, y0 = (int)b.Y, x1 = (int)b.Right, y1 = (int)b.Bottom;
        int count = 0, edge = 0, minX = x1, maxX = x0, minY = y1, maxY = y0;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                if (Lum(c.Pixels[y * c.Width + x]) >= 140) continue;
                count++;
                if (x == x0 || x == x1 - 1 || y == y0 || y == y1 - 1) edge++;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
        if (count == 0) return (0, 0, 0, 0);
        return (count, ((minX + maxX + 1) / 2f - x0) / b.Width, ((minY + maxY + 1) / 2f - y0) / b.Height, edge);
    }

    public static void Run(CheckHarness h)
    {
        DisplayInteractionProbe p = null!;
        var w = h.Open(app => p = app.New(new DisplayInteractionProbe()));
        h.Save(w, "display-icons.png");
        Icons(h, w);
        Images(h, w);
        Buttons(h, w, p);
        // Trang Display cao hơn 2400px mà Program.cs chụp; lưu thêm bản đủ chiều cao để soát bằng mắt.
        var saved = h.Size;
        h.Size = new Size(1000, 3300);
        h.Save(h.Open(app => app.New(new DisplayPage())), "page-display-full-light.png");
        h.Save(h.Open(app => app.New(new DisplayPage()), dark: true), "page-display-full-dark.png");
        h.Size = saved;
    }

    static void Icons(CheckHarness h, Window w)
    {
        var canvas = h.Render(w);
        var empty = new List<string>();
        var clipped = new List<string>();
        var offCentre = new List<string>();
        for (int i = 0; i < Icon.All.Count; i++)
        {
            var m = Measure(canvas, h.Box(w, DisplayInteractionProbe.IconKeys[i]));
            string name = Icon.All[i].Name;
            // Nét mảnh nhất (Minus) ở 48px cũng phủ hơn 100 pixel; hình đặc nhất (Dot) chưa tới 30% ô.
            if (m.Count < 60 || m.Count > 48 * 48 * 0.5f) empty.Add($"{name}={m.Count}");
            // Lucide chừa lề 1 đơn vị viewBox (2px ở cỡ 48): mực chạm viền ô nghĩa là nét bị vẽ lệch rồi bị cắt.
            if (m.OnEdge > 0) clipped.Add(name);
            // Icon Lucide cân quanh tâm viewBox; lệch quá 3.5 đơn vị là có nét vẽ sai chỗ.
            if (MathF.Abs(m.CenterX - 0.5f) > 0.146f || MathF.Abs(m.CenterY - 0.5f) > 0.146f) offCentre.Add($"{name}({m.CenterX:F2},{m.CenterY:F2})");
        }
        h.Check($"icon: cả {Icon.All.Count} icon đều vẽ ra nét, không rỗng và không thành khối đặc [{string.Join(", ", empty)}]", empty.Count == 0);
        h.Check($"icon: không icon nào bị cắt ở mép ô [{string.Join(", ", clipped)}]", clipped.Count == 0);
        h.Check($"icon: mực của mọi icon cân quanh tâm ô [{string.Join(", ", offCentre)}]", offCentre.Count == 0);

        Bounds Box(string name) => h.Box(w, DisplayInteractionProbe.IconKeys[Icon.All.ToList().FindIndex(x => x.Name == name)]);
        var x = Box("X");
        h.Check("icon X: hai nét chéo cắt nhau ở tâm (nét thứ hai bắt đầu bằng lệnh m thường của Lucide)",
            Ink(canvas, x, 0.5f, 0.5f) && Ink(canvas, x, 0.33f, 0.33f) && Ink(canvas, x, 0.67f, 0.33f) && Ink(canvas, x, 0.33f, 0.67f)
            && Ink(canvas, x, 0.67f, 0.67f) && !Ink(canvas, x, 0.5f, 0.2f) && !Ink(canvas, x, 0.2f, 0.5f));
        var plus = Box("Plus");
        h.Check("icon Plus: nét ngang và nét dọc qua tâm, góc trống",
            Ink(canvas, plus, 0.5f, 0.3f) && Ink(canvas, plus, 0.5f, 0.7f) && Ink(canvas, plus, 0.3f, 0.5f) && Ink(canvas, plus, 0.7f, 0.5f) && !Ink(canvas, plus, 0.3f, 0.3f));
        var arrow = Box("ArrowRight");
        h.Check("icon ArrowRight: thân ngang bên trái, hai cánh mũi tên bên phải",
            Ink(canvas, arrow, 0.3f, 0.5f) && Ink(canvas, arrow, 0.646f, 0.354f) && Ink(canvas, arrow, 0.646f, 0.646f) && !Ink(canvas, arrow, 0.3f, 0.3f));
        var check = Box("Check");
        h.Check("icon Check: đáy dấu tích ở (9,17), nhánh dài lên góc phải trên",
            Ink(canvas, check, 9 / 24f, 16.5f / 24f) && Ink(canvas, check, 15 / 24f, 11 / 24f) && !Ink(canvas, check, 0.25f, 0.25f));
        var search = Box("Search");
        h.Check("icon Search: vòng kính rỗng ở giữa, cán ở góc phải dưới",
            !Ink(canvas, search, 11 / 24f, 11 / 24f) && Ink(canvas, search, 3 / 24f, 11 / 24f) && Ink(canvas, search, 19.5f / 24f, 19.5f / 24f));

        var def = h.Box(w, "ic-default");
        h.Check($"icon: cỡ mặc định 16px ({def.Width}x{def.Height})", def.Width == 16 && def.Height == 16);
        var red = h.Box(w, "ic-red");
        int reddish = 0, other = 0;
        for (int y = (int)red.Y; y < (int)red.Bottom; y++)
            for (int px = (int)red.X; px < (int)red.Right; px++)
            {
                uint v = canvas.Pixels[y * canvas.Width + px];
                int r = (int)(v >> 16) & 255, g = (int)(v >> 8) & 255, b = (int)v & 255;
                if (r > 200 && g < 60 && b < 60) reddish++;
                else if (r < 100) other++; // nét tối màu = không theo màu chữ của cha
            }
        h.Check($"icon: lấy màu chữ của cha (text-destructive): {reddish} pixel đỏ, {other} pixel tối", reddish > 100 && other == 0);

        // Spinner là cung hở: quay thì vị trí mực trong ô đổi theo thời gian.
        foreach (string key in new[] { "ic-spin", "ui-spin" })
        {
            var box = h.Box(w, key);
            var first = h.Render(w);
            h.Wait(w, 0.25);
            var second = h.Render(w);
            int ink = Measure(first, box).Count, moved = 0;
            for (int y = (int)box.Y; y < (int)box.Bottom; y++)
                for (int px = (int)box.X; px < (int)box.Right; px++)
                    if ((Lum(first.Pixels[y * first.Width + px]) < 140) != (Lum(second.Pixels[y * second.Width + px]) < 140)) moved++;
            h.Check($"spinner {key}: cung Loader2 48px đang quay ({ink} pixel mực, {moved} pixel đổi sau 0.25s)", ink > 100 && box.Width == 48 && moved > 60);
        }
    }

    static void Images(CheckHarness h, Window w)
    {
        var a = h.Box(w, "a-img");
        var head = h.Pixel(w, CheckHarness.Center(a));
        var sky = h.Pixel(w, new Point(a.X + a.Width / 2, a.Y + 3));
        var corner = h.Pixel(w, new Point(a.X + 1, a.Y + 1));
        h.Check($"avatar ảnh: giữa là màu ảnh (đầu người {head}), đỉnh là nền tím của ảnh {sky}",
            head.R > 235 && head.B is > 200 and < 245 && sky.B > 200 && sky.R < 160);
        h.Check($"avatar ảnh: góc hộp bị cắt tròn nên vẫn là nền trang {corner}", Lum(corner) > 250);
        var n = h.Box(w, "a-null");
        var fallback = h.Pixel(w, new Point(n.X + 5, n.Y + n.Height / 2));
        h.Check($"avatar không ảnh (null): hiện nền muted của chữ viết tắt {fallback}", Lum(fallback) is > 235 and < 250);
        // Ảnh 4:3 trong ô vuông: object-cover lấp kín, không để hở dải nền ở trên/dưới.
        var wide = h.Box(w, "a-wide");
        var top = h.Pixel(w, new Point(wide.X + wide.Width / 2, wide.Y + 2));
        var bottom = h.Pixel(w, new Point(wide.X + wide.Width / 2, wide.Bottom - 3));
        h.Check($"avatar ảnh ngang: object-cover lấp kín chiều cao (trời {top}, núi {bottom})", top.B > 200 && top.R < 200 && bottom.G > bottom.R && Lum(bottom) < 120);
    }

    static void Buttons(CheckHarness h, Window w, DisplayInteractionProbe p)
    {
        var a = h.Box(w, "b-a");
        h.Check("button: chưa focus thì không có vòng focus", !FormsKeyboardChecks.Ring(h, w, a));
        h.Press(w, Key.Tab);
        h.Check("button: Tab focus nút đầu tiên và hiện vòng focus", w.IsFocused("b-a") && w.FocusVisible && FormsKeyboardChecks.Ring(h, w, a));
        h.Press(w, Key.Enter);
        h.Press(w, Key.Space);
        h.Check($"button: Enter và Space đều gọi OnClick ({p.Clicks})", p.Clicks == 2);
        h.Press(w, Key.Tab);
        h.Check("button: Tab bỏ qua nút vô hiệu hoá", w.IsFocused("b-link") && p.DisabledClicks == 0 && !FormsKeyboardChecks.Ring(h, w, a));
        h.Press(w, Key.Tab);
        var del = h.Box(w, "b-del");
        h.Wait(w, 0.3);
        var ring = h.Pixel(w, new Point(del.X - 2, del.Y + del.Height / 2));
        h.Check($"button destructive: vòng focus màu đỏ nhạt {ring}", w.IsFocused("b-del") && ring.R > ring.G + 15 && ring.R > 230);
        h.Press(w, Key.Tab);
        h.Check("button ghost (nền trong suốt): vòng focus nằm ngoài, không tràn vào trong nút",
            w.IsFocused("b-ghost") && FormsKeyboardChecks.Ring(h, w, h.Box(w, "b-ghost"))
            && Lum(h.Pixel(w, new Point(h.Box(w, "b-ghost").X + 5, h.Box(w, "b-ghost").Y + 16))) > 250);
        h.Click(w, "b-a");
        h.Check("button: bấm chuột thì focus nhưng không hiện vòng focus", w.IsFocused("b-a") && !w.FocusVisible && !FormsKeyboardChecks.Ring(h, w, a) && p.Clicks == 3);
        h.Press(w, Key.Escape);
        h.Check("button: Escape bỏ focus", !w.HasElementFocus);

        // Variant Link: hover thì chữ có gạch chân → thêm một dải mực dưới chữ.
        foreach (string key in new[] { "b-link", "badge-link" })
        {
            var box = h.Box(w, key);
            h.Move(w, new Point(5, 5));
            h.Wait(w, 0.3);
            int before = Measure(h.Render(w), box).Count;
            h.Move(w, CheckHarness.Center(box));
            h.Wait(w, 0.3);
            int after = Measure(h.Render(w), box).Count;
            h.Check($"{key}: hover thì gạch chân chữ (mực {before} → {after})", after > before + 20);
        }
    }
}
