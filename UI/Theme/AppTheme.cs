namespace BocceManager.UI.Theme;

public static class AppTheme
{
    private static readonly string SettingsFile =
        Path.Combine(AppContext.BaseDirectory, "bocce-theme.txt");

    public static ThemePreset Current { get; private set; } = ThemePreset.Light;

    // ── Navigation ────────────────────────────────────────────────────────────
    public static Color NavBackground      { get; private set; }
    public static Color NavTitleBackground { get; private set; }
    public static Color NavText            { get; private set; }
    public static Color NavHover           { get; private set; }
    public static Color NavSelected        { get; private set; }
    public static Color NavHeader          { get; private set; }

    // ── Content ───────────────────────────────────────────────────────────────
    public static Color ContentBackground { get; private set; }
    public static Color Surface           { get; private set; }
    public static Color TextPrimary       { get; private set; }
    public static Color TextSecondary     { get; private set; }
    public static Color TextMuted         { get; private set; }
    public static Color Separator         { get; private set; }
    public static Color Accent            { get; private set; }

    // ── Disabled State ─────────────────────────────────────────────────────────
    public static Color DisabledBackground { get; private set; }
    public static Color DisabledText       { get; private set; }

    // ── Grid ──────────────────────────────────────────────────────────────────
    public static Color GridHeaderBackground { get; private set; }
    public static Color GridHeaderText       { get; private set; }
    public static Color GridAlternateRow     { get; private set; }
    public static Color GridLines            { get; private set; }

    // ── Buttons ───────────────────────────────────────────────────────────────
    public static Color ButtonSuccess { get; private set; }
    public static Color ButtonDanger  { get; private set; }

    // ── Edit Mode ─────────────────────────────────────────────────────────────
    public static Color EditModeBackground { get; private set; }
    public static Color CreateModeBackground { get; private set; }

    // ── Fonts ─────────────────────────────────────────────────────────────────
    public static Font FontDefault        { get; private set; } = null!;
    public static Font FontDefaultBold    { get; private set; } = null!;
    public static Font FontCategoryLabel  { get; private set; } = null!;
    public static Font FontSmall          { get; private set; } = null!;
    public static Font FontSmallBold      { get; private set; } = null!;
    public static Font FontButton         { get; private set; } = null!;
    public static Font FontNavItem        { get; private set; } = null!;
    public static Font FontNavHeader      { get; private set; } = null!;
    public static Font FontNavTitle       { get; private set; } = null!;
    public static Font FontPageTitle      { get; private set; } = null!;
    public static Font FontPageSubtitle   { get; private set; } = null!;
    public static Font FontSectionHeading { get; private set; } = null!;
    public static Font FontStatValue      { get; private set; } = null!;
    public static Font FontStatLabel      { get; private set; } = null!;
    public static Font FontGridHeader     { get; private set; } = null!;

    // ── Load & Save ───────────────────────────────────────────────────────────

    public static void LoadSaved()
    {
        var preset = ThemePreset.Light;
        try
        {
            if (File.Exists(SettingsFile) &&
                Enum.TryParse(File.ReadAllText(SettingsFile).Trim(), out ThemePreset saved))
                preset = saved;
        }
        catch { }
        Load(preset);
    }

    public static void Save(ThemePreset preset)
    {
        try { File.WriteAllText(SettingsFile, preset.ToString()); }
        catch { }
        Load(preset);
    }

    public static void Load(ThemePreset preset)
    {
        Current = preset;
        string family = "Segoe UI";

        switch (preset)
        {
            case ThemePreset.Dark:
                NavBackground      = C(25,  25,  28);
                NavTitleBackground = C(15,  15,  18);
                NavText            = C(210, 210, 215);
                NavHover           = C(45,  45,  50);
                NavSelected        = C(0,   120, 215);
                NavHeader          = C(100, 100, 110);
                ContentBackground  = C(32,  32,  35);
                Surface            = C(45,  45,  50);
                TextPrimary        = C(230, 230, 235);
                TextSecondary      = C(150, 150, 160);
                TextMuted          = C(120, 120, 130);
                Separator          = C(65,  65,  72);
                Accent             = C(0,   120, 215);
                GridHeaderBackground = C(20,  20,  24);
                GridHeaderText       = C(210, 210, 215);
                GridAlternateRow     = C(38,  38,  42);
                GridLines            = C(55,  55,  62);
                ButtonSuccess        = C(39,  174, 96);
                ButtonDanger         = C(192, 57,  43);
                EditModeBackground   = C(70,  60,  35);
                CreateModeBackground = C(50,  70,  45);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            case ThemePreset.Classic:
                NavBackground      = C(100, 100, 100);
                NavTitleBackground = C(75,  75,  75);
                NavText            = C(255, 255, 255);
                NavHover           = C(130, 130, 130);
                NavSelected        = C(0,   84,  166);
                NavHeader          = C(190, 190, 190);
                ContentBackground  = C(240, 240, 240);
                Surface            = C(255, 255, 255);
                TextPrimary        = C(0,   0,   0);
                TextSecondary      = C(80,  80,  80);
                TextMuted          = C(100, 100, 100);
                Separator          = C(180, 180, 180);
                Accent             = C(0,   84,  166);
                GridHeaderBackground = C(80,  80,  80);
                GridHeaderText       = C(255, 255, 255);
                GridAlternateRow     = C(248, 248, 248);
                GridLines            = C(200, 200, 200);
                ButtonSuccess        = C(0,   128, 0);
                ButtonDanger         = C(180, 0,   0);
                EditModeBackground   = C(255, 253, 220);
                CreateModeBackground = C(220, 255, 220);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            case ThemePreset.BocceGreen:
                NavBackground      = C(27,  79,  45);
                NavTitleBackground = C(18,  55,  30);
                NavText            = C(220, 240, 228);
                NavHover           = C(39,  110, 64);
                NavSelected        = C(200, 160, 0);
                NavHeader          = C(120, 170, 140);
                ContentBackground  = C(245, 252, 247);
                Surface            = C(255, 255, 255);
                TextPrimary        = C(27,  79,  45);
                TextSecondary      = C(80,  130, 95);
                TextMuted          = C(100, 140, 110);
                Separator          = C(195, 225, 205);
                Accent             = C(39,  174, 96);
                GridHeaderBackground = C(27,  79,  45);
                GridHeaderText       = C(220, 240, 228);
                GridAlternateRow     = C(240, 250, 243);
                GridLines            = C(195, 225, 205);
                ButtonSuccess        = C(39,  174, 96);
                ButtonDanger         = C(192, 57,  43);
                EditModeBackground   = C(255, 253, 235);
                CreateModeBackground = C(230, 255, 240);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            case ThemePreset.MidnightBlue:
                NavBackground      = C(13,  27,  62);
                NavTitleBackground = C(8,   18,  42);
                NavText            = C(180, 210, 255);
                NavHover           = C(22,  44,  95);
                NavSelected        = C(0,   200, 210);
                NavHeader          = C(90,  130, 195);
                ContentBackground  = C(18,  35,  75);
                Surface            = C(25,  48,  100);
                TextPrimary        = C(200, 220, 255);
                TextSecondary      = C(130, 165, 220);
                TextMuted          = C(110, 145, 200);
                Separator          = C(40,  70,  130);
                Accent             = C(0,   200, 210);
                GridHeaderBackground = C(10,  22,  52);
                GridHeaderText       = C(180, 210, 255);
                GridAlternateRow     = C(22,  40,  82);
                GridLines            = C(38,  65,  120);
                ButtonSuccess        = C(0,   180, 160);
                ButtonDanger         = C(180, 50,  80);
                EditModeBackground   = C(45,  55,  95);
                CreateModeBackground = C(35,  65,  50);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            case ThemePreset.Slate:
                NavBackground      = C(52,  62,  72);
                NavTitleBackground = C(36,  44,  52);
                NavText            = C(215, 220, 228);
                NavHover           = C(68,  80,  94);
                NavSelected        = C(220, 100, 30);
                NavHeader          = C(140, 155, 170);
                ContentBackground  = C(244, 245, 248);
                Surface            = C(255, 255, 255);
                TextPrimary        = C(52,  62,  72);
                TextSecondary      = C(108, 122, 137);
                TextMuted          = C(130, 144, 158);
                Separator          = C(208, 212, 218);
                Accent             = C(220, 100, 30);
                GridHeaderBackground = C(52,  62,  72);
                GridHeaderText       = C(215, 220, 228);
                GridAlternateRow     = C(248, 249, 252);
                GridLines            = C(210, 215, 225);
                ButtonSuccess        = C(70,  170, 100);
                ButtonDanger         = C(192, 57,  43);
                EditModeBackground   = C(255, 250, 215);
                CreateModeBackground = C(230, 255, 235);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            case ThemePreset.HighContrast:
                NavBackground      = C(0,   0,   0);
                NavTitleBackground = C(0,   0,   0);
                NavText            = C(255, 255, 0);
                NavHover           = C(40,  40,  0);
                NavSelected        = C(255, 255, 255);
                NavHeader          = C(200, 200, 0);
                ContentBackground  = C(0,   0,   0);
                Surface            = C(20,  20,  20);
                TextPrimary        = C(255, 255, 255);
                TextSecondary      = C(255, 255, 0);
                TextMuted          = C(200, 200, 200);
                Separator          = C(255, 255, 255);
                Accent             = C(255, 255, 0);
                GridHeaderBackground = C(0,   0,   0);
                GridHeaderText       = C(255, 255, 0);
                GridAlternateRow     = C(15,  15,  15);
                GridLines            = C(100, 100, 100);
                ButtonSuccess        = C(0,   200, 0);
                ButtonDanger         = C(255, 0,   0);
                EditModeBackground   = C(255, 255, 0);
                CreateModeBackground = C(0,   255, 0);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            default: // Light
                NavBackground      = C(44,  62,  80);
                NavTitleBackground = C(30,  45,  60);
                NavText            = C(236, 240, 241);
                NavHover           = C(52,  73,  94);
                NavSelected        = C(41,  128, 185);
                NavHeader          = C(127, 140, 141);
                ContentBackground  = Color.White;
                Surface            = C(245, 248, 250);
                TextPrimary        = C(44,  62,  80);
                TextSecondary      = C(127, 140, 141);
                TextMuted          = C(100, 100, 100);
                Separator          = C(220, 220, 220);
                Accent             = C(41,  128, 185);
                GridHeaderBackground = C(44,  62,  80);
                GridHeaderText       = Color.White;
                GridAlternateRow     = C(248, 250, 252);
                GridLines            = C(220, 230, 240);
                ButtonSuccess        = C(46,  204, 113);
                ButtonDanger         = C(231, 76,  60);
                EditModeBackground   = C(255, 254, 230);
                CreateModeBackground = C(230, 255, 235);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;
        }

        FontDefault        = new Font(family, 10f);
        FontDefaultBold    = new Font(family, 10f, FontStyle.Bold);
        FontSmall          = new Font(family, 9f);
        FontSmallBold      = new Font(family, 9.5f, FontStyle.Bold);
        FontButton         = new Font(family, 9.5f);
        FontNavHeader      = new Font(family, 12f, FontStyle.Bold);
        FontNavItem        = new Font(family, 10.5f);
        FontNavTitle       = new Font(family, 13f,  FontStyle.Bold);
        FontPageTitle      = new Font(family, 28f,  FontStyle.Bold);
        FontPageSubtitle   = new Font(family, 13f);
        FontSectionHeading = new Font(family, 12f,  FontStyle.Bold);
        FontStatValue      = new Font(family, 20f,  FontStyle.Bold);
        FontStatLabel      = new Font(family, 8f);
        FontGridHeader     = new Font(family, 9.5f, FontStyle.Bold);
    }

    public static void ApplyDisabledStyle(Control control)
    {
        if (control.Enabled)
            return;

        control.BackColor = DisabledBackground;
        control.ForeColor = DisabledText;

        if (control is TextBox tb)
        {
            tb.ReadOnly = true;
        }
        else if (control is Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = DisabledBackground;
            btn.ForeColor = DisabledText;
            btn.FlatAppearance.BorderColor = DisabledText;
        }
        else if (control is CheckBox chk)
        {
            chk.FlatStyle = FlatStyle.Flat;
            chk.ForeColor = DisabledText;
        }
    }

    public static void ApplyDisabledStylesToAll(Control container)
    {
        foreach (Control control in GetAllControls(container))
        {
            if (!control.Enabled)
                ApplyDisabledStyle(control);
        }
    }

    private static IEnumerable<Control> GetAllControls(Control container)
    {
        foreach (Control control in container.Controls)
        {
            yield return control;
            foreach (var child in GetAllControls(control))
                yield return child;
        }
    }

    private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);

    public static string DisplayName(ThemePreset preset) => preset switch
    {
        ThemePreset.Light        => "Light",
        ThemePreset.Dark         => "Dark",
        ThemePreset.Classic      => "Classic",
        ThemePreset.BocceGreen   => "Bocce Green",
        ThemePreset.MidnightBlue => "Midnight Blue",
        ThemePreset.Slate        => "Slate",
        ThemePreset.HighContrast => "High Contrast",
        _ => preset.ToString()
    };
}
