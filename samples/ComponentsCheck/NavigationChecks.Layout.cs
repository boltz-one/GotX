using GotX;
using GotX.Components;

/// <summary>Kiểm tra Navigation phần bố cục: resizable, carousel, sidebar, chart, table.</summary>
static partial class NavigationChecks
{
    static void ResizableChecks(CheckHarness h)
    {
        Resizable rz = null!, rv = null!;
        var sizes = new List<float>();
        var w = Host(h, app =>
        {
            rz = app.New(new Resizable
            {
                First = static w => w.Div().Key("rz-a"), Second = static w => w.Div().Key("rz-b"),
                FirstMinSize = 0.2f, SecondMinSize = 0.25f, BoxClass = "h-32 w-[600px]",
            });
            rv = app.New(new Resizable
            {
                Direction = ResizableDirection.Vertical, BoxClass = "h-[200px] w-[300px]",
                First = static w => w.Div().Key("rv-a"), Second = static w => w.Div().Key("rv-b"),
            });
            app.Subscribe<Resizable.SizeChanged>(rz, e => sizes.Add(e.Size));
            return w => w.Div().Class("flex flex-col gap-4 p-4").Child(w.View(rz)).Child(w.View(rv));
        });
        var a0 = h.Box(w, "rz-a");
        var b0 = h.Box(w, "rz-b");
        h.Check($"resizable: ban đầu chia đôi ({a0.Width:F0} | {b0.Width:F0})", Near(a0.Width, 300) && Near(b0.Width, 299));

        var handle = CheckHarness.Center(h.Box(w, rz.HandleKey));
        h.Drag(w, handle, new Point(handle.X - 120, handle.Y));
        var a1 = h.Box(w, "rz-a");
        var b1 = h.Box(w, "rz-b");
        h.Check($"resizable: kéo trái 120px → panel đầu 180px, panel sau giãn ({a1.Width:F0} | {b1.Width:F0})",
            Near(a1.Width, 180) && Near(b1.Width, 419) && sizes.Count > 0 && Near(sizes[^1], 0.3f, 0.005f));

        handle = CheckHarness.Center(h.Box(w, rz.HandleKey));
        h.Drag(w, handle, new Point(handle.X - 400, handle.Y));
        h.Check($"resizable: kéo quá xa bị chặn ở min 20% ({h.Box(w, "rz-a").Width:F0})", Near(h.Box(w, "rz-a").Width, 120));
        handle = CheckHarness.Center(h.Box(w, rz.HandleKey));
        h.Drag(w, handle, new Point(handle.X + 800, handle.Y));
        h.Check($"resizable: kéo hết sang phải bị chặn bởi min 25% của panel sau ({h.Box(w, "rz-b").Width:F0})",
            Near(h.Box(w, "rz-a").Width, 450) && Near(h.Box(w, "rz-b").Width, 149));

        var vh = CheckHarness.Center(h.Box(w, rv.HandleKey));
        h.Drag(w, vh, new Point(vh.X, vh.Y + 40));
        h.Check($"resizable dọc: kéo xuống 40px → panel trên 140px ({h.Box(w, "rv-a").Height:F0})",
            Near(h.Box(w, "rv-a").Height, 140) && Near(h.Box(w, "rv-a").Width, 300));
    }

    static void CarouselChecks(CheckHarness h)
    {
        Carousel car = null!;
        int events = 0;
        string[] colors = ["h-32 bg-red-500", "h-32 bg-green-500", "h-32 bg-blue-500"];
        var w = Host(h, app =>
        {
            car = app.New(new Carousel { Count = 3, Slide = (w, i) => w.Div().Class(colors[i]), ShowIndex = false });
            app.Subscribe<Carousel.IndexChanged>(car, _ => events++);
            return w => w.Div().Class("w-[400px] px-12 py-4").Key("car-host").Child(w.View(car));
        });
        var view = h.Box(w, "car-host");
        var center = new Point(view.X + view.Width / 2, view.Y + 16 + 64);
        h.Click(w, car.PreviousKey);
        h.Check("carousel: slide đầu thì nút lùi vô hiệu", car.Index == 0 && events == 0 && h.Pixel(w, center).R > 200);
        h.Click(w, car.NextKey);
        h.Click(w, car.NextKey);
        h.Wait(w, 0.6);
        var px = h.Pixel(w, center);
        h.Check($"carousel: tiến hai lần → slide 3 (xanh dương) nằm giữa khung ({px})", car.Index == 2 && events == 2 && px.B > 200 && px.R < 120);
        h.Click(w, car.NextKey);
        h.Check("carousel: slide cuối thì nút tiến không đi tiếp", car.Index == 2 && events == 2);
        h.Click(w, car.PreviousKey);
        h.Wait(w, 0.6);
        px = h.Pixel(w, center);
        h.Check($"carousel: lùi → slide 2 (xanh lá) ({px})", car.Index == 1 && px.G > 150 && px.R < 120 && px.B < 150);
    }

    static void SidebarChecks(CheckHarness h)
    {
        Sidebar sb = null!;
        var log = new List<string>();
        var w = Host(h, app =>
        {
            sb = app.New(new Sidebar { Groups = [new("Nhóm", [new("Trang chủ"), new("Hộp thư")]), new("Khác", [new("Cài đặt")])] });
            app.Subscribe<Sidebar.CollapsedChanged>(sb, e => log.Add(e.Collapsed ? "thu" : "mở"));
            app.Subscribe<Sidebar.ItemSelected>(sb, e => log.Add($"{e.Group}.{e.Item}:{e.Label}"));
            return w => w.Div().Class("flex h-96").Child(w.View(sb)).Child(Sidebar.Inset(w).Child(sb.Trigger(w)));
        });
        h.Check($"sidebar: mở rộng rộng 16rem ({h.Box(w, sb.Key).Width})", Near(h.Box(w, sb.Key).Width, 256));
        h.Click(w, sb.ItemKey(2));
        h.Check($"sidebar: chọn nút thứ ba → nhóm 1 mục 0 ({string.Join(",", log)})", sb.ActiveIndex == 2 && log is ["1.0:Cài đặt"]);
        h.Click(w, sb.Key + "-trigger");
        var item = h.Box(w, sb.ItemKey(0));
        h.Check($"sidebar: trigger thu gọn còn 3rem, nút menu vuông 32px ({h.Box(w, sb.Key).Width}, {item.Width})",
            sb.Collapsed && Near(h.Box(w, sb.Key).Width, 48) && Near(item.Width, 32));
        h.Click(w, sb.Key + "-trigger");
        h.Check("sidebar: bấm lại thì mở rộng", !sb.Collapsed && Near(h.Box(w, sb.Key).Width, 256) && log[^2..] is ["thu", "mở"]);
    }

    static void ChartChecks(CheckHarness h)
    {
        var w = Host(h, _ => static w => w.Div().Class("w-[500px] p-4")
            .Child(Ui.BarChart(w, ["A", "B", "C"], [new("S", "bg-chart-1", [100, 50, 25]), new("T", "bg-chart-3", [0, 80, 40])],
                plotClass: "h-[200px]", keyPrefix: "bar")));
        var a = h.Box(w, "bar-0-0"); var b = h.Box(w, "bar-0-1"); var c = h.Box(w, "bar-0-2"); var t = h.Box(w, "bar-1-1");
        h.Check($"chart: cột cao nhất chạm đỉnh trục 100 ({a.Height:F1})", Near(a.Height, 200));
        h.Check($"chart: chiều cao tỉ lệ dữ liệu 100:50:25:80 ({a.Height:F0}, {b.Height:F0}, {c.Height:F0}, {t.Height:F0})",
            Near(b.Height, 100) && Near(c.Height, 50) && Near(t.Height, 160));
        h.Check("chart: các cột đứng trên cùng đường đáy", Near(a.Bottom, b.Bottom) && Near(b.Bottom, t.Bottom));
        h.Check("chart: nhóm sau nằm bên phải nhóm trước", b.X > a.Right && c.X > b.Right);
        h.Check("chart: trục Y làm tròn lên số đẹp", Ui.NiceMax(305, 4) == 400 && Ui.NiceMax(186, 4) == 200 && Ui.NiceMax(9, 3) == 15);
    }

    static void TableChecks(CheckHarness h)
    {
        var w = Host(h, _ => static w => w.Div().Class("w-[600px] p-4").Child(Ui.Table(w)
            .Child(Ui.TableRow(w).Class("grid-cols-[200px_200px_200px]")
                .Child(Ui.TableCell(w).Key("tc-l").Child(w.Div().Key("tt-l").Child("Trái")))
                .Child(Ui.TableCell(w, TableAlign.Center).Key("tc-c").Child(w.Div().Key("tt-c").Child("Giữa")))
                .Child(Ui.TableCell(w, TableAlign.Right).Key("tc-r").Child(w.Div().Key("tt-r").Child("$250.00"))))));
        Bounds cl = h.Box(w, "tc-l"), cc = h.Box(w, "tc-c"), cr = h.Box(w, "tc-r");
        Bounds tl = h.Box(w, "tt-l"), tc = h.Box(w, "tt-c"), tr = h.Box(w, "tt-r");
        h.Check($"table: các cột theo lưới 200px ({cl.Width}, {cc.X - cl.X}, {cr.X - cc.X})", Near(cl.Width, 200) && Near(cc.X - cl.X, 200) && Near(cr.X - cc.X, 200));
        h.Check("table: ô căn trái – chữ sát lề trái (đệm 8px)", Near(tl.X, cl.X + 8) && tl.Right < cl.Right - 40);
        h.Check("table: ô căn giữa – chữ ở giữa ô", Near(tc.X + tc.Width / 2, cc.X + cc.Width / 2));
        h.Check("table: ô căn phải – chữ sát lề phải (đệm 8px)", Near(tr.Right, cr.Right - 8) && tr.X > cr.X + 40);
    }
}
