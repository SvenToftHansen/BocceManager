namespace BocceManager.UI.Controls;

public class ThemedNumericUpDown : NumericUpDown
{
    private Color _originalForeColor;
    private Color _originalBackColor;

    public ThemedNumericUpDown()
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
        // Intercept paint messages to maintain our colors
        if (!Enabled && (m.Msg == 0x000F || m.Msg == 0x0014)) // WM_PAINT or WM_ERASEBKGND
        {
            ForeColor = _originalForeColor;
            BackColor = _originalBackColor;
        }
        base.WndProc(ref m);
        if (!Enabled && (m.Msg == 0x000F || m.Msg == 0x0014))
        {
            ForeColor = _originalForeColor;
            BackColor = _originalBackColor;
        }
    }
}
