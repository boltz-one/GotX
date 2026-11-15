using GotX.Perf;

const string Usage = """
    GotX.Perf - perf log reader and benchmark runner
      report <log.jsonl> [--top N] [--phases]     summary of one log (GOTX_LOG=<path> produces it)
      compare <a.jsonl> <b.jsonl>                 what changed between two logs
      bench [--repeat N] [--only a,b] [--update-baseline] [--baseline file] [--out dir] [--raster] [--verbose]
                                                  scripted headless scenarios vs tools/GotX.Perf/perf-baseline.json
      profile-raster <target> [--scroll N]        per-primitive CPU raster cost of the final scene
      real <shadcn|gallery|path-to.exe> [--seconds N] [--out dir]
                                                  drive a real D3D11 window with posted input, then report
    """;

if (args.Length == 0) { Console.WriteLine(Usage); return 2; }
switch (args[0])
{
    case "report": return ReportCommand.Run(args[1..]);
    case "compare": return CompareCommand.Run(args[1..]);
    case "bench": return BenchCommand.Run(args[1..]);
    case "run-target": return ChildRun.Run(args[1..]);
    case "real": return RealWindowCommand.Run(args[1..]);
    case "profile-raster": return RasterProfile.Run(args[1..]);
    default: Console.WriteLine(Usage); return 2;
}
