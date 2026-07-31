namespace BocceManager.UI.Controls;

// A combo-box-like button that opens a checklist popup so multiple filter
// options can be selected at once. Consumers set the item labels via
// SetItems() and read selections back via IsChecked()/CheckedItems.
public class MultiSelectDropDown : Button
{
    public CheckedListBox List { get; } = new()
    {
        CheckOnClick = true,
        BorderStyle = BorderStyle.FixedSingle,
        IntegralHeight = false
    };

    private readonly ToolStripDropDown _dropDown = new() { Padding = Padding.Empty };
    private readonly ToolStripControlHost _host;
    private readonly string _placeholder;

    public event EventHandler? SelectionChanged;

    public MultiSelectDropDown(string placeholder = "All")
    {
        _placeholder = placeholder;
        Text = placeholder;
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(8, 0, 20, 0);
        Cursor = Cursors.Hand;

        _host = new ToolStripControlHost(List) { Margin = Padding.Empty, AutoSize = false };
        _dropDown.Items.Add(_host);

        List.ItemCheck += (_, _) => BeginInvoke(new Action(RefreshSummary));
        Click += (_, _) => _dropDown.Show(this, new Point(0, Height));
    }

    public void SetItems(IEnumerable<string> items)
    {
        List.Items.Clear();
        foreach (var item in items) List.Items.Add(item);
        List.Width  = Math.Max(Width, 220);
        List.Height = Math.Min(300, List.Items.Count * 20 + 6);
        _host.Size = List.Size;
        RefreshSummary();
    }

    public IReadOnlyList<string> CheckedItems => List.CheckedItems.Cast<string>().ToList();

    public bool IsChecked(string item) => List.CheckedItems.Contains(item);

    private void RefreshSummary()
    {
        int n = List.CheckedItems.Count;
        Text = n == 0 ? _placeholder : $"{n} filter{(n == 1 ? "" : "s")} selected";
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
