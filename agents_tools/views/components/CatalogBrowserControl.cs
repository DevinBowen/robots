namespace agents_tools.views.components;

using System.Drawing;
using System.Windows.Forms;
using agents_tools.models;

internal sealed class CatalogBrowserControl : UserControl
{
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _sortCombo = new();
    private readonly Label _countLabel = new();
    private readonly ListBox _listBox = new();
    private readonly TextBox _detailsBox = new();
    private readonly SplitContainer _split = new();

    private IReadOnlyList<CatalogEntry> _entries = Array.Empty<CatalogEntry>();
    private List<CatalogEntry> _filteredEntries = new();

    public event EventHandler? SelectionChanged;

    public CatalogBrowserControl()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Canvas;

        var topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            ColumnCount = 3,
            BackColor = AppTheme.Canvas,
            Padding = new Padding(0, 0, 0, 12)
        };

        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        _searchBox.Dock = DockStyle.Fill;
        _searchBox.Margin = new Padding(0, 24, 12, 0);
        _searchBox.PlaceholderText = "Search agents and tools";
        _searchBox.Font = AppTheme.Body;
        _searchBox.AccessibleName = "Search catalog";
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        _sortCombo.Dock = DockStyle.Fill;
        _sortCombo.Margin = new Padding(0, 24, 12, 0);
        _sortCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _sortCombo.Font = AppTheme.Body;
        _sortCombo.AccessibleName = "Sort catalog";
        _sortCombo.Items.AddRange(new object[] { "Name (A-Z)", "Name (Z-A)", "Type", "Category" });
        _sortCombo.SelectedIndex = 0;
        _sortCombo.SelectedIndexChanged += (_, _) => ApplyFilter();

        _countLabel.Dock = DockStyle.Fill;
        _countLabel.Margin = new Padding(0, 24, 0, 0);
        _countLabel.Font = AppTheme.Body;
        _countLabel.ForeColor = AppTheme.TextMuted;
        _countLabel.TextAlign = ContentAlignment.MiddleRight;

        topPanel.Controls.Add(_searchBox, 0, 0);
        topPanel.Controls.Add(_sortCombo, 1, 0);
        topPanel.Controls.Add(_countLabel, 2, 0);

        _split.Dock = DockStyle.Fill;
        _split.FixedPanel = FixedPanel.Panel2;
        _split.SplitterDistance = 430;
        _split.SplitterWidth = 5;
        _split.Panel1.Padding = new Padding(0, 0, 12, 0);
        _split.Panel2.Padding = new Padding(12, 0, 0, 0);
        _split.BackColor = AppTheme.Canvas;

        _listBox.Dock = DockStyle.Fill;
        _listBox.BorderStyle = BorderStyle.None;
        _listBox.DrawMode = DrawMode.OwnerDrawFixed;
        _listBox.ItemHeight = 38;
        _listBox.Font = AppTheme.Body;
        _listBox.BackColor = AppTheme.Surface;
        _listBox.AccessibleName = "Catalog entries";
        _listBox.DrawItem += DrawListItem;
        _listBox.SelectedIndexChanged += (_, _) => OnSelectionChanged();

        var detailsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12)
        };

        var detailsHeader = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "Details",
            Font = AppTheme.SectionHeading,
            ForeColor = AppTheme.TextStrong
        };

        _detailsBox.Dock = DockStyle.Fill;
        _detailsBox.Multiline = true;
        _detailsBox.ReadOnly = true;
        _detailsBox.BackColor = AppTheme.Surface;
        _detailsBox.BorderStyle = BorderStyle.None;
        _detailsBox.ForeColor = AppTheme.TextBody;
        _detailsBox.Font = AppTheme.Body;
        _detailsBox.WordWrap = true;
        _detailsBox.AccessibleName = "Selected entry details";

        detailsPanel.Controls.Add(_detailsBox);
        detailsPanel.Controls.Add(detailsHeader);

        _split.Panel1.Controls.Add(_listBox);
        _split.Panel2.Controls.Add(detailsPanel);

        Controls.Add(_split);
        Controls.Add(topPanel);
    }

    public CatalogEntry? SelectedEntry
        => _listBox.SelectedIndex >= 0 && _listBox.SelectedIndex < _filteredEntries.Count
            ? _filteredEntries[_listBox.SelectedIndex]
            : null;

    public void SetEntries(IReadOnlyList<CatalogEntry> entries)
    {
        _entries = entries ?? Array.Empty<CatalogEntry>();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = (_searchBox.Text ?? string.Empty).Trim();

        IEnumerable<CatalogEntry> filtered = _entries;
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(e =>
                e.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || e.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                || e.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                || e.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        filtered = _sortCombo.SelectedIndex switch
        {
            1 => filtered.OrderByDescending(e => e.Name),
            2 => filtered.OrderBy(e => e.AssetType).ThenBy(e => e.Name),
            3 => filtered.OrderBy(e => e.Category).ThenBy(e => e.Name),
            _ => filtered.OrderBy(e => e.Name)
        };

        _filteredEntries = filtered.ToList();

        _listBox.BeginUpdate();
        _listBox.Items.Clear();
        _listBox.Items.AddRange(_filteredEntries.Select(e => e.DisplayName).ToArray());
        if (_listBox.Items.Count > 0)
        {
            _listBox.SelectedIndex = 0;
        }
        _listBox.EndUpdate();

        _countLabel.Text = _filteredEntries.Count == 1 ? "1 item" : $"{_filteredEntries.Count} items";
        RefreshDetails();
    }

    private void OnSelectionChanged()
    {
        RefreshDetails();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshDetails()
    {
        var selected = SelectedEntry;
        if (selected is null)
        {
            _detailsBox.Text = "No item selected.";
            return;
        }

        _detailsBox.Text =
            $"Type: {selected.DisplayPrefix}{Environment.NewLine}" +
            $"Name: {selected.Name}{Environment.NewLine}" +
            $"Id: {selected.Id}{Environment.NewLine}" +
            $"Category: {selected.Category}{Environment.NewLine}{Environment.NewLine}" +
            (string.IsNullOrWhiteSpace(selected.Description)
                ? "No description provided."
                : selected.Description);
    }

    private void DrawListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _listBox.Items.Count)
        {
            return;
        }

        var selected = (e.State & DrawItemState.Selected) != 0;
        using var back = new SolidBrush(selected ? AppTheme.AccentMuted : AppTheme.Surface);
        using var fore = new SolidBrush(selected ? AppTheme.Accent : AppTheme.TextBody);
        using var line = new Pen(AppTheme.Border);

        e.Graphics.FillRectangle(back, e.Bounds);

        var format = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        var textBounds = new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 24, e.Bounds.Height);
        e.Graphics.DrawString(_listBox.Items[e.Index].ToString(), AppTheme.Body, fore, textBounds, format);
        e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }
}
