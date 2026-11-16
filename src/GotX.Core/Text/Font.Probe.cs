using System.Buffers.Binary;

namespace GotX.Text;

public sealed partial class Font
{
    /// <summary>
    /// Kiểm tra nhanh (chỉ đọc bảng thư mục, không nạp cả file) rằng file là TrueType glyf mà <see cref="Font"/> dùng được.
    /// Cho phép TextSystem đăng ký font mà chưa nạp dữ liệu: font chỉ vào RAM khi có chữ thật dùng tới.
    /// </summary>
    public static bool Probe(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            Span<byte> head = stackalloc byte[12];
            if (fs.Read(head) < 12) return false;
            if (BinaryPrimitives.ReadUInt32BigEndian(head) == 0x74746366) return false; // ttcf
            int n = BinaryPrimitives.ReadUInt16BigEndian(head[4..]);
            if (n is 0 or > 64) return false;
            Span<byte> dir = stackalloc byte[n * 16];
            if (fs.Read(dir) < dir.Length) return false;
            int found = 0;
            for (int i = 0; i < n; i++)
            {
                uint tag = BinaryPrimitives.ReadUInt32BigEndian(dir[(i * 16)..]);
                if (tag is 0x68656164 or 0x68686561 or 0x676C7966 or 0x6C6F6361 or 0x636D6170) found++; // head hhea glyf loca cmap
            }
            return found == 5;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
