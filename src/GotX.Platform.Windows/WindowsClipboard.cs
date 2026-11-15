using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

/// <summary>Clipboard của Windows, chỉ định dạng văn bản Unicode (CF_UNICODETEXT).</summary>
internal sealed unsafe class WindowsClipboard : IClipboard
{
    public string? GetText()
    {
        if (OpenClipboard(0) == 0) return null; // tiến trình khác đang giữ clipboard
        try
        {
            nint handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == 0) return null;
            char* p = (char*)GlobalLock(handle);
            if (p is null) return null;
            try
            {
                // Chuỗi kết thúc bằng '\0' nhưng không tin tuyệt đối: giới hạn theo kích thước khối nhớ.
                int max = (int)Math.Min(int.MaxValue, GlobalSize(handle) / sizeof(char));
                int len = 0;
                while (len < max && p[len] != '\0') len++;
                return new string(p, 0, len);
            }
            finally { GlobalUnlock(handle); }
        }
        finally { CloseClipboard(); }
    }

    public void SetText(string text)
    {
        if (OpenClipboard(0) == 0) return;
        try
        {
            EmptyClipboard();
            nuint bytes = (nuint)((text.Length + 1) * sizeof(char));
            nint handle = GlobalAlloc(GMEM_MOVEABLE, bytes);
            if (handle == 0) return;
            char* p = (char*)GlobalLock(handle);
            if (p is null) { GlobalFree(handle); return; }
            text.AsSpan().CopyTo(new Span<char>(p, text.Length));
            p[text.Length] = '\0';
            GlobalUnlock(handle);
            // Thành công thì hệ thống sở hữu khối nhớ; thất bại thì ta phải tự giải phóng.
            if (SetClipboardData(CF_UNICODETEXT, handle) == 0) GlobalFree(handle);
        }
        finally { CloseClipboard(); }
    }
}
