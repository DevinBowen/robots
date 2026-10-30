namespace agents_tools.views.components;

using System.Drawing;
using System.Windows.Forms;
using agents_tools.models;

internal sealed class McpMonitorControl : UserControl
{
    private readonly Label _state = new();
    private readonly Label _transport = new();
    private readonly Label _endpoint = new();
    private readonly Label _command = new();
    private readonly Label _source = new();
    private readonly Label _pid = new();
    private readonly Label _note = new();
    private readonly TextBox _logs = new();

    private readonly Button _buttonStart = new();
    private readonly Button _buttonStop = new();
    private readonly Button _buttonRefresh = new();
    private readonly Button _buttonOpenLog = new();
    private readonly Button _buttonClear = new();

    public event EventHandler? StartRequested;
    public event EventHandler? StopRequested;
    public event EventHandler? RefreshRequested;
    public event EventHandler? OpenLogRequested;
    public event EventHandler? ClearLogsRequested;

    public McpMonitorControl()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Canvas;

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Canvas,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };

        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var top = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(16)
        };

        ConfigureInfoLabel(_state, 0, true);
        ConfigureInfoLabel(_transport, 30, false);
        ConfigureInfoLabel(_endpoint, 54, false);
        ConfigureInfoLabel(_command, 78, false, 36);
        ConfigureInfoLabel(_source, 118, false);
        ConfigureInfoLabel(_pid, 142, false);

        _note.Location = new Point(0, 226);
        _note.Size = new Size(1000, 22);
        _note.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        _note.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
        _note.ForeColor = AppTheme.TextMuted;
        _note.Text = "For stdio MCP servers, external instances are not port-discoverable.";

        ConfigureActionButton(_buttonStart, "Start", new Point(0, 172), (_, _) => StartRequested?.Invoke(this, EventArgs.Empty));
        ConfigureActionButton(_buttonStop, "Stop", new Point(100, 172), (_, _) => StopRequested?.Invoke(this, EventArgs.Empty));
        ConfigureActionButton(_buttonRefresh, "Refresh", new Point(200, 172), (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty));
        ConfigureActionButton(_buttonOpenLog, "Open Log", new Point(300, 172), (_, _) => OpenLogRequested?.Invoke(this, EventArgs.Empty));
        ConfigureActionButton(_buttonClear, "Clear", new Point(400, 172), (_, _) => ClearLogsRequested?.Invoke(this, EventArgs.Empty));

        top.Controls.Add(_state);
        top.Controls.Add(_transport);
        top.Controls.Add(_endpoint);
        top.Controls.Add(_command);
        top.Controls.Add(_source);
        top.Controls.Add(_pid);
        top.Controls.Add(_buttonStart);
        top.Controls.Add(_buttonStop);
        top.Controls.Add(_buttonRefresh);
        top.Controls.Add(_buttonOpenLog);
        top.Controls.Add(_buttonClear);
        top.Controls.Add(_note);

        _logs.Dock = DockStyle.Fill;
        _logs.Multiline = true;
        _logs.ReadOnly = true;
        _logs.ScrollBars = ScrollBars.Vertical;
        _logs.WordWrap = false;
        _logs.BackColor = AppTheme.Surface;
        _logs.BorderStyle = BorderStyle.FixedSingle;
        _logs.Font = AppTheme.Mono;
        _logs.AccessibleName = "MCP logs";

        host.Controls.Add(top, 0, 0);
        host.Controls.Add(_logs, 0, 1);
        Controls.Add(host);
    }

    public void SetBusy(bool busy)
    {
        _buttonStart.Enabled = !busy && _buttonStart.Enabled;
        _buttonStop.Enabled = !busy && _buttonStop.Enabled;
        _buttonRefresh.Enabled = !busy;
        _buttonOpenLog.Enabled = !busy;
        _buttonClear.Enabled = !busy;
    }

    public void UpdateStatus(McpServerStatusSnapshot status, IReadOnlyList<string> logs)
    {
        var launchConfig = status.LaunchConfig;

        _state.Text = $"State: {status.Message}";
        _state.ForeColor = status.State == McpServerRunState.Running ? AppTheme.Success : AppTheme.Danger;

        _transport.Text = $"Transport: {launchConfig.TransportType}";
        _endpoint.Text = string.Equals(launchConfig.TransportType, "stdio", StringComparison.OrdinalIgnoreCase)
            ? "Endpoint: n/a (stdio transport)"
            : $"Endpoint: {launchConfig.Host ?? "localhost"}:{(launchConfig.Port.HasValue ? launchConfig.Port.Value.ToString() : "(not configured)")}";

        _command.Text = $"Command: {launchConfig.CommandLineDisplay}";
        _source.Text = $"Config source: {launchConfig.Source}";
        _pid.Text = status.ProcessId.HasValue ? $"Managed PID: {status.ProcessId.Value}" : "Managed PID: (none)";

        var isRunning = status.State == McpServerRunState.Running;
        _buttonStart.Enabled = !isRunning;
        _buttonStop.Enabled = isRunning;

        _logs.Text = logs.Count == 0 ? "No logs yet." : string.Join(Environment.NewLine, logs);
        _logs.SelectionStart = _logs.TextLength;
        _logs.ScrollToCaret();
    }

    private static void ConfigureInfoLabel(Label label, int y, bool semibold, int height = 22)
    {
        label.Location = new Point(0, y);
        label.Size = new Size(980, height);
        label.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        label.Font = semibold ? new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold) : new Font("Segoe UI", 9.5F, FontStyle.Regular);
        label.ForeColor = AppTheme.TextBody;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.AutoEllipsis = true;
    }

    private static void ConfigureActionButton(Button button, string text, Point location, EventHandler handler)
    {
        button.Location = location;
        button.Size = new Size(92, 32);
        button.Text = text;
        AppTheme.StyleSecondaryButton(button);
        button.Click += handler;
        button.AccessibleName = text;
    }
}
