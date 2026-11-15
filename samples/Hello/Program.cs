using GotX;
using GotX.Platform.Windows;

// Renderer mặc định: D3D11, tự lùi về CPU nếu không tạo được thiết bị.
// Ép dùng CPU: đặt biến môi trường GOTX_RENDERER=software.
Application.Run(new WindowsPlatform(), new WindowOptions("GotX – Demo", 960, 620), app => app.New(new DemoView()));
