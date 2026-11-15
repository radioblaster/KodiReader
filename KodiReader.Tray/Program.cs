using KodiReader.Business;
using KodiReader.Data;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace KodiReader.Tray;

internal static class Program
{
    private const string AppName = "KodiReader";
    private const string SingleInstanceMutexName = @"Local\KodiReader";

    [STAThread]
    private static void Main()
    {
        // Only one tray icon: a second start (e.g. manual start after autostart) just exits.
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        IConfiguration configuration = LoadConfiguration();
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

        try
        {
            RegisterGlobalExceptionLogging();
            ApplicationConfiguration.Initialize();

            Log.Information("Kodi Reader started");
            Application.Run(CreateTrayContext(configuration));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Kodi Reader crashed");
        }
        finally
        {
            Log.Information("Kodi Reader stopped");
            Log.CloseAndFlush();
        }
    }

    // BaseDirectory, not the current directory: autostart runs the exe from a different working folder.
    private static IConfiguration LoadConfiguration() => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

    private static void RegisterGlobalExceptionLogging()
    {
        Application.ThreadException += (_, e) => Log.Error(e.Exception, "Unhandled UI exception");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception");
    }

    private static TrayApplicationContext CreateTrayContext(IConfiguration configuration)
    {
        var kodiSettings = configuration.GetSection("Kodi").Get<KodiSettings>() ?? new KodiSettings();
        var nowPlayingService = new NowPlayingService(new KodiPlayerApi(kodiSettings));
        return new TrayApplicationContext(nowPlayingService, new WindowsStartup(AppName));
    }
}
