using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.UI.Theme;

namespace BocceManager.Panels;

public class RolesPanel : UserControl
{
    private ListBox _lstRoles = null!;
    private ListView _lvPlayersByRole = null!;
    private List<PlayerRole> _roles = [];

    public RolesPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUI();
        LoadRoles();
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var listPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            BackColor = AppTheme.Surface
        };
        listPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        listPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblListHeader = new Label
        {
            Text = "Player Roles",
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        listPanel.Controls.Add(lblListHeader, 0, 0);

        _lstRoles = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = new Font(AppTheme.FontDefault.FontFamily, 14f, FontStyle.Regular),
            BorderStyle = BorderStyle.None,
            ItemHeight = 30
        };
        _lstRoles.DoubleClick += OnRoleDoubleClick;
        listPanel.Controls.Add(_lstRoles, 0, 1);
        layout.Controls.Add(listPanel, 0, 0);

        var peoplePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            BackColor = AppTheme.Surface
        };
        peoplePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        peoplePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblPeopleHeader = new Label
        {
            Text = "People In Roles",
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefaultBold,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        peoplePanel.Controls.Add(lblPeopleHeader, 0, 0);

        _lvPlayersByRole = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontDefault,
            BorderStyle = BorderStyle.None
        };
        _lvPlayersByRole.Columns.Add("Role", 180);
        _lvPlayersByRole.Columns.Add("Player", 260);
        peoplePanel.Controls.Add(_lvPlayersByRole, 0, 1);
        layout.Controls.Add(peoplePanel, 0, 1);

        Controls.Add(layout);
    }

    private void LoadRoles()
    {
        try
        {
            using var db = new BocceDbContext();
            _roles = db.PlayerRoles.OrderBy(r => r.Id).ToList();

            _lstRoles.Items.Clear();
            foreach (var role in _roles)
                _lstRoles.Items.Add(new RoleListItem(role.Id, role.RoleName));

            LoadPlayersByRole(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading roles:\n\n{ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadPlayersByRole(BocceDbContext db)
    {
        var roleNames = _roles.ToDictionary(r => r.Id, r => r.RoleName);

        var players = db.Players
            .Where(p => p.Role != 0)
            .OrderBy(p => p.Role)
            .ThenBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new { p.Role, p.FirstName, p.LastName })
            .ToList();

        _lvPlayersByRole.Items.Clear();
        foreach (var p in players)
        {
            string roleName = roleNames.TryGetValue(p.Role, out var name) ? name : $"Role {p.Role}";
            var item = new ListViewItem(roleName);
            item.SubItems.Add($"{p.LastName}, {p.FirstName}");
            _lvPlayersByRole.Items.Add(item);
        }
    }

    private void OnRoleDoubleClick(object? sender, EventArgs e)
    {
        if (_lstRoles.SelectedItem is not RoleListItem item) return;

        var dialog = new Form
        {
            Text = "Rename Role",
            Width = 360,
            Height = 160,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblName = new Label
        {
            Text = "Role Name:",
            Location = new Point(10, 15),
            Size = new Size(100, 20),
            ForeColor = AppTheme.TextPrimary
        };
        var txtName = new TextBox
        {
            Location = new Point(10, 38),
            Size = new Size(320, 28),
            Text = item.Name,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnOk = new Button { Text = "OK", Location = new Point(174, 78), Size = new Size(75, 28), DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(255, 78), Size = new Size(75, 28), DialogResult = DialogResult.Cancel };

        dialog.Controls.AddRange([lblName, txtName, btnOk, btnCancel]);
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var newName = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Role name cannot be blank.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dialog.Dispose();
                return;
            }

            try
            {
                using var db = new BocceDbContext();
                var role = db.PlayerRoles.Find(item.Id);
                if (role != null)
                {
                    role.RoleName = newName;
                    db.SaveChanges();
                    LoadRoles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving role:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        dialog.Dispose();
    }

    private sealed record RoleListItem(int Id, string Name) { public override string ToString() => Name; }
}
