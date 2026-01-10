using Avalonia;
using System;

namespace App;

class Program
{
    [STAThread]
    public static void Main(string[] args) => build_avalonia_app()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder build_avalonia_app()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
