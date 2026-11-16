using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

/// <summary>
/// Bộ nhớ có trần: atlas glyph nở dần (512→2048), buffer instance co lại khi không dùng tới, texture ảnh bị đuổi theo ngân sách,
/// và hai con số để báo cáo (ước tính theo tài nguyên đã tạo + số liệu VRAM thật của DXGI).
/// </summary>
internal sealed unsafe partial class D3D11Renderer
{
    /// <summary>Tổng byte texture ảnh trên GPU tối đa; vượt thì đuổi ảnh lâu chưa vẽ nhất (không bao giờ đuổi ảnh đang vẽ ở frame này).</summary>
    public const long ImageBudgetBytes = 64L << 20;

    long _imageBytes;
    int _slackFrames;

    /// <summary>Ước tính byte tài nguyên GPU của renderer: atlas + buffer instance/vùng cắt/gradient + texture ảnh + đích offscreen.</summary>
    public long GpuBytes =>
        (long)_atlasSide * _atlasSide + (long)_instCap * sizeof(Instance) + (long)_shapeCap * sizeof(ShapeData) +
        (long)_gradCap * sizeof(GradData) + _imageBytes + (_retTex != null ? (long)_retW * _retH * 4 : 0) + (_offTex != null ? (long)_width * _height * 4 : 0);

    /// <summary>Instance buffer dùng chưa tới 1/4 dung lượng suốt 120 frame liên tiếp thì co về gấp đôi mức cần (tối thiểu 1024).</summary>
    void TrimBuffers(int prims, int shapes)
    {
        if (_instCap > 4096 && prims * 4 < _instCap)
        {
            if (++_slackFrames < 120) return;
            _slackFrames = 0;
            Release(ref _instSrv); Release(ref _instBuf);
            int cap = Math.Max(1024, prims * 2);
            _instCap = 0;
            EnsureInstanceCapacity(cap);
            _staging = new Instance[cap];
        }
        else _slackFrames = 0;
    }

    void EvictOverBudget()
    {
        while (_imageBytes > ImageBudgetBytes)
        {
            int oldest = 0, oldestFrame = int.MaxValue;
            foreach (var kv in _images)
                if (kv.Value.Frame != _frame && kv.Value.Frame < oldestFrame) { oldest = kv.Key; oldestFrame = kv.Value.Frame; }
            if (oldestFrame == int.MaxValue) return; // tất cả đang được vẽ ở frame này
            ReleaseImage(oldest);
        }
    }

    static readonly Guid IID_IDXGIDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
    static readonly Guid IID_IDXGIAdapter3 = new("645967a4-1392-4310-a798-8053ce3e93fd");

    /// <summary>
    /// Số liệu bộ nhớ video của tiến trình ở adapter (DXGI QueryVideoMemoryInfo): usage = đang dùng, budget = HĐH cho phép, theo nhóm local (VRAM) và
    /// non-local (RAM hệ thống dùng cho GPU). Mặc định (0,0,0,0) nếu không hỏi được.
    /// </summary>
    public (long LocalUsage, long LocalBudget, long NonLocalUsage, long NonLocalBudget) VideoMemory
    {
        get
        {
            if (_device == null) return default;
            void* dxgi = null, adapter = null, adapter3 = null;
            try
            {
                Guid id = IID_IDXGIDevice;
                if (QueryInterface(_device, &id, &dxgi) < 0 || GetAdapter(dxgi, &adapter) < 0) return default;
                id = IID_IDXGIAdapter3;
                if (QueryInterface(adapter, &id, &adapter3) < 0) return default;
                VIDEO_MEMORY_INFO local, far;
                if (QueryVideoMemoryInfo(adapter3, 0, 0, &local) < 0 || QueryVideoMemoryInfo(adapter3, 0, 1, &far) < 0) return default;
                return ((long)local.CurrentUsage, (long)local.Budget, (long)far.CurrentUsage, (long)far.Budget);
            }
            finally { Release(ref adapter3); Release(ref adapter); Release(ref dxgi); }
        }
    }
}
