namespace GotX.Port;

/// <summary>Command dispatcher. Public so the test project can drive commands in-process with a captured writer.</summary>
public static class PortApp
{
    const string Usage = """
        GotX.Port - porting aids (concise output for AI agents)
          classes <file|dir|-> [--json]          Tailwind class audit: unsupported tokens + workarounds, variants used
          html <file> [--ns N] [--view V] [-o f] HTML -> GotX C# skeleton
          gpui <file.rs|--sample> [--json]       GPUI style chains -> Tailwind classes + behaviour hints
          diff <ref.png> <gotx.png> [out.png]    pixel diff (tol 0 and 24), diff image, top regions
        """;

    public static int Run(string[] args, TextWriter o)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { o.WriteLine(Usage); return 0; }
        GotX.Components.ComponentTheme.Install(); // semantic colour tokens (bg-primary...) must resolve like in a real app
        var rest = args[1..];
        try
        {
            return args[0] switch
            {
                "classes" => ClassesCommand.Run(rest, o),
                "html" => HtmlCommand.Run(rest, o),
                "gpui" => GpuiCommand.Run(rest, o),
                "diff" => DiffCommand.Run(rest, o),
                _ => Fail(o, $"unknown command '{args[0]}'\n{Usage}"),
            };
        }
        catch (Exception e) when (e is IOException or ArgumentException or NotSupportedException or UnauthorizedAccessException)
        {
            return Fail(o, e.Message);
        }
    }

    static int Fail(TextWriter o, string msg) { o.WriteLine("error: " + msg); return 2; }

    /// <summary>Value of <c>--name value</c> (removed from the list), or null.</summary>
    public static string? Option(List<string> args, string name)
    {
        int i = args.IndexOf(name);
        if (i < 0 || i + 1 >= args.Count) return null;
        string v = args[i + 1];
        args.RemoveRange(i, 2);
        return v;
    }

    public static bool Flag(List<string> args, string name) => args.Remove(name);
}
