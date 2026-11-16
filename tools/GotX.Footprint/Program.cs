using GotX.Footprint;

// run [--scenarios a,b] [--renderer d3d|software|both] [--repeat 3] [--seconds 10] [--host exe --mode aot] [--check] [--update-baseline] [--shots]
// host <scenario> --renderer d3d|software   (internal: child process)
if (args.Length > 0 && args[0] == "host") return HostCommand.Run(args[1..]);
if (args.Length > 0 && args[0] == "run") return Runner.Run(args[1..]);
if (args.Length > 1 && args[0] == "baseline") return Baseline.FromJson(args[1]);
Console.Error.WriteLine("usage: GotX.Footprint run [--scenarios hello,home,input,vlist,spin,dialog,resize] [--renderer d3d|software|both] [--repeat 3] [--seconds 10]");
Console.Error.WriteLine("         [--host <exe>] [--mode jit|aot] [--out dir] [--exe-size scenario=path,..] [--check] [--update-baseline] [--shots]");
Console.Error.WriteLine("       GotX.Footprint baseline <out/footprint-*.json>   (merge a finished run into footprint-baseline.json)");
return 2;

static class Args
{
    public static string? Get(string[] a, string name) { int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
}
