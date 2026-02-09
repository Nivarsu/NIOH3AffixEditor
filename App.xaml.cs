using System.IO;
using System.Text;
using System.Windows;
using Nioh3AffixEditor.Services;

namespace Nioh3AffixEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterGlobalExceptionLogging();
        base.OnStartup(e);
    }

    private static void RegisterGlobalExceptionLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            WriteCrashLog("AppDomain.CurrentDomain.UnhandledException", ex);
        };

        Current.DispatcherUnhandledException += (_, args) =>
        {
            WriteCrashLog("Application.DispatcherUnhandledException", args.Exception);
            // Keep default behavior (terminate) to avoid masking severe state corruption.
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            WriteCrashLog("TaskScheduler.UnobservedTaskException", args.Exception);
        };
    }

    private static void WriteCrashLog(string source, Exception? exception)
    {
        try
        {
            var appDataDir = AppPaths.GetAppDataDir();
            Directory.CreateDirectory(appDataDir);

            var path = Path.Combine(appDataDir, "crash.log");
            var sb = new StringBuilder();
            sb.AppendLine("==========");
            sb.AppendLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            sb.AppendLine($"Source: {source}");
            sb.AppendLine($"Process: {Environment.ProcessPath}");
            sb.AppendLine($"Thread: {Environment.CurrentManagedThreadId}");
            sb.AppendLine(exception?.ToString() ?? "<null exception>");

            File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Never throw from logging.
        }
    }
}
