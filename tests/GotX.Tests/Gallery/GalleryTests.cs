using GotX;
using GotX.Test;

/// <summary>
/// Soi các trang gallery của GotX.Components: lint ở sáng/tối × hai bề rộng (so với <c>lint-baseline.txt</c>, chỉ vấn đề MỚI
/// làm hỏng test) và hiệu năng (frame rảnh phát lại hoàn toàn, ngân sách thời gian, cấp phát ổn định).
/// </summary>
static class GalleryTests
{
    /// <summary>Bọc một trang như vùng nội dung của GalleryView: cuộn dọc, nền/chữ theo theme.</summary>
    sealed class PageHost(Func<App, View> page) : View
    {
        View _page = null!;
        protected override void OnCreated() => _page = page(App);
        public override Div Render(Window w) =>
            w.Div().Class("size-full overflow-y-auto bg-background text-foreground").Key("gallery-page").Child(w.View(_page));
    }

    // Animated = số view được phép render lại ở MỖI frame khi trang có animation chạy mãi (0 = trang phải đứng yên).
    // Một phần tử animate-* làm bẩn view chứa nó và mọi view cha (Window.MarkViewDirty đi ngược lên gốc), nên con số này
    // là "chuỗi view từ gốc tới chỗ có animation": display = PageHost + DisplayPage (spinner/skeleton nằm thẳng trong trang);
    // chat = PageHost + ChatPage + Trace đang chạy + Attachment đang tải. Tách animation vào view con KHÔNG giảm được số
    // này chừng nào Core còn làm bẩn view cha; view anh em (các component khác của trang) thì phải được phát lại.
    static readonly (string Name, Func<App, View> Page, int Animated)[] Pages =
    [
        ("display", app => app.New(new DisplayPage()), 2), ("forms", app => app.New(new FormsPage()), 0),
        ("overlays", app => app.New(new OverlaysPage()), 0), ("navigation", app => app.New(new NavigationPage()), 0),
        ("chat", app => app.New(new ChatPage()), 4), ("data", app => app.New(new DataPage()), 0),
    ];

    // Cửa sổ cao để gần như cả trang nằm trong khung nhìn: luật tương phản chỉ đo được chữ đang hiện trên ảnh.
    static readonly Size[] Sizes = [new(1000, 2400), new(640, 2400)];

    public static void Register(TestSuite suite)
    {
        foreach (var (name, page, animated) in Pages)
        {
            Func<App, View> root = app => app.New(new PageHost(page));

            suite.Add($"gallery/{name}/lint", t =>
            {
                var report = UiDoctor.Run(root, new DoctorOptions
                {
                    Sizes = Sizes, Scales = [1, 1.5f], OutDir = t.Options.OutDir, Name = "gallery-" + name,
                });
                if (t.Options.Verbose) t.Notes.Add(report.ToText(200).TrimEnd());
                t.Assert.NoNewLint(name, report.Issues);
            });

            suite.Add($"gallery/{name}/perf", t =>
            {
                t.Open(root, new Size(1000, 760));
                // Trang có spinner/skeleton chạy mãi thì không bao giờ "đứng yên": không đòi frame rảnh, nhưng mỗi frame chỉ
                // được render lại chuỗi view chứa animation (xem Pages), phần còn lại phát lại từ cache.
                bool settles = t.Settle(120);
                t.Assert.True(settles == (animated == 0), $"trang {(animated == 0 ? "phải đứng yên" : "có animation chạy liên tục")} (đứng yên: {settles})");
                if (settles) t.Perf.ExpectIdle();
                else
                {
                    int mark = t.Trace.Mark();
                    t.Step();
                    var rendered = t.Trace.Names(TraceKind.ViewRender, mark);
                    t.Notes.Add("view render mỗi frame animation: " + string.Join(", ", rendered));
                }
                var report = t.Perf.Measure(frames: 30, raster: false);
                t.Assert.True(report.RenderedViews <= animated * report.Frames,
                    $"mỗi frame render {Fmt.N((double)report.RenderedViews / report.Frames)} view (cho phép {animated}: chỉ chuỗi view chứa animation)");
                t.Notes.Add($"{(settles ? "đứng yên" : "có animation chạy liên tục")}; {report}");
                t.Assert.True(report.Total.Median < 30, $"ngân sách frame: trung vị Draw {Fmt.N(report.Total.Median)} ms < 30 ms");
                t.Assert.True(report.AllocatedBytes.Median <= (settles ? 0 : 64 * 1024),
                    $"cấp phát ổn định: trung vị {Fmt.Bytes((long)report.AllocatedBytes.Median)}/frame (cho phép {(settles ? "0" : "64 KB khi có animation")})");
                t.Perf.ExpectReplayMatchesFresh();
            });
        }

        suite.Add("gallery/shell/navigation-renders-only-what-changed", t =>
        {
            t.Open(new GalleryView(), new Size(1100, 760));
            t.ClickText("Biểu mẫu");
            t.Assert.TextVisible("Biểu mẫu");
            // Rê chuột qua một mục điều hướng chỉ được vẽ lại view vỏ (GalleryView), không phải cả trang đang hiện.
            var item = t.Tree.FindText("Chat")!;
            int renders = t.Perf.CountRenders(() => t.Hover(Fmt.Center(item.Screen)));
            t.Assert.True(renders <= 1, $"hover mục điều hướng render {renders} lượt view (mong đợi ≤ 1)");
            t.ClickText("Chế độ tối");
            t.Assert.True(t.Window.DarkMode, "nút chuyển chế độ tối hoạt động");
            t.Assert.TextVisible("Chế độ sáng");
        });
    }
}
