Create a new WinForms panel for BocceManager named $ARGUMENTS.

Steps:
1. Create `Panels/$ARGUMENTSPanel.cs` using the standard panel boilerplate below.
2. Remind the user of the two manual steps needed to wire it up (listed at the bottom).

## Boilerplate template

```csharp
using BocceManager.Data;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class $ARGUMENTSPanel : UserControl
{
    private bool _isLoadingData = false;

    public $ARGUMENTSPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        LoadData();
    }

    private void BuildUi()
    {
        var title = new Label
        {
            Text = "$ARGUMENTS",
            Dock = DockStyle.Top,
            Height = 32,
            Font = AppTheme.FontSectionHeading,
            ForeColor = AppTheme.TextPrimary,
            Padding = new Padding(8, 4, 0, 0)
        };

        Controls.Add(title);
    }

    private void LoadData()
    {
        if (_isLoadingData) return;
        _isLoadingData = true;
        try
        {
            using var db = new BocceDbContext();
            // TODO: load data
        }
        finally
        {
            _isLoadingData = false;
        }
    }
}
```

## Manual wiring steps (remind the user)

After creating the file, the user must do two things in `MainForm.cs`:

1. **Add a `using`** for `BocceManager.Panels` (already present — skip if so).
2. **Register in the nav switch**: find the `switch` or `if/else` block in `ShowPanel(string key)` (or equivalent navigation method) and add a case for the new panel, e.g.:
   ```csharp
   case "$ARGUMENTS":
       ShowContent(new $ARGUMENTSPanel());
       break;
   ```
3. **Add a nav menu entry**: find where sidebar nav items are built (look for `AddNavItem` calls) and add the new entry in the appropriate group.
