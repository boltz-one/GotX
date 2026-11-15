using GotX;
using GotX.Components;

/// <summary>
/// Kiểm tra nhóm Display trên <see cref="DisplayProbe"/>: kích thước theo class (h-5, size-8…), vị trí tương đối giữa các
/// phần (grid của alert/card, nhóm nút dính liền, avatar chồng nhau) và màu pixel thật ở chế độ sáng/tối.
/// </summary>
static class DisplayChecks
{
    static bool Near(float a, float b, float tol = 0.5f) => MathF.Abs(a - b) <= tol;
    static int Lum((int R, int G, int B) c) => (c.R + c.G + c.B) / 3;

    /// <summary>Chạy toàn bộ kiểm tra; cửa sổ được nới cao để probe nằm trọn trong canvas.</summary>
    public static void Run(CheckHarness h)
    {
        var saved = h.Size;
        h.Size = new Size(900, 1000);
        try
        {
            var w = h.Open(app => app.New(new DisplayProbe()));
            var d = h.Open(app => app.New(new DisplayProbe()), dark: true);
            Sizes(h, w);
            Layout(h, w);
            Colors(h, w, d);
            h.Save(w, "display-probe.png");
            DisplayInteractionChecks.Run(h);
        }
        finally { h.Size = saved; }
    }

    static void Sizes(CheckHarness h, Window w)
    {
        var badge = h.Box(w, "p-badge-default");
        h.Check($"badge: cao 20px (h-5), rộng theo chữ ({badge.Height}x{badge.Width})", Near(badge.Height, 20) && badge.Width < 100);
        var kbd = h.Box(w, "p-kbd");
        h.Check($"kbd: cao 20px, rộng tối thiểu 20px ({kbd.Width}x{kbd.Height})", Near(kbd.Height, 20) && kbd.Width >= 19.5f);
        h.Check("spinner: mặc định 16px, size=24 thì 24px",
            Near(h.Box(w, "p-spinner").Width, 16) && Near(h.Box(w, "p-spinner-24").Height, 24));
        h.Check("avatar: Sm 24, Default 32, Lg 40",
            Near(h.Box(w, "p-avatar-sm").Width, 24) && Near(h.Box(w, "p-avatar").Height, 32) && Near(h.Box(w, "p-avatar-lg").Width, 40));
        var ratio = h.Box(w, "p-ratio");
        h.Check($"aspect-ratio: rộng 160, tỉ lệ 16:9 → cao 90 ({ratio.Width}x{ratio.Height})", Near(ratio.Width, 160) && Near(ratio.Height, 90));
        var sepH = h.Box(w, "p-sep-h");
        h.Check($"separator ngang: cao 1px, rộng bằng cha 420 ({sepH.Width}x{sepH.Height})", Near(sepH.Height, 1) && Near(sepH.Width, 420));
        var sepV = h.Box(w, "p-sep-v");
        h.Check($"separator dọc: rộng 1px, giãn theo hàng cao 40 ({sepV.Width}x{sepV.Height})", Near(sepV.Width, 1) && Near(sepV.Height, 40));
    }

    static void Layout(CheckHarness h, Window w)
    {
        // Avatar: badge ở góc dưới phải; nhóm chồng nhau 8px.
        var av = h.Box(w, "p-avatar");
        var dot = h.Box(w, "p-avatar-badge");
        h.Check("avatar badge: 10px, sát góc dưới phải", Near(dot.Width, 10) && Near(dot.Right, av.Right) && Near(dot.Bottom, av.Bottom));
        var ga = h.Box(w, "p-group-a");
        var gb = h.Box(w, "p-group-b");
        var gc = h.Box(w, "p-group-count");
        h.Check($"avatar group: avatar sau chồng lên 8px ({ga.Right} → {gb.X})", Near(gb.X, ga.Right - 8) && Near(gc.X, gb.Right - 8));

        // Button group: các nút liền nhau, không khe hở.
        var b1 = h.Box(w, "p-bg-1");
        var b2 = h.Box(w, "p-bg-2");
        var b3 = h.Box(w, "p-bg-3");
        h.Check("button group ngang: các nút dính liền, cùng hàng", Near(b2.X, b1.Right) && Near(b3.X, b2.Right) && Near(b1.Y, b3.Y));
        var v1 = h.Box(w, "p-bgv-1");
        var v2 = h.Box(w, "p-bgv-2");
        h.Check("button group dọc: nút dưới nằm ngay dưới nút trên", Near(v2.Y, v1.Bottom) && Near(v1.X, v2.X));

        // Card: đệm 16px (Sm 12px); action cột phải cùng hàng tiêu đề; mô tả dưới tiêu đề; footer sát đáy.
        var card = h.Box(w, "p-card");
        var title = h.Box(w, "p-card-title");
        var action = h.Box(w, "p-card-action");
        var desc = h.Box(w, "p-card-desc");
        h.Check($"card: tiêu đề cách mép trái/trên 16px ({title.X - card.X}, {title.Y - card.Y})",
            Near(title.X - card.X, 16) && Near(title.Y - card.Y, 16));
        h.Check("card: action ở cột phải, ngang hàng tiêu đề, sát lề phải 16px",
            Near(action.Y, title.Y) && Near(action.Right, card.Right - 16) && action.X > title.Right - 0.5f);
        h.Check("card: mô tả nằm dưới tiêu đề, cùng cột trái", desc.Y >= title.Bottom - 0.5f && Near(desc.X, title.X));
        var footer = h.Box(w, "p-card-footer");
        h.Check("card: footer sát đáy, rộng hết card (hasFooter bỏ pb)", Near(footer.Bottom, card.Bottom) && Near(footer.Width, card.Width));
        var sm = h.Box(w, "p-card-sm");
        var smHeader = h.Box(w, "p-card-sm-header");
        h.Check("card Sm: đệm 12px", Near(smHeader.Y - sm.Y, 12) && Near(smHeader.X - sm.X, 0) && Near(sm.Bottom - smHeader.Bottom, 12));

        // Alert: icon cột trái, tiêu đề + mô tả cột phải; action góc trên phải.
        var alert = h.Box(w, "p-alert");
        var icon = h.Box(w, "p-alert-icon");
        var aTitle = h.Box(w, "p-alert-title");
        var aDesc = h.Box(w, "p-alert-desc");
        var aAction = h.Box(w, "p-alert-action");
        h.Check("alert: icon cột trái, tiêu đề cách icon 8px", Near(aTitle.X, icon.Right + 8) && icon.X < aTitle.X);
        h.Check("alert: mô tả dưới tiêu đề, cùng cột", aDesc.Y >= aTitle.Bottom - 0.5f && Near(aDesc.X, aTitle.X));
        // Absolute tính từ mép trong viền (padding box) như CSS: viền 1px + right-2/top-2.
        h.Check($"alert: action cách góc trên phải 8px trong viền ({aAction})", Near(aAction.Right, alert.Right - 9) && Near(aAction.Y, alert.Y + 9));
        h.Check("alert: hasAction chừa lề phải 72px cho chữ", aTitle.Right <= alert.Right - 72 + 0.5f);

        // Item: media → content (giãn) → actions sát phải.
        var item = h.Box(w, "p-item");
        var media = h.Box(w, "p-item-media");
        var content = h.Box(w, "p-item-content");
        var actions = h.Box(w, "p-item-actions");
        h.Check("item: media 40px (Image), content giãn giữa, actions cách lề phải 12px + viền 1px",
            Near(media.Width, 40) && Near(content.X, media.Right + 10) && Near(actions.Right, item.Right - 13) && Near(media.X, item.X + 13) && Near(content.Right, actions.X - 10));
        h.Check("item Xs: gọn hơn item thường", h.Box(w, "p-item-xs").Height < item.Height);

        // Empty: nội dung căn giữa.
        var empty = h.Box(w, "p-empty");
        var eTitle = h.Box(w, "p-empty-title");
        var eMedia = h.Box(w, "p-empty-media");
        h.Check("empty: icon 32px, icon và tiêu đề căn giữa theo chiều ngang",
            Near(eMedia.Width, 32) && Near(CheckHarness.Center(eMedia).X, CheckHarness.Center(empty).X, 1)
            && Near(CheckHarness.Center(eTitle).X, CheckHarness.Center(empty).X, 1));
    }

    static void Colors(CheckHarness h, Window w, Window d)
    {
        // Lấy mẫu ở mép trái badge (đã qua phần bo góc, chưa tới chữ).
        static Point BadgeSample(Bounds b) => new(b.X + 6, b.Y + b.Height / 2);
        var def = h.Pixel(w, BadgeSample(h.Box(w, "p-badge-default")));
        var sec = h.Pixel(w, BadgeSample(h.Box(w, "p-badge-secondary")));
        var outl = h.Pixel(w, BadgeSample(h.Box(w, "p-badge-outline")));
        var dest = h.Pixel(w, BadgeSample(h.Box(w, "p-badge-destructive")));
        h.Check($"badge sáng: Default nền tối {def}, Secondary nền xám nhạt {sec}, Outline nền trắng {outl}",
            Lum(def) < 40 && Lum(sec) is > 235 and < 250 && Lum(outl) > 250);
        h.Check($"badge: Destructive nền đỏ nhạt {dest}", dest.R > dest.G + 10 && dest.R > 240);
        var defDark = h.Pixel(d, BadgeSample(h.Box(d, "p-badge-default")));
        h.Check($"badge tối: Default đảo sang nền sáng {defDark}", Lum(defDark) > 200);

        var sk = h.Box(w, "p-skeleton");
        var skLight = h.Pixel(w, CheckHarness.Center(sk));
        var skDark = h.Pixel(d, CheckHarness.Center(h.Box(d, "p-skeleton")));
        h.Check($"skeleton: nền muted, sáng {skLight} / tối {skDark}", Lum(skLight) is > 225 and < 252 && Lum(skDark) is > 20 and < 70);

        var card = h.Box(d, "p-card-content");
        var cardDark = h.Pixel(d, new Point(card.Right - 4, card.Y + 2));
        var bgDark = h.Pixel(d, new Point(890, 990));
        h.Check($"card tối: nền card (#171717) sáng hơn nền trang {cardDark} vs {bgDark}", Lum(cardDark) is >= 20 and <= 26 && Lum(bgDark) < 15);

        // Separator ngang: pixel giữa đường kẻ khác nền.
        var sep = h.Box(w, "p-sep-h");
        var line = h.Pixel(w, new Point(sep.X + 100, sep.Y));
        h.Check($"separator: kẻ màu border (#e5e5e5) {line}", Lum(line) is > 220 and < 235);

        // Icon trong nút của nhóm dọc phải vẽ ra: điểm tối nhất trong ô 16px phải đậm như màu chữ.
        var chev = h.Box(w, "p-chev");
        var canvas = h.Render(w);
        int darkest = 255;
        for (int y = (int)chev.Y; y < (int)chev.Bottom; y++)
            for (int x = (int)chev.X; x < (int)chev.Right; x++)
            {
                uint v = canvas.Pixels[y * canvas.Width + x];
                darkest = Math.Min(darkest, (int)(((v >> 16) & 255) + ((v >> 8) & 255) + (v & 255)) / 3);
            }
        h.Check($"button group dọc: icon trong nút được vẽ (độ sáng thấp nhất {darkest})", darkest < 100);

        var av = h.Box(d, "p-avatar");
        var avDark = h.Pixel(d, new Point(av.X + 6, av.Y + av.Height / 2));
        h.Check($"avatar tối: nền muted #262626 {avDark}", Lum(avDark) is > 30 and < 50);
    }
}
