using System.Buffers.Binary;
using System.IO.Compression;

namespace GotX;

/// <summary>
/// Giải mã PNG thành R,G,B,A 8 bit. Hỗ trợ: xám, xám+alpha, RGB, RGBA, bảng màu (kèm tRNS), độ sâu 1–16 bit.
/// Chưa hỗ trợ ảnh interlace (Adam7).
/// </summary>
public static class PngDecoder
{
    static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static byte[] Decode(ReadOnlySpan<byte> png, out int width, out int height)
    {
        if (png.Length < 8 || !png[..8].SequenceEqual(Signature)) throw new NotSupportedException("Không phải file PNG.");
        width = height = 0;
        int depth = 0, colorType = 0;
        byte[]? palette = null, trns = null;
        using var idat = new MemoryStream();
        for (int p = 8; p + 12 <= png.Length;)
        {
            int len = (int)BinaryPrimitives.ReadUInt32BigEndian(png[p..]);
            uint type = BinaryPrimitives.ReadUInt32BigEndian(png[(p + 4)..]);
            if (len < 0 || p + 12 + len > png.Length) throw new NotSupportedException("PNG bị cắt cụt.");
            var data = png.Slice(p + 8, len);
            switch (type)
            {
                case 0x49484452: // IHDR
                    width = (int)BinaryPrimitives.ReadUInt32BigEndian(data);
                    height = (int)BinaryPrimitives.ReadUInt32BigEndian(data[4..]);
                    depth = data[8]; colorType = data[9];
                    if (data[12] != 0) throw new NotSupportedException("PNG interlace chưa được hỗ trợ.");
                    break;
                case 0x504C5445: palette = data.ToArray(); break; // PLTE
                case 0x74524E53: trns = data.ToArray(); break;    // tRNS
                case 0x49444154: idat.Write(data); break;         // IDAT
            }
            p += 12 + len;
        }
        if (width <= 0 || height <= 0 || (long)width * height > 64L << 20) throw new NotSupportedException("Kích thước PNG không hợp lệ.");
        int channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => throw new NotSupportedException("Kiểu màu PNG lạ.") };
        int bpp = Math.Max(1, channels * depth / 8), stride = (width * channels * depth + 7) / 8;

        var raw = new byte[(stride + 1) * height];
        idat.Position = 0;
        using (var z = new ZLibStream(idat, CompressionMode.Decompress)) z.ReadExactly(raw);

        var rgba = new byte[width * height * 4];
        byte[] prev = new byte[stride], cur = new byte[stride];
        for (int y = 0; y < height; y++)
        {
            int filter = raw[y * (stride + 1)];
            raw.AsSpan(y * (stride + 1) + 1, stride).CopyTo(cur);
            Unfilter(filter, cur, prev, bpp);
            Expand(cur, rgba.AsSpan(y * width * 4, width * 4), width, depth, colorType, palette, trns);
            (prev, cur) = (cur, prev);
        }
        return rgba;
    }

    static void Unfilter(int filter, byte[] cur, byte[] prev, int bpp)
    {
        for (int i = 0; i < cur.Length; i++)
        {
            int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
            cur[i] = (byte)(cur[i] + filter switch
            {
                1 => a, 2 => b, 3 => (a + b) / 2,
                4 => Paeth(a, b, c),
                0 => 0,
                _ => throw new NotSupportedException("Bộ lọc PNG lạ."),
            });
        }
    }

    static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    static void Expand(byte[] row, Span<byte> dst, int width, int depth, int colorType, byte[]? palette, byte[]? trns)
    {
        // Mẫu thứ i của hàng, quy về 0..255 (bảng màu: giữ nguyên chỉ số)
        int Sample(int i)
        {
            if (depth == 8) return row[i];
            if (depth == 16) return row[2 * i];
            int bit = i * depth, v = (row[bit >> 3] >> (8 - depth - (bit & 7))) & ((1 << depth) - 1);
            return colorType == 3 ? v : v * 255 / ((1 << depth) - 1);
        }
        for (int x = 0; x < width; x++)
        {
            byte r, g, b, a = 255;
            switch (colorType)
            {
                case 0: r = g = b = (byte)Sample(x); break;
                case 4: r = g = b = (byte)Sample(2 * x); a = (byte)Sample(2 * x + 1); break;
                case 2: r = (byte)Sample(3 * x); g = (byte)Sample(3 * x + 1); b = (byte)Sample(3 * x + 2); break;
                case 6: r = (byte)Sample(4 * x); g = (byte)Sample(4 * x + 1); b = (byte)Sample(4 * x + 2); a = (byte)Sample(4 * x + 3); break;
                default:
                {
                    int idx = Sample(x);
                    if (palette is null || idx * 3 + 2 >= palette.Length) throw new NotSupportedException("PNG thiếu bảng màu.");
                    r = palette[idx * 3]; g = palette[idx * 3 + 1]; b = palette[idx * 3 + 2];
                    if (trns is not null && idx < trns.Length) a = trns[idx];
                    break;
                }
            }
            dst[4 * x] = r; dst[4 * x + 1] = g; dst[4 * x + 2] = b; dst[4 * x + 3] = a;
        }
    }
}
