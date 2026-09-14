using Avalonia;
using Serilog;
using System;
using System.IO;

namespace BuchstabenOS.UI.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "buchstabenos", "logs"
        );
        Directory.CreateDirectory(logDir);
        string logPath = Path.Combine(logDir, "app.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("=== Starte BuchstabenOS ===");
            Log.Information("Argumente: {Args}", args.Length > 0 ? string.Join(" ", args) : "(keine)");
            Log.Information("Session: {Session}, Wayland: {Wayland}, Display: {Display}",
                Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") ?? "unbekannt",
                Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") ?? "kein",
                Environment.GetEnvironmentVariable("DISPLAY") ?? "kein");

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Kritischer Absturz in Main!");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
