using GotX.Raster;

namespace GotX.Test;

/// <summary>
/// Harness của một test: một <see cref="App"/> + <see cref="Window"/> headless, đồng hồ ảo chung cho cả hai,
/// mô phỏng input, truy vấn cây, assertion ghi lỗi mà không dừng test.
///
/// Cache view BẬT như app thật. Mọi truy vấn phần tử đi qua <see cref="Tree"/> – một frame soi (Window.Inspect,
/// tắt cache trong đúng frame đó) – nên vẫn thấy cả phần tử nằm trong view đang được phát lại từ cache.
/// Cây và canvas được nhớ tới khi có frame mới; đừng gọi thẳng <c>Window.Draw</c> mà hãy dùng <see cref="Draw"/>.
/// </summary>
public sealed partial class UiTest
{
    /// <summary>Bước thời gian của một frame mô phỏng (giây).</summary>
    public const double FrameSeconds = 0.016;

    App? _app;
    Window? _window;
    UiTree? _tree;
    Canvas? _canvas;
    int _artifacts;

    public UiTest(string name = "test", TestOptions? options = null)
    {
        Name = name;
        Options = options ?? new TestOptions();
        Assert = new UiAssert(this);
        Perf = new UiPerf(this);
    }

    public string Name { get; }
    public TestOptions Options { get; }
    public UiAssert Assert { get; }
    public UiPerf Perf { get; }
    public TraceLog Trace { get; } = new();
    public List<Failure> Failures { get; } = new();
    /// <summary>Ghi chú không phải lỗi (snapshot vừa tạo, lỗi lint đã hết…) – runner in ở chế độ verbose và ghi vào report.</summary>
    public List<string> Notes { get; } = new();

    public App App => _app ?? throw new InvalidOperationException("Chưa gọi Open().");
    public Window Window => _window ?? throw new InvalidOperationException("Chưa gọi Open().");
    public bool IsOpen => _window is not null;
    public Size Size { get; private set; } = new(900, 700);
    public float Scale { get; private set; } = 1;
    /// <summary>Đồng hồ ảo (giây) – nguồn thời gian duy nhất của Window.Clock và App.Clock.</summary>
    public double Now { get; private set; } = 1000;

    /// <summary>Mở cửa sổ headless với view gốc do <paramref name="root"/> tạo và vẽ hai frame đầu. Gọi lại để mở cấu hình khác.</summary>
    public void Open(Func<App, View> root, Size? size = null, float scale = 1, bool dark = false, bool cache = true)
    {
        Size = size ?? new Size(900, 700);
        Scale = scale;
        var app = new App { Clock = () => Now };
        var window = new Window(app, TestFonts.Shared) { Clock = () => Now, CacheEnabled = cache, DarkMode = dark, Trace = Trace.Add };
        window.Root = root(app);
        _app = app;
        _window = window;
        Invalidate();
        Frames(2);
    }

    /// <summary>Như <see cref="Open(Func{App, View}, Size?, float, bool, bool)"/> nhưng nhận thẳng view và trả lại nó (đã gắn vào app).</summary>
    public T Open<T>(T view, Size? size = null, float scale = 1, bool dark = false, bool cache = true) where T : View
    {
        Open(app => app.New(view), size, scale, dark, cache);
        return view;
    }

    void Invalidate() { _tree = null; _canvas = null; }

    internal void AdvanceClock(double seconds) => Now += seconds;

    // ================= Thời gian =================

    /// <summary>Vẽ một frame (có cache) tại thời điểm hiện tại rồi xử lý effect phát sinh trong lúc vẽ.</summary>
    public Scene Draw()
    {
        Invalidate();
        var scene = Window.Draw(Size, Scale);
        App.Flush(); // Notify gọi trong Render() phải lộ ra thành FramePending ngay, không đợi input kế tiếp
        return scene;
    }

    /// <summary>Một nhịp của vòng lặp app: đẩy đồng hồ → chạy hẹn giờ tới hạn → vẽ.</summary>
    public void Step(double seconds = FrameSeconds)
    {
        Now += seconds;
        App.RunTimers();
        Draw();
    }

    /// <summary>Xử lý effect rồi chạy <paramref name="count"/> frame cách nhau 16 ms.</summary>
    public void Frames(int count = 2)
    {
        App.Flush();
        for (int i = 0; i < count; i++) Step();
    }

    /// <summary>Cho thời gian trôi <paramref name="seconds"/> giây theo từng frame 16 ms (transition, animation, hẹn giờ).</summary>
    public void Advance(double seconds) => Frames(Math.Max(1, (int)Math.Round(seconds / FrameSeconds)));

    /// <summary>
    /// Vẽ tới khi giao diện đứng yên: không còn yêu cầu vẽ lại và không còn animation. Trả về false nếu sau
    /// <paramref name="maxFrames"/> frame vẫn chưa yên (spinner chạy mãi, view tự Notify mỗi frame…).
    /// </summary>
    public bool Settle(int maxFrames = 300)
    {
        App.Flush();
        for (int i = 0; i < maxFrames; i++)
        {
            if (!Window.FramePending && !Window.WantsAnimationFrame) return true;
            Step();
        }
        return !Window.FramePending && !Window.WantsAnimationFrame;
    }

    // ================= Cây và ảnh =================

    /// <summary>Cây giao diện của trạng thái hiện tại (soi một lần rồi nhớ tới frame kế tiếp).</summary>
    public UiTree Tree
    {
        get
        {
            if (_tree is not null) return _tree;
            App.Flush();
            bool paused = Trace.Paused;
            Trace.Paused = true; // frame soi render lại mọi view: không phải hành vi của app, đừng để lẫn vào nhật ký
            try { _tree = new UiTree(Window.Inspect(Size, Scale)) { SemanticsSource = BuildSemantics }; }
            finally { Trace.Paused = paused; }
            App.Flush();
            _canvas = null;
            return _tree;
        }
    }

    public int PixelWidth => (int)MathF.Ceiling(Size.Width * Scale);
    public int PixelHeight => (int)MathF.Ceiling(Size.Height * Scale);

    /// <summary>Ảnh của trạng thái hiện tại, dựng bằng renderer CPU từ đúng Scene của <see cref="Tree"/>.</summary>
    public Canvas Render()
    {
        if (_canvas is not null) return _canvas;
        var scene = Tree.Snapshot.Scene;
        var canvas = new Canvas();
        canvas.Resize(PixelWidth, PixelHeight);
        SoftwareRenderer.Render(scene, canvas);
        return _canvas = canvas;
    }

    /// <summary>Màu pixel tại điểm logical <paramref name="p"/>.</summary>
    public Rgba PixelAt(Point p) => Pixels.Get(Render(), (int)(p.X * Scale), (int)(p.Y * Scale));

    /// <summary>Đường dẫn file bằng chứng trong thư mục ra: <c>&lt;tên test&gt;.&lt;suffix&gt;</c>.</summary>
    public string ArtifactPath(string suffix)
    {
        string dir = Options.OutDir ?? Path.Combine(Path.GetTempPath(), "gotx-test");
        Directory.CreateDirectory(dir);
        var safe = new System.Text.StringBuilder();
        foreach (char c in Name) safe.Append(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_');
        return Path.Combine(dir, safe + "." + suffix);
    }

    /// <summary>Lưu ảnh hiện tại ra PNG; trả về đường dẫn. <paramref name="outline"/>: các node cần khoanh vùng.</summary>
    public string Save(string name, params UiNode[] outline)
    {
        string path = ArtifactPath(name.EndsWith(".png", StringComparison.Ordinal) ? name : name + ".png");
        var canvas = Render();
        if (outline.Length > 0)
        {
            var marks = new List<Mark>();
            foreach (var n in outline) marks.Add(new Mark(n.Screen, Annotator.Red));
            canvas = Annotator.Annotate(canvas, Scale, marks);
        }
        PngWriter.Write(path, canvas);
        return path;
    }

    /// <summary>Bản in cây (xem <see cref="TreeDump"/>).</summary>
    public string Dump(DumpOptions? options = null) => TreeDump.Dump(Tree, options);

    /// <summary>Giải thích một pixel: mọi primitive phủ lên nó theo thứ tự vẽ và node sở hữu (xem <see cref="PixelExplainer"/>).</summary>
    public PixelExplanation Explain(Point logical) => PixelExplainer.Explain(Tree, Render(), logical);
}
