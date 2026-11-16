namespace GotX;

/// <summary>Số liệu GPU của một frame do renderer D3D11 điền (renderer CPU để 0). Chỉ để quan sát: không ảnh hưởng hình vẽ.</summary>
public struct GpuFrameCounters
{
    /// <summary>Số lệnh DrawInstanced.</summary>
    public int DrawCalls;
    /// <summary>Số lần đổi state pipeline (shader, blend, raster, sampler, scissor).</summary>
    public int PipelineSwitches;
    /// <summary>Số lần gắn lại texture ảnh giữa các lệnh vẽ.</summary>
    public int TextureSwitches;
    /// <summary>Byte instance + vùng cắt + gradient ghi lên GPU.</summary>
    public long InstanceBytes;
    /// <summary>Glyph trúng / trượt atlas GPU (trượt = phải tải mask lên).</summary>
    public int AtlasHits, AtlasMisses;
    /// <summary>Thời gian CPU dựng instance + ghi lệnh (ms), chưa tính Present.</summary>
    public double EncodeMs;
    /// <summary>Thời gian GPU (ms) của một frame gần đây, 0 nếu không đo được.</summary>
    public double GpuMs;
    /// <summary>Số primitive đã gửi (sau khi lọc theo vùng hỏng nếu có).</summary>
    public int Primitives;

    public readonly double AtlasHitRate => AtlasHits + AtlasMisses == 0 ? 1 : AtlasHits / (double)(AtlasHits + AtlasMisses);
}

/// <summary>Bộ đếm GPU toàn tiến trình (một luồng UI): <see cref="Last"/> là frame vừa vẽ, phần còn lại cộng dồn.</summary>
public static class GpuCounters
{
    public static GpuFrameCounters Last;
    public static long Frames, Presents, DrawCalls, InstanceBytes, DeviceRecoveries, SoftwareFallbacks;
    /// <summary>Frame bỏ qua hoàn toàn nhờ vùng hỏng rỗng (không encode, không present).</summary>
    public static long Skipped;

    public static void Commit(in GpuFrameCounters f, bool presented)
    {
        Last = f;
        Frames++;
        if (presented) Presents++;
        DrawCalls += f.DrawCalls;
        InstanceBytes += f.InstanceBytes;
    }
}
