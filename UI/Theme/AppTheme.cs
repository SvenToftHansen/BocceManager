using BocceManager.UI.Controls;

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
            case ThemePreset.LightRed:
                NavBackground      = C(180, 50,  50);
                NavTitleBackground = C(150, 40,  40);
                NavText            = C(255, 255, 255);
                NavHover           = C(200, 70,  70);
                NavSelected        = C(255, 200, 0);
                NavHeader          = C(220, 100, 100);
                ContentBackground  = Color.White;
                Surface            = C(248, 245, 245);
                TextPrimary        = C(44,  44,  44);
                TextSecondary      = C(127, 127, 127);
                TextMuted          = C(100, 100, 100);
                Separator          = C(220, 180, 180);
                Accent             = C(200, 50,  50);
                GridHeaderBackground = C(180, 50,  50);
                GridHeaderText       = Color.White;
                GridAlternateRow     = C(248, 245, 245);
                GridLines           = C(220, 180, 180);
                ButtonSuccess        = C(46,  204, 113);
                ButtonDanger         = C(231, 76,  60);
                EditModeBackground   = C(255, 240, 240);
                CreateModeBackground = C(240, 255, 240);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            case ThemePreset.LightYellow:
                NavBackground      = C(200, 160, 40);
                NavTitleBackground = C(180, 140, 30);
                NavText            = C(255, 255, 255);
                NavHover           = C(220, 180, 60);
                NavSelected        = C(50,  100, 200);
                NavHeader          = C(220, 190, 100);
                ContentBackground  = Color.White;
                Surface            = C(250, 248, 240);
                TextPrimary        = C(44,  44,  44);
                TextSecondary      = C(127, 127, 127);
                TextMuted          = C(100, 100, 100);
                Separator          = C(230, 210, 180);
                Accent             = C(200, 160, 40);
                GridHeaderBackground = C(200, 160, 40);
                GridHeaderText       = Color.White;
                GridAlternateRow     = C(250, 248, 240);
                GridLines            = C(230, 210, 180);
                ButtonSuccess        = C(46,  204, 113);
                ButtonDanger         = C(231, 76,  60);
                EditModeBackground   = C(255, 254, 230);
                CreateModeBackground = C(240, 255, 240);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            case ThemePreset.LightPurple:
                NavBackground      = C(120, 70,  160);
                NavTitleBackground = C(100, 50,  140);
                NavText            = C(255, 255, 255);
                NavHover           = C(140, 90,  180);
                NavSelected        = C(255, 200, 0);
                NavHeader          = C(160, 120, 200);
                ContentBackground  = Color.White;
                Surface            = C(248, 245, 252);
                TextPrimary        = C(44,  44,  44);
                TextSecondary      = C(127, 127, 127);
                TextMuted          = C(100, 100, 100);
                Separator          = C(210, 190, 230);
                Accent             = C(120, 70,  160);
                GridHeaderBackground = C(120, 70,  160);
                GridHeaderText       = Color.White;
                GridAlternateRow     = C(248, 245, 252);
                GridLines            = C(210, 190, 230);
                ButtonSuccess        = C(46,  204, 113);
                ButtonDanger         = C(231, 76,  60);
                EditModeBackground   = C(245, 240, 255);
                CreateModeBackground = C(240, 255, 240);
                DisabledBackground   = ContentBackground;
                DisabledText         = TextPrimary;
                break;

            case ThemePreset.LightCyan:
                NavBackground      = C(30,  140, 160);
                NavTitleBackground = C(20,  120, 140);
                NavText            = C(255, 255, 255);
                NavHover           = C(50,  160, 180);
                NavSelected        = C(255, 200, 0);
                NavHeader          = C(100, 180, 200);
                ContentBackground  = Color.White;
                Surface            = C(240, 250, 252);
                TextPrimary        = C(44,  44,  44);
                TextSecondary      = C(127, 127, 127);
                TextMuted          = C(100, 100, 100);
                Separator          = C(190, 230, 240);
                Accent             = C(30,  140, 160);
                GridHeaderBackground = C(30,  140, 160);
                GridHeaderText       = Color.White;
                GridAlternateRow     = C(240, 250, 252);
                GridLines            = C(190, 230, 240);
                ButtonSuccess        = C(46,  204, 113);
                ButtonDanger         = C(231, 76,  60);
                EditModeBackground   = C(240, 255, 255);
                CreateModeBackground = C(240, 255, 240);
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

    public static void ApplyControlStyles(Control container)
    {
        foreach (Control ctrl in GetAllControls(container))
        {
            if (ctrl is TextBox tb &&
                ctrl.Parent is not SearchBoxControl &&
                ctrl.Parent is not NumericUpDown &&
                tb.BorderStyle != BorderStyle.None)
            {
                tb.BorderStyle = BorderStyle.Fixed3D;
            }
            else if (ctrl is RichTextBox rtb)
            {
                rtb.BorderStyle = BorderStyle.Fixed3D;
            }
            else if (ctrl is ListBox lb)
            {
                lb.BorderStyle = BorderStyle.Fixed3D;
            }
            else if (ctrl is Button btn &&
                     ctrl.Parent is not SearchBoxControl &&
                     btn.FlatStyle == FlatStyle.Flat)
            {
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = ControlPaint.Dark(btn.BackColor);
                if (btn.FlatAppearance.MouseDownBackColor == Color.Empty)
                    btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(btn.BackColor);
            }
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
        ThemePreset.LightRed     => "Light Red",
        ThemePreset.LightYellow  => "Light Yellow",
        ThemePreset.LightPurple  => "Light Purple",
        ThemePreset.LightCyan    => "Light Cyan",
        ThemePreset.Classic      => "Classic",
        ThemePreset.BocceGreen   => "Bocce Green",
        ThemePreset.Slate        => "Slate",
        _ => preset.ToString()
    };
}
