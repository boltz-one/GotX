using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using GotX.Raster;
using GotX.Text;
using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class Win32Window
{
    static string CompositionString(nint himc, uint index)
    {
        int bytes = ImmGetCompositionStringW(himc, index, null, 0);
        if (bytes <= 0) return "";
        var buf = new char[bytes / 2];
        fixed (char* p = buf) ImmGetCompositionStringW(himc, index, p, (uint)bytes);
        return new string(buf);
    }

    /// <summary>Đặt cửa sổ gợi ý của IME ngay dưới con trỏ nhập liệu.</summary>
    void PositionIme()
    {
        if (ImeCaret?.Invoke() is not { } c) return;
        nint himc = ImmGetContext(_hwnd);
        if (himc == 0) return;
        float s = ScaleFactor;
        int x = (int)(c.X * s), y = (int)(c.Y * s), h = (int)(c.Height * s);
        var cf = new COMPOSITIONFORM { dwStyle = CFS_POINT, ptCurrentPos = new POINT { x = x, y = y } };
        ImmSetCompositionWindow(himc, &cf);
        var cand = new CANDIDATEFORM
        {
            dwIndex = 0, dwStyle = CFS_EXCLUDE,
            ptCurrentPos = new POINT { x = x, y = y + h },
            rcArea = new RECT { left = x, top = y, right = x + 1, bottom = y + h },
        };
        ImmSetCandidateWindow(himc, &cand);
        ImmReleaseContext(_hwnd, himc);
    }
}
