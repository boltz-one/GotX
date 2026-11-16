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
BackgroundClipTests.Register(suite);
ShadowExclusionTests.Register(suite);
GridFrMinTests.Register(suite);
FitContentTests.Register(suite);
TextInputAlignTests.Register(suite);
TextInputEditTests.Register(suite);
StrokeCoverageTests.Register(suite);
FlexWrapMeasureTests.Register(suite);
BaselineAlignTests.Register(suite);
FlexBasisTests.Register(suite);
ShrinkWrapTests.Register(suite);
OverlayFocusTests.Register(suite);
TimerTests.Register(suite);
VectorTests.Register(suite);
ImageTests.Register(suite);
TextStyleTests.Register(suite);
TextShapingTests.Register(suite);
TextLayoutTests.Register(suite);
TextLatinParityTests.Register(suite);
DiagnosticsTests.Register(suite);
StateAndScrollTests.Register(suite);
RegressionTests.Register(suite);
TailwindFeatureTests.Register(suite);
VirtualListTests.Register(suite);
VirtualListVariableTests.Register(suite);
VirtualDataTableTests.Register(suite);
SemanticsTests.Register(suite);
SemanticsCostTests.Register(suite);
SemanticsUiaTests.Register(suite);

// 3. Các trang gallery của GotX.Components: lint (so với lint-baseline.txt) và hiệu năng.
GalleryTests.Register(suite);

// 4. Ví dụ samples/Shadcn (port trang chủ và block của shadcn/ui): hành vi, lint, hiệu năng.
ShadcnHomeTests.Register(suite);
ShadcnBlocksTests.Register(suite);
ShadcnFidelityTests.Register(suite);

// 5. Hiệu năng: nhật ký PerfLog, các cache đường nóng, đường nhanh của renderer CPU, ngân sách trên trang lớn (xem docs/performance.md).
PerfLogTests.Register(suite);
PerfCacheTests.Register(suite);
PerfRasterTests.Register(suite);
PerfBudgetTests.Register(suite);
ZeroAllocTests.Register(suite);
PartialParityTests.Register(suite);
IsolationTests.Register(suite);

// 5b. GPU: parity CPU↔D3D11 (WARP), mất thiết bị, rò rỉ, bộ đếm, vùng hỏng (docs/performance.md §5).
GpuParityTests.Register(suite);
GpuRobustnessTests.Register(suite);
GpuCountersTests.Register(suite);
GpuDamageTests.Register(suite);

// 5. tools/GotX.Port (công cụ cho skill gotx-port): trích class, gợi ý class chưa hỗ trợ, html/gpui → GotX, diff ảnh.
PortToolTests.Register(suite);

return suite.Run(args);
