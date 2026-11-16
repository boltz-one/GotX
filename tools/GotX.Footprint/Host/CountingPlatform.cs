namespace GotX.Footprint;

/// <summary>Platform decorator: counts frames the window asked to draw and scenes handed to the renderer; stamps the first completed present.</summary>
sealed class CountingPlatform(IPlatform inner) : IPlatform
{
    public static long Frames, Presents, FirstPresentMs = -1;

    public void Run(Action onLaunch) => inner.Run(onLaunch);
    public void PostToMain(Action action) => inner.PostToMain(action);
    public IClipboard Clipboard => inner.Clipboard;
    public void RegisterDefaultFonts(GotX.Text.TextSystem text) => inner.RegisterDefaultFonts(text);
    public IPlatformWindow OpenWindow(WindowOptions options) => new CountingWindow(inner.OpenWindow(options));

    sealed class CountingWindow : IPlatformWindow
    {
        readonly IPlatformWindow _w;
        public CountingWindow(IPlatformWindow w)
        {
            _w = w;
            w.Frame += () => { Frames++; Frame?.Invoke(); };
            w.Input += e => Input?.Invoke(e);
            w.Closed += () => Closed?.Invoke();
        }
        public Size LogicalSize => _w.LogicalSize;
        public float ScaleFactor => _w.ScaleFactor;
        public void RequestFrame() => _w.RequestFrame();
        public void RequestAnimationFrame() => _w.RequestAnimationFrame();
        public event Action<InputEvent>? Input;
        public event Action? Frame;
        public event Action? Closed;
        public Func<Bounds?>? ImeCaret { set => _w.ImeCaret = value; }
        public Func<CursorKind>? CursorProvider { set => _w.CursorProvider = value; }

        public void Draw(Scene scene)
        {
            _w.Draw(scene);
            Presents++;
            if (FirstPresentMs < 0)
                FirstPresentMs = (long)(DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds;
        }
    }
}
