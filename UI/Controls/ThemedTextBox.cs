namespace BocceManager.UI.Controls;

public class ThemedTextBox : TextBox
{
    private Color _originalForeColor;
    private Color _originalBackColor;

    public ThemedTextBox()
    {
        _originalBackColor = BackColor;
        _originalForeColor = ForeColor;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        if (!Enabled)
        {
            _originalForeColor = ForeColor;
            _originalBackColor = BackColor;
            ReadOnly = true;
        }
        else
        {
            ReadOnly = false;
        }
        base.OnEnabledChanged(e);

        // Force colors to stay the same
        if (!Enabled)
        {
            ForeColor = _originalForeColor;
            BackColor = _originalBackColor;
        }
        Invalidate();
    }

    protected override void WndProc(ref Message m)
    {
        // Keep our colors even during painting
        if (!Enabled)
        {
            var oldFore = ForeColor;
            var oldBack = BackColor;
            ForeColor = _originalForeColor;
            BackColor = _originalBackColor;
            base.WndProc(ref m);
            ForeColor = oldFore;
            BackColor = oldBack;
        }
        else
        {
            base.WndProc(ref m);
        }
    }
}
