// Sinh src/GotX.Core/Styling/Styled.g.cs và Tw.g.cs.
// Chạy: dotnet run --project tools/StyleGen -- src/GotX.Core/Styling
// (Sau này có thể chuyển thành Roslyn source generator; giữ dạng tool để không cần package ngoài.)
var outDir = args.Length > 0 ? args[0] : "src/GotX.Core/Styling";
StyledWriter.Write(outDir);
TwWriter.Write(outDir);
Console.WriteLine("Đã sinh Styled.g.cs và Tw.g.cs");