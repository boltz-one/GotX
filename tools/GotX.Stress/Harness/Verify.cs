using GotX.Raster;

namespace GotX.Stress;

/// <summary>STRESS_VERIFY=1: after every frame the retained (damage / scroll-blit) canvas must equal a whole-frame raster of the same scene, pixel for pixel.</summary>
public static class Verify
{
    public static readonly bool On = Environment.GetEnvironmentVariable("STRESS_VERIFY") == "1";
    static readonly Canvas Full = new();
    public static int Frames, Bad, Shifted; public static double ShiftCover, RegionSum;

    public static void Check(Scene scene, Canvas retained, in SceneDamage.Result dr, string label)
    {
        Frames++;
        if (dr.HasShift) { Shifted++; ShiftCover += dr.Coverage; RegionSum += dr.ShiftRect.Width * dr.ShiftRect.Height / Math.Max(1.0, retained.Width * (double)retained.Height); }
        Full.Resize(retained.Width, retained.Height);
        SoftwareRenderer.Render(scene, Full);
        int n = retained.Width * retained.Height;
        for (int i = 0; i < n; i++)
            if (Full.Pixels[i] != retained.Pixels[i])
            {
                if (Bad++ < 5) Console.WriteLine($"    VERIFY MISMATCH {label} frame {Frames} at ({i % retained.Width},{i / retained.Width}) got {retained.Pixels[i]:X8} want {Full.Pixels[i]:X8} shift={dr.ShiftDy} cover={dr.Coverage:0.00}");
                return;
            }
    }
}
