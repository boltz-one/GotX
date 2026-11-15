using GotX;
using GotX.Components;
using GotX.Platform.Windows;

// Gallery các component của GotX.Components trên Windows (D3D11, tự lùi về CPU).
ComponentTheme.Install();
Application.Run(new WindowsPlatform(), new WindowOptions("GotX.Components – Gallery", 1100, 760), app => app.New(new GalleryView()));