// 1) Kiểm tra bộ biên dịch class Tailwind.  2) Chạy DemoView: cache view, input, dark, responsive → PNG.
// dotnet run --project samples/Headless -- [thư mục ra] [scale]
// Mỗi phần nằm ở một file: ClassChecks, FeatureClassChecks, LayoutChecks, RegressionChecks, DemoRun; view thử ở Probes.cs.
string outDir = args.Length > 0 ? args[0] : ".";
float scale = args.Length > 1 ? float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 1f;
Directory.CreateDirectory(outDir);
var c = new Checker(outDir, scale);

ClassChecks.Run(c);
FeatureClassChecks.Run(c);
if (!c.LoadFonts()) { Console.Error.WriteLine("Không tìm thấy font TrueType."); return 1; }
LayoutChecks.Run(c);
RegressionChecks.Run(c);
Console.WriteLine($"Tổng: {c.Pass} đạt, {c.Fail} lỗi.");
DemoRun.Run(c);
return c.Fail == 0 ? 0 : 2;