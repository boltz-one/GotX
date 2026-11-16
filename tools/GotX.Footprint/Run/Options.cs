using System.Reflection;

namespace GotX.Footprint;

/// <summary>Command line of <c>run</c>. Defaults: every scenario, both renderers, 3 repeats, 10 s measured after a 3 s warm-up, JIT host built next to this tool.</summary>
sealed class Options
{
    public string[] Scenarios, Renderers;
    public int Repeat;
    public double Seconds;
    public string HostExe, Mode, OutDir, Root;
    public string[] HostArgs = [];
    public bool Check, UpdateBaseline, Shots;
    /// <summary>scenario → exe size in MB (Native AOT runs).</summary>
    public Dictionary<string, double> ExeMB = new();

    public Options(string[] a)
    {
        Scenarios = (Args.Get(a, "--scenarios") ?? string.Join(',', Scenes.Names)).Split(',');
        string r = Args.Get(a, "--renderer") ?? "both";
        Renderers = r == "both" ? ["d3d", "software"] : [r];
        Repeat = int.Parse(Args.Get(a, "--repeat") ?? "3");
        Seconds = double.Parse(Args.Get(a, "--seconds") ?? "10", System.Globalization.CultureInfo.InvariantCulture);
        Mode = Args.Get(a, "--mode") ?? "jit";
        Check = a.Contains("--check"); UpdateBaseline = a.Contains("--update-baseline"); Shots = a.Contains("--shots");
        Root = FindRoot();
        OutDir = Path.GetFullPath(Args.Get(a, "--out") ?? Path.Combine(Root, "tools", "GotX.Footprint", "out"));
        HostExe = Args.Get(a, "--host") ?? Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe");
        if (!File.Exists(HostExe)) throw new FileNotFoundException("host executable not found (build the tool first, or pass --host)", HostExe);
        if (Mode == "aot")
            foreach (string s in Scenarios) ExeMB[s] = new FileInfo(HostExe).Length / 1048576.0;
        // --exe-size hello=C:\path\Hello.exe,home=C:\path\Shadcn.exe : size of the matching standalone sample publish
        foreach (string pair in (Args.Get(a, "--exe-size") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            int i = pair.IndexOf('=');
            ExeMB[pair[..i]] = new FileInfo(pair[(i + 1)..]).Length / 1048576.0;
        }
    }

    /// <summary>Repo root = nearest parent holding GotX.slnx (falls back to the working directory).</summary>
    static string FindRoot()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "GotX.slnx"))) return d.FullName;
        for (var d = new DirectoryInfo(Environment.CurrentDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "GotX.slnx"))) return d.FullName;
        return Environment.CurrentDirectory;
    }

    public string BaselinePath => Path.Combine(Root, "tools", "GotX.Footprint", "footprint-baseline.json");
}
