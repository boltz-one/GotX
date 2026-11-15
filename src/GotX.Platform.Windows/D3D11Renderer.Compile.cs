using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class D3D11Renderer
{
    /// <summary>
    /// Biên dịch HLSL tốn ~100 ms (O3, hai entry point) – hơn một nửa thời gian từ lúc chạy tới frame đầu. Bytecode DXBC không phụ thuộc
    /// GPU/driver nên được cache ra đĩa theo hash của mã nguồn: lần chạy đầu biên dịch (vs và ps song song) rồi ghi file, các lần sau chỉ đọc file.
    /// File hỏng / không dùng được thì biên dịch lại và ghi đè.
    /// </summary>
    void CompileShaders()
    {
        byte[] src = Encoding.UTF8.GetBytes(GradientHlsl + Hlsl);
        string path = CachePath(src);
        if (TryLoadCached(path, out var cachedVs, out var cachedPs) && TryCreate(cachedVs, cachedPs))
        {
            PerfLog.Shared?.Mark("shader-cache", "hit");
            return;
        }
        // vs và ps biên dịch cùng lúc trên hai luồng: D3DCompile an toàn khi gọi song song, mỗi lần đều phải phân tích cả nguồn.
        byte[]? vsCode = null;
        Exception? error = null;
        var worker = new Thread(() => { try { vsCode = CompileToBytes(src, "vs"u8, "vs_5_0"u8); } catch (Exception ex) { error = ex; } }) { IsBackground = true };
        worker.Start();
        byte[] psCode = CompileToBytes(src, "ps"u8, "ps_5_0"u8);
        worker.Join();
        if (error is not null || vsCode is null) throw error ?? new InvalidOperationException("D3DCompile vs thất bại.");
        if (!TryCreate(vsCode, psCode)) throw new InvalidOperationException("Không tạo được shader từ bytecode vừa biên dịch.");
        TrySaveCached(path, vsCode, psCode);
        PerfLog.Shared?.Mark("shader-cache", "miss");
    }

    bool TryCreate(byte[] vsCode, byte[] psCode)
    {
        fixed (byte* v = vsCode) fixed (byte* p = psCode)
        {
            void* vs, ps;
            if (CreateVertexShader(_device, v, (nuint)vsCode.Length, null, &vs) < 0) return false;
            if (CreatePixelShader(_device, p, (nuint)psCode.Length, null, &ps) < 0) { Release(ref vs); return false; }
            _vs = vs; _ps = ps;
            return true;
        }
    }

    static string CachePath(byte[] src)
    {
        string hash = Convert.ToHexString(SHA256.HashData(src))[..24];
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GotX", "shader-cache");
        return Path.Combine(dir, $"d3d11-{hash}.bin");
    }

    static bool TryLoadCached(string path, out byte[] vs, out byte[] ps)
    {
        vs = ps = [];
        try
        {
            if (!File.Exists(path)) return false;
            var data = File.ReadAllBytes(path);
            if (data.Length < 12 || BitConverter.ToUInt32(data, 0) != Magic) return false;
            int vl = BitConverter.ToInt32(data, 4), pl = BitConverter.ToInt32(data, 8);
            if (vl <= 0 || pl <= 0 || 12L + vl + pl != data.Length) return false;
            vs = data.AsSpan(12, vl).ToArray();
            ps = data.AsSpan(12 + vl, pl).ToArray();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }

    static void TrySaveCached(string path, byte[] vs, byte[] ps)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string tmp = path + "." + Environment.ProcessId + ".tmp";
            using (var f = File.Create(tmp))
            {
                f.Write(BitConverter.GetBytes(Magic));
                f.Write(BitConverter.GetBytes(vs.Length));
                f.Write(BitConverter.GetBytes(ps.Length));
                f.Write(vs);
                f.Write(ps);
            }
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* cache là tối ưu hoá, không bắt buộc */ }
    }

    const uint Magic = 0x31425347; // "GSB1"

    static byte[] CompileToBytes(byte[] src, ReadOnlySpan<byte> entry, ReadOnlySpan<byte> target)
    {
        void* blob = Compile(src, entry, target);
        try { return new ReadOnlySpan<byte>(BlobPtr(blob), (int)BlobSize(blob)).ToArray(); }
        finally { Release(ref blob); }
    }

    static void* Compile(byte[] src, ReadOnlySpan<byte> entry, ReadOnlySpan<byte> target)
    {
        // Chuỗi kết thúc bằng 0 cho API C
        Span<byte> e = stackalloc byte[entry.Length + 1]; entry.CopyTo(e); e[^1] = 0;
        Span<byte> t = stackalloc byte[target.Length + 1]; target.CopyTo(t); t[^1] = 0;
        void* code = null, errors = null;
        int hr;
        fixed (byte* s = src) fixed (byte* pe = e) fixed (byte* pt = t)
            hr = D3DCompile(s, (nuint)src.Length, null, null, null, pe, pt, 1u << 15 /*O3*/, 0, &code, &errors);
        if (hr < 0)
        {
            string msg = errors != null ? Marshal.PtrToStringAnsi((nint)BlobPtr(errors)) ?? "" : "";
            Release(ref errors);
            throw new InvalidOperationException($"D3DCompile {Encoding.ASCII.GetString(entry)}: {msg}");
        }
        Release(ref errors);
        return code;
    }
}
