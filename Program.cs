using BocceManager.Data;
using BocceManager.UI.Theme;

namespace BocceManager;

static class Program
{
    [STAThread]
    static void Main()
    {
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
            splash.Hide();
            MessageBox.Show(
                $"Database initialization failed:\n\n{ex.Message}",
                "Golden Vista Bocce League Manager — Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // Ensure splash is visible for at least 2 seconds
        int elapsed = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
        if (elapsed < 2000)
            System.Threading.Thread.Sleep(2000 - elapsed);

        try
        {
            var mainForm = new MainForm();
            splash.Close();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            splash.Close();
            MessageBox.Show(
                $"Application failed to start:\n\n{ex.GetType().Name}\n{ex.Message}\n\n{ex.StackTrace}",
                "Golden Vista Bocce League Manager — Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
