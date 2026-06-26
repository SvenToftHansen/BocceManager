using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Serilog;

namespace BocceManager;

static class Program
{
    [STAThread]
    static void Main()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BocceManager", "logs", "bocce-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        AppLogger.Init(Log.Logger);

        ApplicationConfiguration.Initialize();
        AppTheme.LoadSaved();

        using var splash = new SplashForm();
        splash.Show();
        Application.DoEvents();

        var startTime = DateTime.UtcNow;

        try
        {
            DatabaseInitializer.Initialize();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database initialization failed");
            splash.Hide();
            MessageBox.Show(
                $"Database initialization failed:\n\n{ex.Message}",
                "Golden Vista Bocce League Manager — Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Log.CloseAndFlush();
            return;
        }

        // Ensure splash is visible for at least 2 seconds
        int elapsed = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
        if (elapsed < 2000)
            System.Threading.Thread.Sleep(2000 - elapsed);

        try
        {
            Log.Information("BocceManager starting");
            var mainForm = new MainForm();
            splash.Close();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception during startup");
            splash.Close();
            MessageBox.Show(
                $"Application failed to start:\n\n{ex.GetType().Name}\n{ex.Message}\n\n{ex.StackTrace}",
                "Golden Vista Bocce League Manager — Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
