using System.Buffers.Binary;
using System.IO.Compression;

namespace GotX.Raster;

/// <summary>Ghi Canvas ra PNG (RGBA 8-bit) chỉ bằng BCL – dùng cho snapshot test và chạy headless.</summary>
public static class PngWriter
{
    static readonly uint[] CrcTable = BuildCrcTable();

    public static void Write(string path, Canvas canvas)
    {
        using var fs = File.Create(path);
        Write(fs, canvas);
    }

    public static void Write(Stream output, Canvas c)
    {
        output.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, c.Width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..], c.Height);
        ihdr[8] = 8; ihdr[9] = 6; // 8 bit, RGBA
        Chunk(output, "IHDR"u8, ihdr);

        using var raw = new MemoryStream();
        using (var z = new ZLibStream(raw, CompressionLevel.Optimal, leaveOpen: true))
        {
            var row = new byte[1 + c.Width * 4];
            for (int y = 0; y < c.Height; y++)
            {
                row[0] = 0; // filter: none
                for (int x = 0; x < c.Width; x++)
                {
                    uint p = c.Pixels[y * c.Width + x];
                    byte a = (byte)(p >> 24);
                    float un = a == 0 ? 0 : 255f / a; // bỏ premultiply
                    int o = 1 + x * 4;
                    row[o] = (byte)Math.Min(255, ((p >> 16) & 0xFF) * un + 0.5f);
                    row[o + 1] = (byte)Math.Min(255, ((p >> 8) & 0xFF) * un + 0.5f);
                    row[o + 2] = (byte)Math.Min(255, (p & 0xFF) * un + 0.5f);
                    row[o + 3] = a;
                }
                z.Write(row);
            }
        }
        Chunk(output, "IDAT"u8, raw.ToArray());
        Chunk(output, "IEND"u8, ReadOnlySpan<byte>.Empty);
    }

    static void Chunk(Stream s, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf, data.Length);
        s.Write(buf);
        s.Write(type);
        s.Write(data);
        uint crc = Crc(0xFFFFFFFF, type);
        crc = Crc(crc, data) ^ 0xFFFFFFFF;
        BinaryPrimitives.WriteUInt32BigEndian(buf, crc);
        s.Write(buf);
    }

    static uint Crc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (byte b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    static uint[] BuildCrcTable()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }
}
