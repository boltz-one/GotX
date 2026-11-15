using GotX;
using GotX.Components;
using GotX.Platform.Windows;
using ShadcnShots;

// Bản port trang chủ + các block của shadcn/ui, chạy trên Windows (D3D11, tự lùi về CPU).
// --screenshots [dir] [--only ids] / --compare [dir]: chụp headless và so với ảnh tham chiếu (xem Capture/ShotRunner.cs).
ComponentTheme.Install();
if (ShotRunner.Wants(args)) return ShotRunner.Run(args);
// Geist (font của ui.shadcn.com) được nạp trước font hệ thống để thắng khi trùng độ đậm.
Application.Run(new GeistPlatform(new WindowsPlatform()), new WindowOptions("GotX – shadcn/ui", 1440, 900), app => app.New(new SiteShell()));
return 0;
