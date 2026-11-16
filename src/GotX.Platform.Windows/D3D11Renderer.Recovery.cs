using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

/// <summary>Thiết bị D3D11 đã mất (removed / reset / hung / driver lỗi): Render bắt và dựng lại.</summary>
internal sealed class DeviceLostException(int hr, string what = "", int reason = 0) : Exception($"Mất thiết bị D3D11 ở {what}: HRESULT 0x{hr:X8}, lý do thiết bị 0x{reason:X8}")
{
    public int Hr { get; } = hr;
}

internal sealed unsafe partial class D3D11Renderer
{
    /// <summary>Số lần dựng lại liên tiếp (không có frame thành công xen giữa) tối đa trước khi bỏ cuộc và để cửa sổ chuyển sang renderer CPU.</summary>
    public const int MaxConsecutiveFailures = 3;

    int _failures;
    /// <summary>true khi dựng lại thất bại quá <see cref="MaxConsecutiveFailures"/> lần liên tiếp: người gọi phải đổi sang SoftwareRenderer.</summary>
    public bool Failed { get; private set; }
    /// <summary>Số lần dựng lại thiết bị thành công từ lúc tạo.</summary>
    public int Recoveries { get; private set; }

    // Test seam: tiêm HRESULT lỗi vào Present / ResizeBuffers (ví dụ DXGI_ERROR_DEVICE_REMOVED) để thử đường dựng lại bằng thiết bị thật.
    // Các lần dựng lại và vẽ sau đó là thật; chỉ phần "báo lỗi" là giả lập vì không thể ép driver thật rút thiết bị.
    internal int InjectHr { get; set; }
    internal int InjectCount { get; set; }

    int Inject(int hr)
    {
        if (InjectCount <= 0) return hr;
        InjectCount--;
        return InjectHr;
    }

    /// <summary>Kiểm tra HRESULT trong đường vẽ: lỗi mất thiết bị (hoặc lý do thiết bị bị gỡ) → DeviceLostException, lỗi khác → InvalidOperationException.</summary>
    void CheckDev(int hr, string what)
    {
        if (hr >= 0) return;
        int why = _device != null ? GetDeviceRemovedReason(_device) : 0;
        if (IsDeviceLost(hr) || why < 0) throw new DeviceLostException(hr, what, why);
        Check(hr, what);
    }

    /// <summary>
    /// Dựng lại thiết bị, swap chain, shader, buffer; atlas glyph/ảnh trên GPU trống và được nạp lại theo nhu cầu (mask nằm ở GlyphAtlas phía CPU
    /// nên chỉ số glyph của cache view vẫn đúng – không cần đổi Generation của GlyphAtlas, chỉ Generation phía GPU bị làm hết hiệu lực).
    /// </summary>
    string _lastLoss = "";

    bool Recover(int hr, string detail = "")
    {
        _lastLoss = detail;
        if (++_failures > MaxConsecutiveFailures) { Failed = true; return false; }
        ReleaseAll();
        try
        {
            Init(_hwnd, _width, _height);
            Recoveries++;
            GpuCounters.DeviceRecoveries++;
            PerfLog.Shared?.Warn("device-lost", $"0x{hr:X8} recovered: {_lastLoss}", Recoveries);
            return true;
        }
        catch (Exception ex)
        {
            ReleaseAll();
            Console.Error.WriteLine($"Dựng lại D3D11 thất bại: {ex.Message}");
            if (_failures >= MaxConsecutiveFailures) Failed = true;
            return false;
        }
    }
}
