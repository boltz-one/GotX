using System.Runtime.InteropServices;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class D3D11Renderer
{
    // Đo thời gian thực thi trên GPU bằng timestamp query. Kết quả đọc lại sau vài frame (vòng 4 ô) để không bắt GPU đồng bộ.
    const int GpuRing = 4;
    readonly void*[] _qDisjoint = new void*[GpuRing], _qBegin = new void*[GpuRing], _qEnd = new void*[GpuRing];
    readonly bool[] _qPending = new bool[GpuRing];
    int _qSlot;
    bool _gpuTimer;

    /// <summary>Thời gian GPU (ms) của một frame gần đây; 0 nếu không đo được (WARP, driver không hỗ trợ).</summary>
    public double GpuMs { get; private set; }

    void InitGpuTimer()
    {
        try
        {
            for (int i = 0; i < GpuRing; i++)
            {
                var d = new QUERY_DESC { Query = 3 /*TIMESTAMP_DISJOINT*/ };
                var t = new QUERY_DESC { Query = 2 /*TIMESTAMP*/ };
                void* q;
                if (CreateQuery(_device, &d, &q) < 0) return; _qDisjoint[i] = q;
                if (CreateQuery(_device, &t, &q) < 0) return; _qBegin[i] = q;
                if (CreateQuery(_device, &t, &q) < 0) return; _qEnd[i] = q;
            }
            _gpuTimer = true;
            PerfLog.Shared?.Mark("gpu-timer", "ok");
        }
        catch (Exception) { _gpuTimer = false; }
        if (!_gpuTimer) PerfLog.Shared?.Mark("gpu-timer", "unavailable");
    }

    void GpuBegin()
    {
        if (!_gpuTimer || _qPending[_qSlot]) return;
        QueryBegin(_ctx, _qDisjoint[_qSlot]);
        QueryEnd(_ctx, _qBegin[_qSlot]);
    }

    /// <summary>Đóng đoạn đo của frame này và thu kết quả của frame cũ nhất đã xong.</summary>
    void GpuEnd()
    {
        if (!_gpuTimer) return;
        if (!_qPending[_qSlot])
        {
            QueryEnd(_ctx, _qEnd[_qSlot]);
            QueryEnd(_ctx, _qDisjoint[_qSlot]);
            _qPending[_qSlot] = true;
        }
        for (int k = 1; k <= GpuRing; k++)
        {
            int s = (_qSlot + k) % GpuRing;
            if (!_qPending[s]) continue;
            QUERY_DISJOINT dj;
            if (QueryData(_ctx, _qDisjoint[s], &dj, (uint)sizeof(QUERY_DISJOINT), 1) != 0) break; // chưa xong
            ulong b, e;
            if (dj.Disjoint == 0 && QueryData(_ctx, _qBegin[s], &b, 8, 1) == 0 && QueryData(_ctx, _qEnd[s], &e, 8, 1) == 0)
                GpuMs = (e - b) * 1000.0 / dj.Frequency;
            _qPending[s] = false;
        }
        _qSlot = (_qSlot + 1) % GpuRing;
    }

    void ReleaseGpuTimer()
    {
        for (int i = 0; i < GpuRing; i++) { Release(ref _qDisjoint[i]); Release(ref _qBegin[i]); Release(ref _qEnd[i]); }
    }
}
