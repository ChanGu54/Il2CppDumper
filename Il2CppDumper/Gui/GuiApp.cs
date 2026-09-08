using System;
using Avalonia;

namespace Il2CppDumper
{
    public static class GuiApp
    {
        public static void Run()
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
