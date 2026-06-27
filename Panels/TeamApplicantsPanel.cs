using BocceManager.Data;
using BocceManager.Data.Entities;
using BocceManager.Services;
using BocceManager.UI.Theme;
using Microsoft.EntityFrameworkCore;

namespace BocceManager.Panels;

public class TeamApplicantsPanel : UserControl
{
    // ── State ──────────────────────────────────────────────────────────────────
    private int? _leagueId;
    private int? _seasonId;
    private int? _selectedApplicantId;
    private bool _isLoadingData = false;
    private bool _isDirty = false;
    private bool _isCreatingNew = false;
    private readonly System.Windows.Forms.Timer _autoSaveTimer = new() { Interval = 1500 };

    private sealed class ApplicantListItem
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public override string ToString() => Label;
    }

    // ── Left panel ─────────────────────────────────────────────────────────────
    private TextBox   _txtSearch    = null!;
    private ComboBox  _cmbFilter    = null!;
    private ListBox   _lstApplicants = null!;

    // ── Right — Group tab ──────────────────────────────────────────────────────
    private TextBox  _txtGroupName    = null!;
    private TextBox  _txtEmail        = null!;
    private TextBox  _txtPhone        = null!;
    private ComboBox _cmbDivision     = null!;
    private TextBox  _txtNotes        = null!;
    private Label    _lblStatus       = null!;
    private Label    _lblPlacedTeam   = null!;
    private Label    _lblCreated      = null!;

    // ── Right — Members tab ────────────────────────────────────────────────────
    private DataGridView _membersGrid     = null!;
    private Button       _btnAddExisting  = null!;
    private Button       _btnAddNew       = null!;
    private Button       _btnRemoveMember = null!;

    // Members working copy (edited in-memory until Save)
    private List<MemberRow> _members = [];

    private sealed class MemberRow
    {
        public int? MemberId    { get; set; }  // TeamApplicantMember.Id (null = new unsaved)
        public int? PlayerId    { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
        public string? Email    { get; set; }
        public string? Phone    { get; set; }
        public string? Notes    { get; set; }
        public bool IsExisting  => PlayerId.HasValue;

        public string DisplayName => IsExisting
            ? $"{LastName}, {FirstName}".Trim().TrimStart(',').Trim()
            : $"{LastName}, {FirstName} (new)".Trim().TrimStart(',').Trim();
    }

    // ── Shared toolbar (bottom of right panel) ─────────────────────────────────
    private Button _btnAdd        = null!;
    private Button _btnSave       = null!;
    private Button _btnCancel     = null!;
    private Button _btnDelete     = null!;
    private Button _btnPlaceGroup = null!;

    private SplitContainer _mainSplit = null!;
    private const int PreferredLeftWidth = 260;

    // ── Constructor ────────────────────────────────────────────────────────────
    public TeamApplicantsPanel()
    {
        BackColor = AppTheme.ContentBackground;
        Dock = DockStyle.Fill;
        BuildUi();
        _autoSaveTimer.Tick += (_, _) => { _autoSaveTimer.Stop(); if (_isDirty && !_isCreatingNew) SaveApplicant(silent: true); };
        AppParameterService.DefaultsChanged += OnDefaultsChanged;
        LoadContext();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            AppParameterService.DefaultsChanged -= OnDefaultsChanged;
        base.Dispose(disposing);
    }

    private void OnDefaultsChanged(object? sender, DefaultsChangedEventArgs e)
        => LoadContext();

    // ── Context loading ────────────────────────────────────────────────────────
    private void LoadContext()
    {
        try
        {
            using var db = new BocceDbContext();
            _leagueId = AppParameterService.GetDefaultLeagueId(db);
            _seasonId = AppParameterService.GetDefaultSeasonId(db);
        }
        catch
        {
            _leagueId = null;
            _seasonId = null;
        }

        LoadApplicantList();
        LoadDivisionCombo();
    }

    // ── UI Construction ────────────────────────────────────────────────────────
    private void BuildUi()
    {
        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.ContentBackground,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        _mainSplit.Panel1.Controls.Add(BuildLeftPanel());
        _mainSplit.Panel2.Controls.Add(BuildRightPanel());
        Controls.Add(_mainSplit);

        _mainSplit.SizeChanged  += (_, _) => SafeApplySplit();
        _mainSplit.HandleCreated += (_, _) => BeginInvoke(new Action(SafeApplySplit));
    }

    private void SafeApplySplit()
    {
        if (_mainSplit.Width <= 1) return;
        const int leftMin  = 220;
        const int rightMin = 400;
        int total = Math.Max(0, _mainSplit.Width - 1);
        int lMin  = leftMin, rMin = rightMin;
        if (lMin + rMin > total)
        {
            double ratio = leftMin / (double)(leftMin + rightMin);
            lMin = (int)Math.Floor(total * ratio);
            rMin = total - lMin;
        }
        _mainSplit.Panel1MinSize = lMin;
        _mainSplit.Panel2MinSize = rMin;
        int maxLeft = _mainSplit.Width - rMin;
        int clamped = Math.Max(lMin, Math.Min(PreferredLeftWidth, maxLeft));
        if (clamped > 0) _mainSplit.SplitterDistance = clamped;
    }

    private Control BuildLeftPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        _cmbFilter = new ComboBox
        {
            Dock = DockStyle.Top, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary
        };
        _cmbFilter.Items.AddRange(["Pending", "All"]);
        _cmbFilter.SelectedIndex = 0;
        _cmbFilter.SelectedIndexChanged += (_, _) => LoadApplicantList();

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Top, Height = 30, PlaceholderText = "Search groups...",
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };
        _txtSearch.TextChanged += (_, _) => LoadApplicantList();

        _lstApplicants = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 30
        };
        _lstApplicants.DrawItem += OnDrawApplicantItem;
        _lstApplicants.SelectedIndexChanged += OnApplicantSelected;

        var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        _btnAdd = new Button
        {
            Text = "+ Add Group", Location = new Point(12, 8), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnAdd.Click += (_, _) => StartAddApplicant();
        _btnDelete = new Button
        {
            Text = "Delete", Location = new Point(144, 8), Size = new Size(90, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnDelete.Click += (_, _) => DeleteApplicant();
        toolbar.Controls.AddRange([_btnAdd, _btnDelete]);

        var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = AppTheme.Separator };

        // Dock order: bottom first, then fill, then top items in reverse
        pnl.Controls.Add(_lstApplicants);
        pnl.Controls.Add(sep);
        pnl.Controls.Add(toolbar);
        pnl.Controls.Add(_txtSearch);
        pnl.Controls.Add(_cmbFilter);
        return pnl;
    }

    private void OnDrawApplicantItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _lstApplicants.Items.Count) return;
        var item = _lstApplicants.Items[e.Index] as ApplicantListItem;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        e.Graphics.FillRectangle(new SolidBrush(selected ? AppTheme.NavSelected : AppTheme.Surface), e.Bounds);
        e.Graphics.DrawString(item?.Label ?? "", AppTheme.FontDefault,
            new SolidBrush(selected ? Color.White : AppTheme.TextPrimary),
            new PointF(e.Bounds.X + 8, e.Bounds.Y + 7));
    }

    private Control BuildRightPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.ContentBackground };

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill, Font = AppTheme.FontDefault,
            BackColor = AppTheme.ContentBackground
        };

        tabs.TabPages.Add(BuildGroupTab());
        tabs.TabPages.Add(BuildMembersTab());

        var toolbar = BuildRightToolbar();

        pnl.Controls.Add(tabs);
        pnl.Controls.Add(toolbar);
        return pnl;
    }

    private TabPage BuildGroupTab()
    {
        var page = new TabPage("Group") { BackColor = AppTheme.ContentBackground, Padding = new Padding(0) };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.ContentBackground };

        int y = 20;
        const int lx = 16, fx = 160, fw = 360;

        scroll.Controls.Add(Lbl("Group Name *", lx, y));
        _txtGroupName = TBox(fx, y, fw); _txtGroupName.TextChanged += OnFieldChanged;
        scroll.Controls.Add(_txtGroupName);
        y += 36;

        scroll.Controls.Add(Lbl("Contact Email", lx, y));
        _txtEmail = TBox(fx, y, fw); _txtEmail.TextChanged += OnFieldChanged;
        scroll.Controls.Add(_txtEmail);
        y += 36;

        scroll.Controls.Add(Lbl("Contact Phone", lx, y));
        _txtPhone = TBox(fx, y, fw); _txtPhone.TextChanged += OnFieldChanged;
        scroll.Controls.Add(_txtPhone);
        y += 36;

        scroll.Controls.Add(Lbl("Preferred Division", lx, y));
        _cmbDivision = new ComboBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary
        };
        _cmbDivision.SelectedIndexChanged += OnFieldChanged;
        scroll.Controls.Add(_cmbDivision);
        y += 36;

        scroll.Controls.Add(Lbl("Notes", lx, y));
        _txtNotes = new TextBox
        {
            Location = new Point(fx, y), Size = new Size(fw, 80),
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            Font = AppTheme.FontDefault, BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };
        _txtNotes.TextChanged += OnFieldChanged;
        scroll.Controls.Add(_txtNotes);
        y += 96;

        scroll.Controls.Add(new Panel { Location = new Point(lx, y), Size = new Size(fw + fx - lx, 1), BackColor = AppTheme.Separator });
        y += 12;

        _lblStatus = new Label
        {
            Location = new Point(lx, y), AutoSize = true,
            Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextMuted
        };
        scroll.Controls.Add(_lblStatus);
        y += 24;

        _lblPlacedTeam = new Label
        {
            Location = new Point(lx, y), AutoSize = true,
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextSecondary
        };
        scroll.Controls.Add(_lblPlacedTeam);
        y += 24;

        _lblCreated = new Label
        {
            Location = new Point(lx, y), AutoSize = true,
            Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted
        };
        scroll.Controls.Add(_lblCreated);

        page.Controls.Add(scroll);
        return page;
    }

    private TabPage BuildMembersTab()
    {
        var page = new TabPage("Members") { BackColor = AppTheme.ContentBackground };

        _membersGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, ReadOnly = true,
            AllowUserToAddRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault, RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground, ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader
            }
        };
        _membersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MIdx",  Visible = false });
        _membersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MType", HeaderText = "Type",  FillWeight = 15 });
        _membersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MName", HeaderText = "Name",  FillWeight = 35 });
        _membersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MEmail",HeaderText = "Email", FillWeight = 30 });
        _membersGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MPhone",HeaderText = "Phone", FillWeight = 20 });

        var memberToolbar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        _btnAddExisting = new Button
        {
            Text = "+ Existing Player", Location = new Point(12, 8), Size = new Size(140, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnAddExisting.Click += (_, _) => AddExistingPlayer();

        _btnAddNew = new Button
        {
            Text = "+ New Person", Location = new Point(164, 8), Size = new Size(120, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }
        };
        _btnAddNew.Click += (_, _) => AddNewPerson();

        _btnRemoveMember = new Button
        {
            Text = "Remove", Location = new Point(296, 8), Size = new Size(90, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonDanger, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnRemoveMember.Click += (_, _) => RemoveMember();
        _membersGrid.SelectionChanged += (_, _) =>
            _btnRemoveMember.Enabled = _membersGrid.SelectedRows.Count > 0;

        memberToolbar.Controls.AddRange([_btnAddExisting, _btnAddNew, _btnRemoveMember]);
        page.Controls.Add(_membersGrid);
        page.Controls.Add(memberToolbar);
        return page;
    }

    private Panel BuildRightToolbar()
    {
        var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };

        _btnSave = new Button
        {
            Text = "Create Group", Location = new Point(12, 8), Size = new Size(110, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Visible = false
        };
        _btnSave.Click += (_, _) => SaveApplicant();

        _btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(112, 8), Size = new Size(80, 30),
            FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton, Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 1 }, Visible = false
        };
        _btnCancel.Click += (_, _) => CancelEdit();

        _btnPlaceGroup = new Button
        {
            Text = "Place Group...", Location = new Point(204, 8), Size = new Size(130, 30),
            FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White,
            Font = AppTheme.FontButton, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 }, Enabled = false
        };
        _btnPlaceGroup.Click += (_, _) => PlaceGroup();

        toolbar.Controls.AddRange([_btnSave, _btnCancel, _btnPlaceGroup]);
        return toolbar;
    }

    // ── List loading ────────────────────────────────────────────────────────────
    private void LoadApplicantList()
    {
        _isLoadingData = true;
        try
        {
            _lstApplicants.Items.Clear();
            if (!_leagueId.HasValue || !_seasonId.HasValue) return;

            bool pendingOnly = _cmbFilter.SelectedIndex == 0;
            string search = _txtSearch.Text.Trim();

            List<TeamApplicant> items;
            try
            {
                using var db = new BocceDbContext();
                items = pendingOnly
                    ? TeamApplicantService.GetPending(db, _leagueId.Value, _seasonId.Value)
                    : TeamApplicantService.GetAll(db, _leagueId.Value, _seasonId.Value);
            }
            catch { items = []; }

            foreach (var a in items)
            {
                string label = a.Status == "Pending"
                    ? a.GroupName
                    : $"{a.GroupName}  [{a.Status}]";

                if (!string.IsNullOrWhiteSpace(search) &&
                    !label.Contains(search, StringComparison.OrdinalIgnoreCase)) continue;

                _lstApplicants.Items.Add(new ApplicantListItem { Id = a.Id, Label = label });
            }

            // Reselect if still in list
            if (_selectedApplicantId.HasValue)
            {
                for (int i = 0; i < _lstApplicants.Items.Count; i++)
                {
                    if (((ApplicantListItem)_lstApplicants.Items[i]!).Id == _selectedApplicantId.Value)
                    {
                        _lstApplicants.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        finally { _isLoadingData = false; }
    }

    private void LoadDivisionCombo()
    {
        _cmbDivision.Items.Clear();
        _cmbDivision.Items.Add(new DivisionItem { Id = null, Name = "(no preference)" });

        if (!_leagueId.HasValue || !_seasonId.HasValue) return;
        try
        {
            using var db = new BocceDbContext();
            var divs = db.Divisions
                .Where(d => d.SeasonId == _seasonId.Value)
                .OrderBy(d => d.SortName).ThenBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToList();
            foreach (var d in divs)
                _cmbDivision.Items.Add(new DivisionItem { Id = d.Id, Name = d.Name });
        }
        catch { }

        if (_cmbDivision.Items.Count > 0) _cmbDivision.SelectedIndex = 0;
    }

    // ── Selection ────────────────────────────────────────────────────────────────
    private void OnApplicantSelected(object? sender, EventArgs e)
    {
        if (_isLoadingData) return;
        if (_lstApplicants.SelectedItem is not ApplicantListItem item)
        {
            ClearEditor();
            return;
        }
        if (_isDirty && !_isCreatingNew)
        {
            _autoSaveTimer.Stop();
            SaveApplicant(silent: true);
        }

        _selectedApplicantId = item.Id;
        LoadApplicant(item.Id);
    }

    private void LoadApplicant(int id)
    {
        _isLoadingData = true;
        try
        {
            using var db = new BocceDbContext();
            var a = db.TeamApplicants
                .Include(x => x.Members).ThenInclude(m => m.Player)
                .Include(x => x.PlacedTeam)
                .FirstOrDefault(x => x.Id == id);
            if (a == null) return;

            _txtGroupName.Text = a.GroupName;
            _txtEmail.Text     = a.ContactEmail ?? "";
            _txtPhone.Text     = a.ContactPhone ?? "";
            _txtNotes.Text     = a.Notes ?? "";

            SelectDivisionCombo(a.PreferredDivisionId);

            _lblStatus.Text    = $"Status: {a.Status}";
            _lblStatus.Visible = a.Status != "Pending";
            _lblPlacedTeam.Text    = a.PlacedTeam != null ? $"Placed as: {a.PlacedTeam.SystemName}" : "";
            _lblPlacedTeam.Visible = a.PlacedTeam != null;
            _lblCreated.Text   = $"Created: {a.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}";

            // Load members
            _members = a.Members.Select(m => new MemberRow
            {
                MemberId  = m.Id,
                PlayerId  = m.PlayerId ?? (m.CreatedPlayerId.HasValue ? m.CreatedPlayerId : null),
                FirstName = m.Player?.FirstName ?? m.FirstName,
                LastName  = m.Player?.LastName  ?? m.LastName,
                Email     = m.Player?.Email     ?? m.Email,
                Phone     = m.Player?.Phone     ?? m.Phone,
                Notes     = m.Notes
            }).ToList();
            RefreshMembersGrid();

            bool isPending = a.Status == "Pending";
            _btnPlaceGroup.Enabled = isPending;
            _btnDelete.Enabled     = true;
            _btnCancel.Visible     = false;
            _isCreatingNew         = false;
            ClearDirty();
        }
        finally { _isLoadingData = false; }
    }

    private void SelectDivisionCombo(int? divisionId)
    {
        _cmbDivision.SelectedIndex = 0;
        if (!divisionId.HasValue) return;
        for (int i = 0; i < _cmbDivision.Items.Count; i++)
        {
            if (_cmbDivision.Items[i] is DivisionItem d && d.Id == divisionId)
            {
                _cmbDivision.SelectedIndex = i;
                return;
            }
        }
    }

    private void RefreshMembersGrid()
    {
        _membersGrid.Rows.Clear();
        for (int i = 0; i < _members.Count; i++)
        {
            var m = _members[i];
            _membersGrid.Rows.Add(i, m.IsExisting ? "Existing" : "New", m.DisplayName, m.Email ?? "", m.Phone ?? "");
        }
    }

    private void ClearEditor()
    {
        _isLoadingData = true;
        try
        {
            _selectedApplicantId   = null;
            _txtGroupName.Text     = "";
            _txtEmail.Text         = "";
            _txtPhone.Text         = "";
            _txtNotes.Text         = "";
            if (_cmbDivision.Items.Count > 0) _cmbDivision.SelectedIndex = 0;
            _lblStatus.Text        = "";
            _lblStatus.Visible     = false;
            _lblPlacedTeam.Text    = "";
            _lblPlacedTeam.Visible = false;
            _lblCreated.Text       = "";
            _members.Clear();
            _membersGrid.Rows.Clear();
            _btnCancel.Visible     = false;
            _btnDelete.Enabled     = false;
            _btnPlaceGroup.Enabled = false;
            _isCreatingNew         = false;
            ClearDirty();
        }
        finally { _isLoadingData = false; }
    }

    // ── Dirty tracking ─────────────────────────────────────────────────────────
    private void OnFieldChanged(object? sender, EventArgs e)
    {
        if (_isLoadingData) return;
        SetDirty();
    }

    private void SetDirty()
    {
        _isDirty = true;
        _btnSave.Visible   = _isCreatingNew;
        _btnCancel.Visible = _isCreatingNew;
        if (!_isCreatingNew) { _autoSaveTimer.Stop(); _autoSaveTimer.Start(); }
    }

    private void ClearDirty()
    {
        _isDirty = false;
        _autoSaveTimer.Stop();
        _btnSave.Visible = false;
    }

    // ── Add / Save / Cancel / Delete ───────────────────────────────────────────
    private void StartAddApplicant()
    {
        if (_isDirty && !_isCreatingNew)
        {
            _autoSaveTimer.Stop();
            SaveApplicant(silent: true);
        }
        if (!_leagueId.HasValue || !_seasonId.HasValue)
        {
            MessageBox.Show("Select a league and season first.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _isLoadingData = true;
        _lstApplicants.SelectedIndex = -1;
        _isLoadingData = false;
        ClearEditor();
        _isCreatingNew = true;
        _btnSave.Visible = true;
        _btnCancel.Visible = true;
        _txtGroupName.Focus();
    }

    private void SaveApplicant(bool silent = false)
    {
        string name = _txtGroupName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            if (!silent) { MessageBox.Show("Group Name is required.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Warning); _txtGroupName.Focus(); }
            return;
        }

        int? divId = (_cmbDivision.SelectedItem as DivisionItem)?.Id;

        try
        {
            using var db = new BocceDbContext();

            TeamApplicant applicant;
            if (_isCreatingNew)
            {
                applicant = new TeamApplicant
                {
                    LeagueId  = _leagueId!.Value,
                    SeasonId  = _seasonId!.Value,
                    Status    = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                db.TeamApplicants.Add(applicant);
            }
            else
            {
                applicant = db.TeamApplicants
                    .Include(a => a.Members)
                    .First(a => a.Id == _selectedApplicantId!.Value);
            }

            applicant.GroupName           = name;
            applicant.ContactEmail        = _txtEmail.Text.Trim().NullIfEmpty();
            applicant.ContactPhone        = _txtPhone.Text.Trim().NullIfEmpty();
            applicant.PreferredDivisionId = divId;
            applicant.Notes               = _txtNotes.Text.Trim().NullIfEmpty();

            if (!_isCreatingNew)
            {
                // Sync members: remove deleted ones, add new ones
                var existingIds = _members.Where(m => m.MemberId.HasValue).Select(m => m.MemberId!.Value).ToHashSet();
                var toRemove = applicant.Members.Where(m => !existingIds.Contains(m.Id)).ToList();
                db.TeamApplicantMembers.RemoveRange(toRemove);
            }

            db.SaveChanges();
            _selectedApplicantId = applicant.Id;

            // Upsert members
            foreach (var row in _members)
            {
                if (row.MemberId.HasValue)
                {
                    var m = db.TeamApplicantMembers.Find(row.MemberId.Value);
                    if (m != null) { m.Notes = row.Notes; db.SaveChanges(); }
                }
                else
                {
                    db.TeamApplicantMembers.Add(new TeamApplicantMember
                    {
                        TeamApplicantId = applicant.Id,
                        PlayerId        = row.PlayerId,
                        FirstName       = row.FirstName,
                        LastName        = row.LastName,
                        Email           = row.Email,
                        Phone           = row.Phone,
                        Notes           = row.Notes
                    });
                    db.SaveChanges();
                }
            }

            _isCreatingNew         = false;
            _btnCancel.Visible     = false;
            _btnDelete.Enabled     = true;
            _btnPlaceGroup.Enabled = true;
            ClearDirty();

            AppLogger.Info("Saved applicant group {Name} (Id={Id})", name, applicant.Id);
            LoadApplicantList();
        }
        catch (Exception ex)
        {
            if (!silent) MessageBox.Show($"Could not save:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else AppLogger.Error(ex, "Autosave failed for applicant {Id}", _selectedApplicantId);
        }
    }

    private void CancelEdit()
    {
        _autoSaveTimer.Stop();
        _isCreatingNew = false;
        _isDirty = false;
        _btnCancel.Visible = false;
        if (_selectedApplicantId.HasValue)
            LoadApplicant(_selectedApplicantId.Value);
        else
            ClearEditor();
    }

    private void DeleteApplicant()
    {
        if (!_selectedApplicantId.HasValue) return;
        var res = MessageBox.Show("Delete this applicant group?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (res != DialogResult.Yes) return;
        try
        {
            using var db = new BocceDbContext();
            var a = db.TeamApplicants.Find(_selectedApplicantId.Value);
            if (a != null) { db.TeamApplicants.Remove(a); db.SaveChanges(); }
            _selectedApplicantId = null;
            ClearEditor();
            LoadApplicantList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Members ────────────────────────────────────────────────────────────────
    private void AddExistingPlayer()
    {
        var excluded = _members.Where(m => m.PlayerId.HasValue).Select(m => m.PlayerId!.Value).ToHashSet();
        var picked = PickPlayer(excluded);
        if (picked == null) return;

        try
        {
            using var db = new BocceDbContext();
            var p = db.Players.Find(picked.Value);
            if (p == null) return;
            _members.Add(new MemberRow { PlayerId = p.Id, FirstName = p.FirstName, LastName = p.LastName, Email = p.Email, Phone = p.Phone });
            RefreshMembersGrid();
            SetDirty();
        }
        catch { }
    }

    private void AddNewPerson()
    {
        var (ok, first, last, email, phone) = PromptNewPerson();
        if (!ok) return;
        _members.Add(new MemberRow { FirstName = first, LastName = last, Email = email.NullIfEmpty(), Phone = phone.NullIfEmpty() });
        RefreshMembersGrid();
        SetDirty();
    }

    private void RemoveMember()
    {
        if (_membersGrid.SelectedRows.Count == 0) return;
        int idx = Convert.ToInt32(_membersGrid.SelectedRows[0].Cells["MIdx"].Value);
        if (idx < 0 || idx >= _members.Count) return;
        _members.RemoveAt(idx);
        RefreshMembersGrid();
        SetDirty();
    }

    // ── Place Group ────────────────────────────────────────────────────────────
    private void PlaceGroup()
    {
        if (!_selectedApplicantId.HasValue) return;
        if (_isDirty)
        {
            MessageBox.Show("Save your changes before placing the group.", "Unsaved Changes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        TeamApplicant? applicant;
        List<(int Id, string Name)> divisions;
        try
        {
            using var db = new BocceDbContext();
            applicant = db.TeamApplicants
                .Include(a => a.Members)
                .FirstOrDefault(a => a.Id == _selectedApplicantId.Value);
            if (applicant == null) return;

            divisions = db.Divisions
                .Where(d => d.SeasonId == _seasonId)
                .OrderBy(d => d.SortName).ThenBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .AsEnumerable()
                .Select(d => (d.Id, d.Name))
                .ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (divisions.Count == 0)
        {
            MessageBox.Show("No divisions exist for the current season.", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Prefer the group's preferred division — pre-select it in the picker
        int? preferDivId = applicant.PreferredDivisionId;
        int? chosenDivId = PickDivision(divisions, preferDivId);
        if (!chosenDivId.HasValue) return;

        // Identify new members to show in confirm dialog
        var newMembers = applicant.Members.Where(m => !m.PlayerId.HasValue && !m.CreatedPlayerId.HasValue).ToList();
        string divName = divisions.FirstOrDefault(d => d.Id == chosenDivId.Value).Name ?? "";

        var confirmMsg = $"Place \"{applicant.GroupName}\" in division \"{divName}\"?\n\n" +
                         $"A new team will be created and {applicant.Members.Count} player(s) added.";
        if (newMembers.Count > 0)
        {
            var names = string.Join("\n  • ", newMembers.Select(m => $"{m.FirstName} {m.LastName}".Trim()));
            confirmMsg += $"\n\nThe following new player records will also be created:\n  • {names}";
        }

        var result = MessageBox.Show(confirmMsg, "Confirm Placement",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        (bool success, string message, Team? _) placement;
        try
        {
            using var db = new BocceDbContext();
            placement = TeamApplicantService.PlaceGroup(db, _selectedApplicantId.Value, chosenDivId.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Placement failed:\n{ex.Message}", "Golden Vista Bocce League Master", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (placement.success)
        {
            MessageBox.Show(placement.message, "Group Placed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadApplicantList();
            if (_selectedApplicantId.HasValue) LoadApplicant(_selectedApplicantId.Value);
        }
        else
        {
            MessageBox.Show(placement.message, "Placement Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ── Dialogs ─────────────────────────────────────────────────────────────────
    private int? PickPlayer(HashSet<int> excludeIds)
    {
        using var form = new Form
        {
            Text = "Select Existing Player", Width = 460, Height = 460,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };
        var search = new TextBox
        {
            Dock = DockStyle.Top, Font = AppTheme.FontDefault, Height = 30,
            PlaceholderText = "Search by name...", BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
        };
        var grid = MakePickerGrid(["PId", "Name"]);
        grid.DoubleClick += (_, _) => { if (grid.SelectedRows.Count > 0) form.DialogResult = DialogResult.OK; };

        List<(int Id, string Name)> all = [];
        try
        {
            using var db = new BocceDbContext();
            all = db.Players.Where(p => p.IsActive && !excludeIds.Contains(p.Id))
                .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
                .AsEnumerable()
                .Select(p => (p.Id, $"{p.LastName}, {p.FirstName}"))
                .ToList();
        }
        catch { }

        void Filter(string q)
        {
            grid.Rows.Clear();
            foreach (var (id, name) in all)
                if (SearchQueryService.MatchesAnyTerm(name, q)) grid.Rows.Add(id, name);
        }
        search.TextChanged += (_, _) => Filter(search.Text);
        Filter("");

        var bar = PickerBar(form);
        ((Button)bar.Controls[0]!).Text = "Select Player";
        form.Controls.AddRange([grid, bar, search]);
        form.AcceptButton = (Button)bar.Controls[0];
        form.CancelButton = (Button)bar.Controls[1];

        if (form.ShowDialog(this) == DialogResult.OK && grid.SelectedRows.Count > 0)
        {
            var v = grid.SelectedRows[0].Cells[0].Value;
            if (v != null && v != DBNull.Value) return Convert.ToInt32(v);
        }
        return null;
    }

    private static (bool ok, string first, string last, string email, string phone) PromptNewPerson()
    {
        using var form = new Form
        {
            Text = "Add New Person", Width = 420, Height = 280,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 0), BackColor = AppTheme.ContentBackground };

        static (Label lbl, TextBox txt) Row(Panel p, string label, int y, string placeholder = "")
        {
            var lbl = new Label { Text = label, Location = new Point(0, y + 3), AutoSize = true, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary };
            var txt = new TextBox { Location = new Point(120, y), Size = new Size(240, 26), Font = AppTheme.FontDefault, BackColor = AppTheme.Surface, ForeColor = AppTheme.TextPrimary, PlaceholderText = placeholder, BorderStyle = BorderStyle.FixedSingle };
            p.Controls.AddRange([lbl, txt]);
            return (lbl, txt);
        }

        var (_, txtFirst) = Row(pnl, "First Name *", 10);
        var (_, txtLast)  = Row(pnl, "Last Name *",  44);
        var (_, txtEmail) = Row(pnl, "Email",         78);
        var (_, txtPhone) = Row(pnl, "Phone",        112);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        var btnOk  = new Button { Text = "Add",    DialogResult = DialogResult.OK,     Left = 12,  Top = 8, Width = 90, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.ButtonSuccess, ForeColor = Color.White, Font = AppTheme.FontButton };
        var btnCxl = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 114, Top = 8, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("First and Last name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                form.DialogResult = DialogResult.None;
            }
        };
        bar.Controls.AddRange([btnOk, btnCxl]);
        form.Controls.AddRange([pnl, bar]);
        form.AcceptButton = btnOk; form.CancelButton = btnCxl;

        if (form.ShowDialog() == DialogResult.OK)
            return (true, txtFirst.Text.Trim(), txtLast.Text.Trim(), txtEmail.Text.Trim(), txtPhone.Text.Trim());
        return (false, "", "", "", "");
    }

    private int? PickDivision(List<(int Id, string Name)> divisions, int? preferDivId)
    {
        using var form = new Form
        {
            Text = "Select Division", Width = 420, Height = 380,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AppTheme.ContentBackground
        };
        var hint = new Label
        {
            Dock = DockStyle.Top, Height = 28, Text = "  Select target division for this group:",
            Font = AppTheme.FontDefault, ForeColor = AppTheme.TextMuted, BackColor = AppTheme.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var grid = MakePickerGrid(["DId", "Division"]);
        grid.DoubleClick += (_, _) => { if (grid.SelectedRows.Count > 0) form.DialogResult = DialogResult.OK; };

        int preSelectIdx = -1;
        for (int i = 0; i < divisions.Count; i++)
        {
            grid.Rows.Add(divisions[i].Id, divisions[i].Name);
            if (divisions[i].Id == preferDivId) preSelectIdx = i;
        }
        if (preSelectIdx >= 0) grid.Rows[preSelectIdx].Selected = true;
        else if (grid.Rows.Count > 0) grid.Rows[0].Selected = true;

        var bar = PickerBar(form);
        ((Button)bar.Controls[0]!).Text = "Place Here";
        form.Controls.AddRange([grid, bar, hint]);
        form.AcceptButton = (Button)bar.Controls[0];
        form.CancelButton = (Button)bar.Controls[1];

        if (form.ShowDialog(this) == DialogResult.OK && grid.SelectedRows.Count > 0)
        {
            var v = grid.SelectedRows[0].Cells[0].Value;
            if (v != null && v != DBNull.Value) return Convert.ToInt32(v);
        }
        return null;
    }

    // ── Helper factories ────────────────────────────────────────────────────────
    private static DataGridView MakePickerGrid(string[] columns)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None, BackgroundColor = AppTheme.ContentBackground,
            Font = AppTheme.FontDefault, RowTemplate = { Height = 28 },
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.GridHeaderBackground, ForeColor = AppTheme.GridHeaderText,
                SelectionBackColor = AppTheme.GridHeaderBackground, SelectionForeColor = AppTheme.GridHeaderText,
                Font = AppTheme.FontGridHeader
            }
        };
        bool first = true;
        foreach (var col in columns)
        {
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = col, HeaderText = col, Visible = !first || columns[0] == col && col == columns[0] && false });
            if (first) { g.Columns[col].Visible = false; first = false; }
        }
        return g;
    }

    private static Panel PickerBar(Form form)
    {
        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = AppTheme.Surface };
        var ok  = new Button { Text = "OK",     DialogResult = DialogResult.OK,     Left = 12,  Top = 8, Width = 100, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = AppTheme.Accent, ForeColor = Color.White, Font = AppTheme.FontButton };
        var cxl = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 124, Top = 8, Width = 80,  Height = 30, FlatStyle = FlatStyle.Flat, Font = AppTheme.FontButton };
        bar.Controls.AddRange([ok, cxl]);
        return bar;
    }

    private bool ConfirmDiscard()
        => MessageBox.Show("You have unsaved changes. Discard them?", "Unsaved Changes",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private static Label Lbl(string text, int x, int y) => new()
    {
        Text = text, Font = AppTheme.FontDefaultBold, ForeColor = AppTheme.TextPrimary,
        AutoSize = true, Location = new Point(x, y + 3)
    };

    private static TextBox TBox(int x, int y, int w) => new()
    {
        Location = new Point(x, y), Size = new Size(w, 26),
        Font = AppTheme.FontDefault, BackColor = AppTheme.Surface,
        ForeColor = AppTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle
    };

    private sealed class DivisionItem
    {
        public int? Id  { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }
}

file static class StringExtensions
{
    public static string? NullIfEmpty(this string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s;
}
