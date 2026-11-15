using GotX.Components;
using GotX.Test;

// Bộ test của GotX. Chạy: dotnet run --project tests/GotX.Tests -- [--filter chuỗi] [--out thư-mục]
//   [--update-snapshots] [--update-baseline] [--list] [--verbose]      (chi tiết: docs/testing.md)
ComponentTheme.Install();
var suite = new TestSuite();

// 1. Khung test tự kiểm tra: mỗi luật lint / phép đo phải bắt đúng lỗi được cài sẵn và im lặng trên giao diện sạch.
HarnessTests.Register(suite);
AssertTests.Register(suite);
LintGeometryTests.Register(suite);
LintContentTests.Register(suite);
VisualTests.Register(suite);
PerfTests.Register(suite);
RunnerTests.Register(suite);

// 2. Tính năng của Core.
FocusTests.Register(suite);
OverlayFocusTests.Register(suite);
TimerTests.Register(suite);
VectorTests.Register(suite);
ImageTests.Register(suite);
TextStyleTests.Register(suite);
DiagnosticsTests.Register(suite);
StateAndScrollTests.Register(suite);
RegressionTests.Register(suite);
TailwindFeatureTests.Register(suite);

// 3. Các trang gallery của GotX.Components: lint (so với lint-baseline.txt) và hiệu năng.
GalleryTests.Register(suite);

// 4. Ví dụ samples/Shadcn (port trang chủ và block của shadcn/ui): hành vi, lint, hiệu năng.
ShadcnHomeTests.Register(suite);
ShadcnBlocksTests.Register(suite);
ShadcnFidelityTests.Register(suite);

return suite.Run(args);
