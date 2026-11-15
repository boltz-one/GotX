using GotX;
using GotX.Components;

static partial class HomeCards
{
    // Ma trận 21×21 của bản gốc (qr-connect.tsx), '1' = ô đen.
    static readonly string[] QrCells =
    [
        "111111100101101111111", "100000101001001000001", "101110101111101011101", "101110100100001011101",
        "101110101010101011101", "100000100111001000001", "111111101010101111111", "000000001101000000000",
        "101011111001111010110", "010100001110010101001", "111010111011101111010", "001101000101000010101",
        "110111101111010111011", "000000001001010001010", "111111101101111101001", "100000100010001001111",
        "101110101011101110100", "101110100110100010011", "101110101000111101110", "100000101101000011001",
        "111111101011101101111",
    ];

    // Bản gốc là SVG với shape-rendering crispEdges. Một VectorPath vài trăm ô vuông sẽ bị khử răng cưa ở mép ô, nên dựng
    // ảnh RGBA đúng 160×160 (cỡ hiển thị size-40): mép ô rơi đúng pixel, ở scale 1 không bị lấy mẫu lại.
    static readonly ImageSource QrImage = BuildQr(160);

    static ImageSource BuildQr(int size)
    {
        int n = QrCells.Length;
        var rgba = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool dark = QrCells[y * n / size][x * n / size] == '1';
                int i = (y * size + x) * 4;
                rgba[i] = rgba[i + 1] = rgba[i + 2] = dark ? (byte)0 : (byte)255;
                rgba[i + 3] = 255;
            }
        return ImageSource.FromRgba(size, size, rgba);
    }

    /// <summary>qr-connect.tsx: mã QR trên nền trắng (cả ở dark mode) và lời hướng dẫn quét.</summary>
    public static Div QrConnect(Window w) =>
        HomeUi.Card(w).Key("qr-card")
            .Child(HomeUi.Content(w).Class("flex justify-center pt-6")
                .Child(w.Div().Class("rounded-xl border border-border bg-white p-4").Key("qr-frame")
                    .Child(w.Image(QrImage).Class("size-40").Key("qr-code"))))
            .Child(HomeUi.Header(w).Class("text-center")
                .Child(Ui.CardTitle(w, "Scan to connect your mobile device"))
                .Child(Ui.CardDescription(w, "Open the Ledger mobile app and scan this code to link your device.")));
}
