using System.Runtime.CompilerServices;
using GotX;
using GotX.Text;

namespace ShadcnShots;

/// <summary>
/// Geist / Geist Mono (OFL-1.1, static TTF from vercel/geist-font v1.9.2) — the fonts ui.shadcn.com uses.
/// Files live in <c>samples/Shadcn/Assets/fonts</c> (copied next to the executable on build).
/// </summary>
public static class GeistFonts
{
    static readonly (string File, FontWeight Weight, FontFamily Family)[] Files =
    [
        ("Geist-Regular.ttf", FontWeight.Normal, FontFamily.Sans),
        ("Geist-Medium.ttf", FontWeight.Medium, FontFamily.Sans),
        ("Geist-SemiBold.ttf", FontWeight.Semibold, FontFamily.Sans),
        ("Geist-Bold.ttf", FontWeight.Bold, FontFamily.Sans),
        ("GeistMono-Regular.ttf", FontWeight.Normal, FontFamily.Mono),
        ("GeistMono-Medium.ttf", FontWeight.Medium, FontFamily.Mono),
    ];

    /// <summary>Font folder: next to the executable, else the source tree (tests compile this file from samples/Shadcn).</summary>
    public static string Dir => FindDir();

    static string FindDir([CallerFilePath] string source = "")
    {
        string local = Path.Combine(AppContext.BaseDirectory, "Assets", "fonts");
        if (File.Exists(Path.Combine(local, Files[0].File))) return local;
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source) ?? ".", "..", "Assets", "fonts"));
    }

    /// <summary>Adds Geist to <paramref name="text"/>. Call before any other font is added so Geist wins weight ties.</summary>
    public static bool Register(TextSystem text)
    {
        string dir = Dir;
        bool all = true;
        foreach (var (file, weight, family) in Files) all &= text.TryAddFont(Path.Combine(dir, file), weight, family);
        return all;
    }

    /// <summary>A text system with Geist only (headless captures and fidelity tests: deterministic, no system fonts).</summary>
    public static TextSystem Create()
    {
        var text = new TextSystem();
        if (!Register(text)) throw new InvalidOperationException($"Geist fonts not found in {Dir}.");
        return text;
    }
}

/// <summary>
/// Wraps a platform so Geist is registered BEFORE the platform's default fonts: <see cref="TextSystem.FontFor(FontFamily, FontWeight, bool)"/>
/// keeps the first font on a tie, so Geist wins every weight it has; system fonts stay as fallback (italic, missing glyphs).
/// </summary>
public sealed class GeistPlatform(IPlatform inner) : IPlatform
{
    public void Run(Action onLaunch) => inner.Run(onLaunch);
    public IPlatformWindow OpenWindow(WindowOptions options) => inner.OpenWindow(options);
    public void PostToMain(Action action) => inner.PostToMain(action);
    public IClipboard Clipboard => inner.Clipboard;

    public void RegisterDefaultFonts(TextSystem text)
    {
        GeistFonts.Register(text);
        inner.RegisterDefaultFonts(text);
    }
}
