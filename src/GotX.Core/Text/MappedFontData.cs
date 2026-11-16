using System.IO.MemoryMappedFiles;

namespace GotX.Text;

/// <summary>
/// Dữ liệu font ánh xạ bộ nhớ từ file (chỉ đọc). Font hệ thống lớn (Segoe UI Emoji ~13 MB) nạp bằng ReadAllBytes thì cả file nằm trong heap + working set
/// dù chỉ vài trang được chạm tới; ánh xạ file chỉ tốn các trang thật sự đọc (bảng cmap, glyph đã vẽ) và hệ điều hành thu hồi được.
/// Sống cùng tiến trình (font không bao giờ bị giải phóng), nên không có Dispose; trình xử lý cuối của handle tự giải phóng khi font bị thu gom.
/// </summary>
sealed unsafe class MappedFontData
{
    readonly MemoryMappedFile _file;
    readonly MemoryMappedViewAccessor _view;
    readonly byte* _ptr;
    readonly int _length;

    MappedFontData(MemoryMappedFile file, MemoryMappedViewAccessor view, byte* ptr, int length) { _file = file; _view = view; _ptr = ptr; _length = length; }

    public ReadOnlySpan<byte> Span => new(_ptr, _length);

    /// <summary>Ánh xạ file; null nếu không ánh xạ được (file rỗng / quá 2 GB / lỗi hệ thống): người gọi nạp bằng ReadAllBytes.</summary>
    public static MappedFontData? TryOpen(string path)
    {
        MemoryMappedFile? file = null;
        MemoryMappedViewAccessor? view = null;
        try
        {
            long size = new FileInfo(path).Length;
            if (size <= 0 || size > int.MaxValue) return null;
            file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            view = file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            byte* p = null;
            view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
            return new MappedFontData(file, view, p + view.PointerOffset, (int)size);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            view?.Dispose(); file?.Dispose();
            return null;
        }
    }
}
