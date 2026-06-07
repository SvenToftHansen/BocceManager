namespace BocceManager.UI.Controls;

public class ThemedCheckBox : CheckBox
{
    private Color _originalForeColor;

    public ThemedCheckBox()
    {
        FlatStyle = FlatStyle.Flat;
        _originalForeColor = ForeColor;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        if (!Enabled)
        {
            _originalForeColor = ForeColor;
        }
        base.OnEnabledChanged(e);
        ForeColor = _originalForeColor;
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        if (!Enabled)
        {
            ForeColor = _originalForeColor;
        }
        base.WndProc(ref m);
    }
}
