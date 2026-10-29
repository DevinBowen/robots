using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using agents_tools.controllers;
using agents_tools.models;

namespace agents_tools.views
{
    public partial class Main : Form
    {
        private readonly AssetCatalogService _catalogService = new();
        private readonly InstallService _installService = new();
        private readonly McpServerProcessService _mcpServerService = new();
        private IReadOnlyList<AgentManifest> _agents = Array.Empty<AgentManifest>();
        private IReadOnlyList<ToolManifest> _tools = Array.Empty<ToolManifest>();
        private readonly List<Tuple<string, string>> _currentDisplayed = new();

        private readonly Button _buttonMcp = new();
        private readonly Panel _mcpPanel = new();
        private readonly Label _labelMcpState = new();
        private readonly Label _labelMcpTransport = new();
        private readonly Label _labelMcpEndpoint = new();
        private readonly Label _labelMcpCommand = new();
        private readonly Label _labelMcpSource = new();
        private readonly Label _labelMcpPid = new();
        private readonly Label _labelMcpNote = new();
        private readonly TextBox _textMcpLogs = new();
        private readonly Button _buttonMcpStart = new();
        private readonly Button _buttonMcpStop = new();
        private readonly Button _buttonMcpRefresh = new();
        private readonly Button _buttonMcpOpenLog = new();
        private readonly Button _buttonMcpClearLog = new();
        private readonly System.Windows.Forms.Timer _mcpRefreshTimer = new();

        public Main()
        {
            InitializeComponent();
            InitializeMcpMonitorUi();
            LoadCatalog();
            button1_Click(this, EventArgs.Empty);
            FormClosing += Main_FormClosing;
        }

        private void LoadCatalog()
        {
            _agents = _catalogService.GetAgents() ?? Array.Empty<AgentManifest>();
            _tools = _catalogService.GetTools() ?? Array.Empty<ToolManifest>();
        }

        /// <summary>
        /// Displays the list of agents in the list box when button1 is clicked.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            SetActiveCategory(button1, "Agents");
            panelInstall.Visible = true;
            listBox1.Visible = true;
            _mcpPanel.Visible = false;

            var lines = _agents.Select(a => "Agent: " + a.Name).ToArray();
            Render(lines);

            _currentDisplayed.Clear();
            _currentDisplayed.AddRange(_agents.Select(a => Tuple.Create("agent", a.Id)));
        }

        /// <summary>
        /// Displays the list of tools in the list box when button2 is clicked.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            SetActiveCategory(button2, "Tools");
            panelInstall.Visible = true;
            listBox1.Visible = true;
            _mcpPanel.Visible = false;

            var lines = _tools.Select(t => "Tool: " + t.Name).ToArray();
            Render(lines);

            _currentDisplayed.Clear();
            _currentDisplayed.AddRange(_tools.Select(t => Tuple.Create("tool", t.Id)));
        }

        private async void buttonMcp_Click(object? sender, EventArgs e)
        {
            SetActiveCategory(_buttonMcp, "MCP Server Monitor");
            _currentDisplayed.Clear();
            panelInstall.Visible = false;
            listBox1.Visible = false;
            _mcpPanel.Visible = true;
            await RefreshMcpMonitorAsync();
        }

        private async void buttonMcpStart_Click(object? sender, EventArgs e)
        {
            try
            {
                _buttonMcpStart.Enabled = false;
                await _mcpServerService.StartAsync();
                await RefreshMcpMonitorAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to start MCP server: " + ex.Message, "MCP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonMcpStop_Click(object? sender, EventArgs e)
        {
            try
            {
                _buttonMcpStop.Enabled = false;
                await _mcpServerService.StopAsync();
                await RefreshMcpMonitorAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to stop MCP server: " + ex.Message, "MCP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonMcpRefresh_Click(object? sender, EventArgs e)
        {
            await RefreshMcpMonitorAsync();
        }

        private async void buttonMcpClearLog_Click(object? sender, EventArgs e)
        {
            try
            {
                _mcpServerService.ClearLogs();
                await RefreshMcpMonitorAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to clear log: " + ex.Message, "MCP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonMcpOpenLog_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!System.IO.File.Exists(_mcpServerService.LogFilePath))
                {
                    System.IO.File.WriteAllText(_mcpServerService.LogFilePath, string.Empty);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{_mcpServerService.LogFilePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to open log file: " + ex.Message, "MCP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RefreshMcpMonitorAsync()
        {
            var status = _mcpServerService.GetStatus();
            var launchConfig = status.LaunchConfig;

            _labelMcpState.Text = $"State: {status.Message}";
            _labelMcpState.ForeColor = status.State == McpServerRunState.Running
                ? Color.FromArgb(0, 122, 64)
                : Color.FromArgb(155, 38, 44);

            _labelMcpTransport.Text = $"Transport: {launchConfig.TransportType}";
            var endpointText = string.Equals(launchConfig.TransportType, "stdio", StringComparison.OrdinalIgnoreCase)
                ? "Endpoint: n/a (stdio transport)"
                : $"Endpoint: {launchConfig.Host ?? "localhost"}:{(launchConfig.Port.HasValue ? launchConfig.Port.Value.ToString() : "(not configured)")}";
            _labelMcpEndpoint.Text = endpointText;

            _labelMcpCommand.Text = $"Command: {launchConfig.CommandLineDisplay}";
            _labelMcpSource.Text = $"Config source: {launchConfig.Source}";
            _labelMcpPid.Text = status.ProcessId.HasValue ? $"Managed PID: {status.ProcessId.Value}" : "Managed PID: (none)";

            var lines = _mcpServerService.ReadRecentLogLines(200);
            _textMcpLogs.Text = lines.Count == 0 ? "No logs yet." : string.Join(Environment.NewLine, lines);
            _textMcpLogs.SelectionStart = _textMcpLogs.Text.Length;
            _textMcpLogs.ScrollToCaret();

            var isRunning = status.State == McpServerRunState.Running;
            _buttonMcpStart.Enabled = !isRunning;
            _buttonMcpStop.Enabled = isRunning;

            await Task.CompletedTask;
        }

        private void InitializeMcpMonitorUi()
        {
            ConfigureNavButton(_buttonMcp, "buttonMcp", "MCP Server", 3);
            _buttonMcp.Click += buttonMcp_Click;
            splitContainer1.Panel1.Controls.Add(_buttonMcp);

            _mcpPanel.Dock = DockStyle.Fill;
            _mcpPanel.Visible = false;
            _mcpPanel.BackColor = Color.White;
            _mcpPanel.Padding = new Padding(0, 8, 0, 0);

            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 236,
                BackColor = Color.White
            };

            ConfigureActionButton(_buttonMcpStart, "Start", new Point(0, 172));
            _buttonMcpStart.Click += buttonMcpStart_Click;

            ConfigureActionButton(_buttonMcpStop, "Stop", new Point(95, 172));
            _buttonMcpStop.Click += buttonMcpStop_Click;

            ConfigureActionButton(_buttonMcpRefresh, "Refresh", new Point(190, 172));
            _buttonMcpRefresh.Click += buttonMcpRefresh_Click;

            ConfigureActionButton(_buttonMcpOpenLog, "Open Log", new Point(285, 172));
            _buttonMcpOpenLog.Click += buttonMcpOpenLog_Click;

            ConfigureActionButton(_buttonMcpClearLog, "Clear", new Point(380, 172));
            _buttonMcpClearLog.Click += buttonMcpClearLog_Click;

            ConfigureInfoLabel(_labelMcpState, new Point(0, 8), 1000, true, 24);
            ConfigureInfoLabel(_labelMcpTransport, new Point(0, 36), 1000, false, 20);
            ConfigureInfoLabel(_labelMcpEndpoint, new Point(0, 58), 1000, false, 20);
            ConfigureInfoLabel(_labelMcpCommand, new Point(0, 82), 1000, false, 38);
            ConfigureInfoLabel(_labelMcpSource, new Point(0, 122), 1000, false, 24);
            ConfigureInfoLabel(_labelMcpPid, new Point(0, 148), 1000, false, 20);
            ConfigureInfoLabel(_labelMcpNote, new Point(0, 208), 1000, false, 22);
            _labelMcpCommand.AutoEllipsis = true;
            _labelMcpSource.AutoEllipsis = true;
            _labelMcpNote.AutoEllipsis = true;
            _labelMcpNote.ForeColor = Color.FromArgb(98, 103, 115);
            _labelMcpNote.Text = "For stdio MCP servers, external instances are not port-discoverable.";

            panelTop.Controls.Add(_labelMcpState);
            panelTop.Controls.Add(_labelMcpTransport);
            panelTop.Controls.Add(_labelMcpEndpoint);
            panelTop.Controls.Add(_labelMcpCommand);
            panelTop.Controls.Add(_labelMcpSource);
            panelTop.Controls.Add(_labelMcpPid);
            panelTop.Controls.Add(_buttonMcpStart);
            panelTop.Controls.Add(_buttonMcpStop);
            panelTop.Controls.Add(_buttonMcpRefresh);
            panelTop.Controls.Add(_buttonMcpOpenLog);
            panelTop.Controls.Add(_buttonMcpClearLog);
            panelTop.Controls.Add(_labelMcpNote);

            _textMcpLogs.Dock = DockStyle.Fill;
            _textMcpLogs.Multiline = true;
            _textMcpLogs.ReadOnly = true;
            _textMcpLogs.ScrollBars = ScrollBars.Vertical;
            _textMcpLogs.BorderStyle = BorderStyle.FixedSingle;
            _textMcpLogs.BackColor = Color.White;
            _textMcpLogs.Font = new Font("Consolas", 9F);
            _textMcpLogs.WordWrap = false;

            _mcpPanel.Controls.Add(_textMcpLogs);
            _mcpPanel.Controls.Add(panelTop);
            splitContainer1.Panel2.Controls.Add(_mcpPanel);

            _mcpRefreshTimer.Interval = 2500;
            _mcpRefreshTimer.Tick += async (_, _) =>
            {
                if (_mcpPanel.Visible)
                {
                    await RefreshMcpMonitorAsync();
                }
            };
            _mcpRefreshTimer.Start();
        }

        private static void ConfigureActionButton(Button button, string text, Point location)
        {
            button.BackColor = Color.FromArgb(248, 249, 251);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(214, 219, 227);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(236, 241, 247);
            button.Size = new Size(88, 30);
            button.Location = location;
            button.Text = text;
            button.Font = new Font("Segoe UI", 9F);
            button.UseVisualStyleBackColor = false;
        }

        private static void ConfigureInfoLabel(Label label, Point location, int width, bool semibold, int height)
        {
            label.AutoSize = false;
            label.Location = location;
            label.Size = new Size(width, height);
            label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label.Font = semibold ? new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold) : new Font("Segoe UI", 9.5F);
            label.ForeColor = Color.FromArgb(40, 44, 52);
            label.TextAlign = ContentAlignment.MiddleLeft;
        }

        private async void buttonInstall_Click(object sender, EventArgs e)
        {
            var index = listBox1.SelectedIndex;
            if (index < 0 || index >= _currentDisplayed.Count)
            {
                MessageBox.Show(this, "Select an agent or tool to install.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var itemType = _currentDisplayed[index].Item1;
            var itemId = _currentDisplayed[index].Item2;

            buttonInstall.Enabled = false;
            progressBar1.Value = 0;
            labelStatus.Text = "Installing...";

            var cts = new CancellationTokenSource();

            var progress = new Progress<InstallProgress>(p =>
            {
                try
                {
                    var pct = Math.Min(100, Math.Max(0, p.Percentage));
                    progressBar1.Value = pct;
                    labelStatus.Text = $"{pct}% ({p.BytesWritten}/{p.TotalBytes} bytes)";
                }
                catch
                {
                    // ignore UI update errors
                }
            });

            try
            {
                if (string.Equals(itemType, "agent", StringComparison.OrdinalIgnoreCase))
                {
                    var agent = _agents.FirstOrDefault(a => string.Equals(a.Id, itemId, StringComparison.OrdinalIgnoreCase));
                    if (agent == null)
                    {
                        MessageBox.Show(this, "Agent not found.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    await _installService.InstallAgentAsync(agent, progress, cts.Token).ConfigureAwait(false);
                }
                else if (string.Equals(itemType, "tool", StringComparison.OrdinalIgnoreCase))
                {
                    var tool = _tools.FirstOrDefault(t => string.Equals(t.Id, itemId, StringComparison.OrdinalIgnoreCase));
                    if (tool == null)
                    {
                        MessageBox.Show(this, "Tool not found.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    await _installService.InstallToolAsync(tool, progress, cts.Token).ConfigureAwait(false);
                }
                else
                {
                    MessageBox.Show(this, "Unknown asset type.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                this.Invoke(() => MessageBox.Show(this, "Install failed: " + ex.Message, "Install", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            finally
            {
                this.Invoke(() =>
                {
                    progressBar1.Value = 100;
                    labelStatus.Text = "Done";
                    buttonInstall.Enabled = true;
                });
            }
        }

        private void SetActiveCategory(Button active, string header)
        {
            foreach (var b in new[] { button1, button2, _buttonMcp })
            {
                var isActive = b == active;
                b.BackColor = isActive ? Color.FromArgb(62, 68, 82) : Color.FromArgb(32, 36, 44);
                b.ForeColor = isActive ? Color.White : Color.FromArgb(220, 224, 232);
            }

            labelHeader.Text = header;
        }

        private void Main_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _mcpRefreshTimer.Stop();
            _mcpServerService.Dispose();
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
            {
                return;
            }

            var selected = (e.State & DrawItemState.Selected) != 0;
            using var back = new SolidBrush(selected ? Color.FromArgb(222, 236, 249) : Color.White);
            using var fore = new SolidBrush(selected ? Color.FromArgb(0, 84, 150) : Color.FromArgb(40, 44, 52));
            using var line = new Pen(Color.FromArgb(238, 240, 244));

            e.Graphics.FillRectangle(back, e.Bounds);
            var format = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            var textBounds = new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 24, e.Bounds.Height);
            e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), e.Font, fore, textBounds, format);
            e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void Render(string[] lines)
        {
            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            listBox1.Items.AddRange(lines);
            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;
            }

            listBox1.EndUpdate();
        }
    }
}
