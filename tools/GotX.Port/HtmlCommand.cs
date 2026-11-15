namespace GotX.Port;

/// <summary><c>html &lt;file&gt;</c>: HTML to GotX C# skeleton on stdout or <c>-o file</c>.</summary>
public static class HtmlCommand
{
    public static int Run(string[] argv, TextWriter o)
    {
        var args = argv.ToList();
        string ns = PortApp.Option(args, "--ns") ?? "Ported";
        string view = PortApp.Option(args, "--view") ?? "PortedView";
        string? outFile = PortApp.Option(args, "-o");
        if (args.Count != 1) { o.WriteLine("usage: html <file|-> [--ns Name] [--view Class] [-o out.cs]"); return 0; }
        string html = args[0] == "-" ? Console.In.ReadToEnd() : File.ReadAllText(args[0]);
        string cs = HtmlToCs.Generate(html, ns, view);
        if (outFile is null) o.Write(cs);
        else
        {
            File.WriteAllText(outFile, cs, new System.Text.UTF8Encoding(false));
            o.WriteLine($"wrote {outFile} ({cs.Count(c => c == '\n')} lines)");
        }
        return 0;
    }
}
