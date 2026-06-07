namespace BocceManager.UI.Theme;

public static class ThemeExtensions
{
    public static T ApplyDisabled<T>(this T control) where T : Control
    {
        AppTheme.ApplyDisabledStyle(control);
        return control;
    }

    public static void ApplyDisabledStyle(this Control control)
    {
        AppTheme.ApplyDisabledStyle(control);
    }
}
