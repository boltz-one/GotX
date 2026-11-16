using System.Diagnostics;

namespace GotX;

/// <summary>
/// Bộ đếm hot path của tiến trình (luồng UI duy nhất, nên chỉ cần <c>++</c> thường): Window chụp trước/sau mỗi frame để ra
/// số liệu theo frame. Chỉ tăng ở nhánh trượt cache (đã đắt) hoặc là một phép cộng số nguyên, nên bật hay tắt nhật ký đều không đổi chi phí.
/// </summary>
public static class PerfCounters
{
    public static long WrapHit, WrapMiss, WrapTicks;
    public static long GlyphRaster, GlyphRasterTicks, GlyphBytes;
    public static long ClassMiss, ClassMissTicks;
    public static long CombineBuild; // số ClassList ghép mới (Tailwind.Combine trượt cả hai thế hệ cache)
    public static long Measure, MeasureMemoHit;
    public static long AtlasUpload, AtlasUploadBytes, AtlasUploadTicks;

    /// <summary>Ảnh chụp các bộ đếm: hiệu hai ảnh = số liệu của khoảng giữa.</summary>
    public readonly record struct Snapshot(
        long WrapHit, long WrapMiss, long WrapTicks, long GlyphRaster, long GlyphRasterTicks, long ClassMiss, long ClassMissTicks,
        long Measure, long MeasureMemoHit, long AtlasUpload, long AtlasUploadBytes, long AtlasUploadTicks)
    {
        public static Snapshot Now() => new(PerfCounters.WrapHit, PerfCounters.WrapMiss, PerfCounters.WrapTicks,
            PerfCounters.GlyphRaster, PerfCounters.GlyphRasterTicks, PerfCounters.ClassMiss, PerfCounters.ClassMissTicks,
            PerfCounters.Measure, PerfCounters.MeasureMemoHit, PerfCounters.AtlasUpload, PerfCounters.AtlasUploadBytes, PerfCounters.AtlasUploadTicks);

        public static Snapshot operator -(Snapshot a, Snapshot b) => new(a.WrapHit - b.WrapHit, a.WrapMiss - b.WrapMiss, a.WrapTicks - b.WrapTicks,
            a.GlyphRaster - b.GlyphRaster, a.GlyphRasterTicks - b.GlyphRasterTicks, a.ClassMiss - b.ClassMiss, a.ClassMissTicks - b.ClassMissTicks,
            a.Measure - b.Measure, a.MeasureMemoHit - b.MeasureMemoHit, a.AtlasUpload - b.AtlasUpload, a.AtlasUploadBytes - b.AtlasUploadBytes,
            a.AtlasUploadTicks - b.AtlasUploadTicks);
    }

    public static double TicksToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
}
