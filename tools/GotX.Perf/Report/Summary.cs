namespace GotX.Perf;

/// <summary>Aggregate numbers over a set of frames (a whole log or one scenario segment).</summary>
public sealed class Summary
{
    public int Frames;
    public Stat Draw, Layout, Paint, RenderViews, Wrap, Glyph, Present, Gpu, Flip, Alloc, Interval, Latency, Build, Submit, Inst, Map;
    public double RendPerFrame, ReplPerFrame, WrapHitRate, MeasMemoRate;
    public long TotalAlloc, WrapMiss, GlyphN, UpBytes;
    public int Slow, SlowLatency, ZeroAllocFrames, Gc0, Gc1, Gc2;
    public double TotalDraw;

    public static Summary Of(IReadOnlyList<FrameRec> frames)
    {
        var s = new Summary { Frames = frames.Count };
        if (frames.Count == 0) return s;
        List<double> draw = new(), layout = new(), paint = new(), rv = new(), wrap = new(), glyph = new(), present = new(), gpu = new(),
            flip = new(), alloc = new(), dt = new(), lat = new(), build = new(), submit = new(), inst = new(), map = new();
        long wrapHit = 0, wrapMiss = 0, meas = 0, memo = 0, rend = 0, repl = 0;
        foreach (var f in frames)
        {
            draw.Add(f.Draw); layout.Add(f.Layout); paint.Add(f.Paint); rv.Add(f.Rv); wrap.Add(f.Wrap); glyph.Add(f.Glyph);
            if (f.HasPresent && f.Build > 0) { build.Add(f.Build); submit.Add(f.Submit); if (f.Inst > 0) { inst.Add(f.Inst); map.Add(f.Map); } }
            if (f.HasPresent) { present.Add(f.Present); if (f.Gpu > 0) gpu.Add(f.Gpu); if (f.Flip > 0) flip.Add(f.Flip); }
            alloc.Add(f.Alloc);
            if (f.Dt > 0) dt.Add(f.Dt);
            if (f.Lat > 0) lat.Add(f.Lat);
            wrapHit += f.WrapHit; wrapMiss += f.WrapMiss; meas += f.Meas; memo += f.Memo; rend += f.Rend; repl += f.Repl;
            s.TotalAlloc += f.Alloc; s.GlyphN += f.GlyphN; s.UpBytes += f.UpBytes;
            if (f.Slow) s.Slow++;
            if (f.Lat > 50) s.SlowLatency++;
            if (f.Alloc == 0) s.ZeroAllocFrames++;
            s.Gc0 += f.Gc0; s.Gc1 += f.Gc1; s.Gc2 += f.Gc2;
        }
        s.Draw = Stat.Of(draw); s.Layout = Stat.Of(layout); s.Paint = Stat.Of(paint); s.RenderViews = Stat.Of(rv);
        s.Wrap = Stat.Of(wrap); s.Glyph = Stat.Of(glyph); s.Present = Stat.Of(present); s.Gpu = Stat.Of(gpu); s.Flip = Stat.Of(flip); s.Build = Stat.Of(build); s.Submit = Stat.Of(submit); s.Inst = Stat.Of(inst); s.Map = Stat.Of(map);
        s.Alloc = Stat.Of(alloc); s.Interval = Stat.Of(dt); s.Latency = Stat.Of(lat);
        s.TotalDraw = s.Draw.Sum;
        s.RendPerFrame = rend / (double)frames.Count; s.ReplPerFrame = repl / (double)frames.Count;
        s.WrapMiss = wrapMiss;
        s.WrapHitRate = wrapHit + wrapMiss == 0 ? 1 : wrapHit / (double)(wrapHit + wrapMiss);
        s.MeasMemoRate = meas == 0 ? 1 : memo / (double)meas;
        return s;
    }
}
