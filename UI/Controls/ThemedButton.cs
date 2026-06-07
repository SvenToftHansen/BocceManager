namespace BocceManager.UI.Controls;

public class ThemedButton : Button
{
    private Color _originalForeColor;
    private Color _originalBackColor;

    public ThemedButton()
    {
        FlatStyle = FlatStyle.Flat;
        _originalBackColor = BackColor;
        _originalForeColor = ForeColor;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        if (!Enabled)
        {
            _originalForeColor = ForeColor;
            _originalBackColor = BackColor;
        }
        base.OnEnabledChanged(e);
        if (!Enabled)
        {
            ForeColor = _originalForeColor;
            BackColor = _originalBackColor;
        }
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        if (!Enabled)
        {
            ForeColor = _originalForeColor;
            BackColor = _originalBackColor;
        }
        base.WndProc(ref m);
    }
}
