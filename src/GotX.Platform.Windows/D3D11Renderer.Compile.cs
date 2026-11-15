using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class D3D11Renderer
{
    void CompileShaders()
    {
        byte[] src = Encoding.UTF8.GetBytes(GradientHlsl + Hlsl);
        void* vsBlob = Compile(src, "vs"u8, "vs_5_0"u8);
        void* psBlob = Compile(src, "ps"u8, "ps_5_0"u8);
        try
        {
            void* vs, ps;
            Check(CreateVertexShader(_device, BlobPtr(vsBlob), BlobSize(vsBlob), null, &vs), "CreateVertexShader");
            Check(CreatePixelShader(_device, BlobPtr(psBlob), BlobSize(psBlob), null, &ps), "CreatePixelShader");
            _vs = vs; _ps = ps;
        }
        finally { Release(ref vsBlob); Release(ref psBlob); }
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
