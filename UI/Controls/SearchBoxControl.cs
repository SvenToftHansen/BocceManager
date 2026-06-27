using System.ComponentModel;
using BocceManager.UI.Theme;

namespace BocceManager.UI.Controls;

public class SearchBoxControl : UserControl
{
    private TextBox _textBox = null!;
    private Button _clearButton = null!;
    private string _placeholder = "Search...";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Placeholder
    {
        get => _placeholder;
        set
        {
            _placeholder = value;
            if (_textBox != null && string.IsNullOrEmpty(_textBox.Text.Trim()))
                _textBox.Text = _placeholder;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SearchText
    {
        get => _textBox.Text == _placeholder ? "" : _textBox.Text;
        set => _textBox.Text = value ?? _placeholder;
    }

    public event EventHandler? SearchTextChanged;
    public event EventHandler? ClearButtonClicked;

    public SearchBoxControl(string? placeholder = null)
    {
        if (placeholder != null)
            _placeholder = placeholder;
        Height = 28;
        BackColor = Color.Black;
        Padding = new Padding(1);
        BuildUI();
    }

    private void BuildUI()
    {
        _textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.FontDefault,
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.ContentBackground,
            Text = _placeholder,
            BorderStyle = BorderStyle.None
        };

        _clearButton = new Button
        {
            Dock = DockStyle.Right,
            Width = 28,
            Height = 28,
            Text = "✕",
            Font = new Font(AppTheme.FontDefault.FontFamily, 10, FontStyle.Bold),
            ForeColor = AppTheme.TextSecondary,
            BackColor = AppTheme.ContentBackground,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = Padding.Empty
        };
        _clearButton.FlatAppearance.BorderSize = 0;
        _clearButton.FlatAppearance.MouseDownBackColor = AppTheme.ContentBackground;
        _clearButton.FlatAppearance.MouseOverBackColor = AppTheme.ContentBackground;
        _clearButton.Click += (_, _) => ClearSearch();

        _textBox.Enter += (_, _) =>
        {
            if (_textBox.Text == _placeholder)
            {
                _textBox.Text = "";
                _textBox.ForeColor = AppTheme.TextPrimary;
            }
        };

        _textBox.Leave += (_, _) =>
        {
            if (string.IsNullOrEmpty(_textBox.Text))
            {
                _textBox.Text = _placeholder;
                _textBox.ForeColor = AppTheme.TextSecondary;
            }
        };

        _textBox.TextChanged += (_, _) => SearchTextChanged?.Invoke(this, EventArgs.Empty);

        Controls.Add(_textBox);
        Controls.Add(_clearButton);
    }

    private void ClearSearch()
    {
        _textBox.Text = _placeholder;
        _textBox.ForeColor = AppTheme.TextSecondary;
        ClearButtonClicked?.Invoke(this, EventArgs.Empty);
        SearchTextChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Focus(bool selectText = false)
    {
        _textBox.Focus();
        if (selectText) _textBox.SelectAll();
    }
}
