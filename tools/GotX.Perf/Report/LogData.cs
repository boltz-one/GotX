using System.Text.Json;

namespace GotX.Perf;

/// <summary>One "frame" record, merged with its "present" record (when the platform logged one).</summary>
public sealed class FrameRec
{
    public int N, Win, Rend, Repl, Nodes, Prim, Clips, Grad, Hit, Hov, WrapHit, WrapMiss, GlyphN, ClsMiss, Meas, Memo;
    public double Wall, Dt, Draw, Layout, Paint, Rv, Wrap, Glyph, Cls, Lat;
    public long Alloc;
    public int Gc0, Gc1, Gc2, W, H;
    public bool Slow, Anim;
    /// <summary>Rendered view names with their Render() cost in microseconds (top 5, or all at trace level).</summary>
    public List<(string Name, long Us)> Views = new();
    public List<string> Cause = new(), Dirty = new(), Clear = new();
    public List<(string Kind, int Count)> Input = new();
    public double WsMb, HeapMb, AtlasKb;
    public bool HasPresent;
    public double Present, Build, Submit, Flip, Gpu, Inst, Map;
    public long UpBytes;
    /// <summary>GPU counters of the frame (D3D11 only): draw calls, pipeline switches, texture switches, instance bytes, atlas hit rate, CPU encode ms.</summary>
    public double Draws, Pipe, TexSw, InstB, AtlasHit, Enc;
    /// <summary>Name of the last "mark" before this frame (scenario phase), empty if none.</summary>
    public string Phase = "";
}

public sealed record WarnRec(double Wall, string Kind, string Msg, int Frame);
public sealed record StartRec(string What, double Ms);
public sealed record MarkRec(double Wall, string Name, string? Detail);

/// <summary>A whole JSONL perf log, parsed.</summary>
public sealed class LogData
{
    public string Path = "";
    public List<FrameRec> Frames = new();
    public List<WarnRec> Warns = new();
    public List<StartRec> Starts = new();
    public List<MarkRec> Marks = new();
    public int BadLines;

    public static LogData Read(string path)
    {
        var data = new LogData { Path = path };
        var byFrame = new Dictionary<(int, int), FrameRec>();
        string phase = "";
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(fs);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var r = doc.RootElement;
                switch (r.GetProperty("t").GetString())
                {
                    case "frame":
                    {
                        var f = ParseFrame(r);
                        f.Phase = phase;
                        data.Frames.Add(f);
                        byFrame[(f.Win, f.N)] = f;
                        break;
                    }
                    case "present":
                        if (byFrame.TryGetValue((Int(r, "win"), Int(r, "n")), out var pf))
                        {
                            pf.HasPresent = true; pf.Present = Dbl(r, "ms"); pf.Build = Dbl(r, "build"); pf.Submit = Dbl(r, "submit");
                            pf.Flip = Dbl(r, "flip"); pf.Gpu = Dbl(r, "gpu"); pf.Inst = Dbl(r, "inst"); pf.Map = Dbl(r, "map"); pf.UpBytes = (long)Dbl(r, "upBytes");
                            pf.Draws = Dbl(r, "draws"); pf.Pipe = Dbl(r, "pipe"); pf.TexSw = Dbl(r, "texsw"); pf.InstB = Dbl(r, "instB"); pf.AtlasHit = Dbl(r, "atlasHit"); pf.Enc = Dbl(r, "enc");
                        }
                        break;
                    case "warn": data.Warns.Add(new WarnRec(Dbl(r, "wall"), Str(r, "kind"), Str(r, "msg"), Int(r, "n"))); break;
                    case "start": data.Starts.Add(new StartRec(Str(r, "what"), Dbl(r, "ms"))); break;
                    case "mark":
                        phase = Str(r, "name");
                        data.Marks.Add(new MarkRec(Dbl(r, "wall"), phase, r.TryGetProperty("detail", out var d) ? d.GetString() : null));
                        break;
                }
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) { data.BadLines++; }
        }
        return data;
    }

    static FrameRec ParseFrame(JsonElement r)
    {
        var f = new FrameRec
        {
            N = Int(r, "n"), Win = Int(r, "win"), Wall = Dbl(r, "wall"), Dt = Dbl(r, "dt"), Draw = Dbl(r, "draw"), Layout = Dbl(r, "layout"),
            Paint = Dbl(r, "paint"), Rv = Dbl(r, "rv"), Wrap = Dbl(r, "wrap"), WrapHit = Int(r, "wrapHit"), WrapMiss = Int(r, "wrapMiss"),
            Glyph = Dbl(r, "glyph"), GlyphN = Int(r, "glyphN"), ClsMiss = Int(r, "clsMiss"), Cls = Dbl(r, "cls"), Meas = Int(r, "meas"),
            Memo = Int(r, "memo"), Rend = Int(r, "rend"), Repl = Int(r, "repl"), Nodes = Int(r, "nodes"), Prim = Int(r, "prim"),
            Clips = Int(r, "clips"), Grad = Int(r, "grad"), Hit = Int(r, "hit"), Hov = Int(r, "hov"), Alloc = (long)Dbl(r, "alloc"),
            Lat = Dbl(r, "lat"), Slow = Int(r, "slow") == 1, Anim = Int(r, "anim") == 1, W = Int(r, "w"), H = Int(r, "h"),
            WsMb = Dbl(r, "wsMB"), HeapMb = Dbl(r, "heapMB"), AtlasKb = Dbl(r, "atlasKB"),
        };
        if (r.TryGetProperty("gc", out var gc) && gc.GetArrayLength() == 3) { f.Gc0 = gc[0].GetInt32(); f.Gc1 = gc[1].GetInt32(); f.Gc2 = gc[2].GetInt32(); }
        if (r.TryGetProperty("views", out var v))
            for (int i = 0; i + 1 < v.GetArrayLength(); i += 2) f.Views.Add((v[i].GetString() ?? "?", v[i + 1].GetInt64()));
        Strings(r, "cause", f.Cause); Strings(r, "dirty", f.Dirty); Strings(r, "clr", f.Clear);
        if (r.TryGetProperty("in", out var inp))
            for (int i = 0; i + 1 < inp.GetArrayLength(); i += 2) f.Input.Add((inp[i].GetString() ?? "?", inp[i + 1].GetInt32()));
        return f;
    }

    static void Strings(JsonElement r, string key, List<string> into)
    {
        if (!r.TryGetProperty(key, out var a)) return;
        foreach (var s in a.EnumerateArray()) into.Add(s.GetString() ?? "");
    }

    static int Int(JsonElement r, string k) => r.TryGetProperty(k, out var v) ? (int)v.GetDouble() : 0;
    static double Dbl(JsonElement r, string k) => r.TryGetProperty(k, out var v) ? v.GetDouble() : 0;
    static string Str(JsonElement r, string k) => r.TryGetProperty(k, out var v) ? v.GetString() ?? "" : "";
}
