using GotX;
using GotX.Components;
using GotX.Raster;
using GotX.Text;

/// <summary>
/// Công cụ cho các kiểm tra headless: mở Window không cần platform, mô phỏng chuột/phím, xuất PNG.
/// Window mở ở đây tắt cache view, nên mọi phần tử đều có trong frame gần nhất và tra được bằng <see cref="Box"/>.
/// </summary>
/// <summary>
/// State cho handler <c>static</c> của nút mở overlay trong view thử: giữ cửa sổ (gán trong hàm dựng) và component cần mở,
/// để hàm dựng – chạy lại mỗi lần render – không tạo lambda capture mới.
/// </summary>
sealed class Opener
{
    public Window Window = null!;
    public Dialog? Dialog;
    public AlertDialog? Alert;
    public Sheet? Sheet;

    /// <summary>Số lần một handler đếm đã chạy.</summary>
    public int Count;
}

sealed class CheckHarness
{
    public readonly TextSystem Text = new();
    public readonly string OutDir;
    public readonly List<string> UnknownClasses = new();
    public double Now;
    public int Pass, Fail;
    public Size Size = new(900, 700);

    public CheckHarness(string outDir)
    {
        OutDir = outDir;
        Directory.CreateDirectory(outDir);
        string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[][] candidates =
        [
            [Path.Combine(fonts, "segoeui.ttf"), Path.Combine(fonts, "seguisb.ttf"), Path.Combine(fonts, "segoeuib.ttf")],
            ["/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"],
        ];
        foreach (var set in candidates)
        {
            Text.TryAddFont(set[0], FontWeight.Normal);
            if (set[1] != "") Text.TryAddFont(set[1], FontWeight.Semibold);
            if (set[2] != "") Text.TryAddFont(set[2], FontWeight.Bold);
            if (Text.HasFonts) break;
        }
        if (!Text.HasFonts) throw new InvalidOperationException("Không tìm thấy font TrueType.");
        Text.TryAddFont(Path.Combine(fonts, "segoeuii.ttf"), FontWeight.Normal, FontFamily.Sans, italic: true);
        Text.TryAddFont(Path.Combine(fonts, "consola.ttf"), FontWeight.Normal, FontFamily.Mono);
        Text.TryAddFont(Path.Combine(fonts, "consolab.ttf"), FontWeight.Bold, FontFamily.Mono);
        Tailwind.OnUnknownClass = (classes, token) => UnknownClasses.Add($"{token}   (trong \"{classes}\")");
    }

    public void Check(string name, bool ok)
    {
        if (ok) { Pass++; return; }
        Fail++;
        Console.WriteLine($"  ✗ {name}");
    }

    /// <summary>Mở một cửa sổ headless với view gốc do <paramref name="root"/> tạo, đã vẽ một frame.</summary>
    public Window Open(Func<App, View> root, bool dark = false)
    {
        var app = new App { Clock = () => Now }; // hẹn giờ của app chạy theo cùng đồng hồ ảo với cửa sổ
        var window = new Window(app, Text) { Clock = () => Now, CacheEnabled = false, DarkMode = dark };
        window.Root = root(app);
        Frames(window);
        return window;
    }

    /// <summary>Xử lý effect rồi vẽ <paramref name="count"/> frame, mỗi frame cách nhau 16ms; hẹn giờ tới hạn được chạy trước mỗi frame.</summary>
    public void Frames(Window w, int count = 2)
    {
        w.App.Flush();
        for (int i = 0; i < count; i++) { Now += 0.016; w.App.RunTimers(); w.Draw(Size, 1); }
    }

    /// <summary>Cho thời gian trôi (transition, animation) rồi vẽ lại.</summary>
    public void Wait(Window w, double seconds)
    {
        int frames = Math.Max(1, (int)(seconds / 0.016));
        Frames(w, frames);
    }

    /// <summary>Hộp layout của phần tử có <c>.Key(key)</c> trong frame gần nhất. Ném lỗi nếu không có.</summary>
    public Bounds Box(Window w, string key) => w.FindBounds(key) ?? throw new InvalidOperationException($"Không thấy phần tử có Key \"{key}\".");

    public bool Exists(Window w, string key) => w.FindBounds(key) is not null;

    public static Point Center(Bounds b) => new(b.X + b.Width / 2, b.Y + b.Height / 2);

    public void Move(Window w, Point p)
    {
        w.DispatchInput(new InputEvent(InputKind.MouseMove, p));
        Frames(w);
    }

    public void Click(Window w, Point p, MouseButton button = MouseButton.Left)
    {
        w.DispatchInput(new InputEvent(InputKind.MouseMove, p));
        w.DispatchInput(new InputEvent(InputKind.MouseDown, p, button));
        w.DispatchInput(new InputEvent(InputKind.MouseUp, p, button));
        Frames(w);
    }

    /// <summary>Bấm vào giữa phần tử có <c>.Key(key)</c>.</summary>
    public void Click(Window w, string key, MouseButton button = MouseButton.Left) => Click(w, Center(Box(w, key)), button);

    /// <summary>Nhấn ở <paramref name="from"/>, rê qua vài điểm tới <paramref name="to"/>, rồi thả.</summary>
    public void Drag(Window w, Point from, Point to)
    {
        w.DispatchInput(new InputEvent(InputKind.MouseMove, from));
        w.DispatchInput(new InputEvent(InputKind.MouseDown, from));
        for (int i = 1; i <= 4; i++)
            w.DispatchInput(new InputEvent(InputKind.MouseMove, new Point(from.X + (to.X - from.X) * i / 4, from.Y + (to.Y - from.Y) * i / 4)));
        w.DispatchInput(new InputEvent(InputKind.MouseUp, to));
        Frames(w);
    }

    public void Press(Window w, Key key, Modifiers modifiers = Modifiers.None)
    {
        w.DispatchInput(new InputEvent(InputKind.KeyDown, Key: key, Modifiers: modifiers));
        Frames(w);
    }

    public void Type(Window w, string text)
    {
        w.DispatchInput(new InputEvent(InputKind.TextInput, Text: text));
        Frames(w);
    }

    public void Scroll(Window w, Point p, float deltaY)
    {
        w.DispatchInput(new InputEvent(InputKind.Scroll, p, Delta: new Point(0, deltaY)));
        Wait(w, 0.6);
    }

    /// <summary>Vẽ frame hiện tại ra canvas CPU (để đo pixel).</summary>
    public Canvas Render(Window w)
    {
        var canvas = new Canvas();
        canvas.Resize((int)Size.Width, (int)Size.Height);
        SoftwareRenderer.Render(w.Draw(Size, 1), canvas);
        return canvas;
    }

    /// <summary>Màu (r, g, b) của pixel tại điểm logical.</summary>
    public (int R, int G, int B) Pixel(Window w, Point p)
    {
        var c = Render(w);
        uint v = c.Pixels[(int)p.Y * c.Width + (int)p.X];
        return ((int)(v >> 16) & 255, (int)(v >> 8) & 255, (int)v & 255);
    }

    public void Save(Window w, string name)
    {
        string path = Path.Combine(OutDir, name);
        PngWriter.Write(path, Render(w));
        Console.WriteLine($"  → {path}");
    }
}
