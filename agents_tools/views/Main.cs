using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using agents_tools.controllers;
using agents_tools.models;
using agents_tools.views.components;

namespace agents_tools.views
{
    public partial class Main : Form
    {
        private readonly MainCoordinator _coordinator;
        private readonly CatalogBrowserControl _catalogBrowser = new();
        private readonly McpMonitorControl _mcpMonitor = new();
        private readonly Button _buttonMcp = new();
        private readonly System.Windows.Forms.Timer _mcpRefreshTimer = new();
        private readonly SemaphoreSlim _mcpRefreshGate = new(1, 1);

        private IReadOnlyList<CatalogEntry> _agentEntries = Array.Empty<CatalogEntry>();
        private IReadOnlyList<CatalogEntry> _toolEntries = Array.Empty<CatalogEntry>();

        public Main() : this(new MainCoordinator(new AssetCatalogService(), new InstallService(), new McpServerProcessService()))
        {
        }

        public Main(MainCoordinator coordinator)
        {
            _coordinator = coordinator;

            InitializeComponent();
            ApplyTheme();
            InitializeHostedViews();
            LoadCatalog();
            button1_Click(this, EventArgs.Empty);

            FormClosing += Main_FormClosing;
        }

        private void ApplyTheme()
        {
            splitContainer1.Panel1.BackColor = AppTheme.SidebarInk;
            splitContainer1.Panel2.BackColor = AppTheme.Canvas;

            labelTitle.Font = AppTheme.Heading;
            labelTitle.ForeColor = Color.White;

            labelHeader.Font = AppTheme.Heading;
            labelHeader.ForeColor = AppTheme.TextStrong;

            AppTheme.StyleNavButton(button1, "button1", "Agents", 1);
            AppTheme.StyleNavButton(button2, "button2", "Tools", 2);
            AppTheme.StyleNavButton(_buttonMcp, "buttonMcp", "MCP Server", 3);

            AppTheme.StylePrimaryButton(buttonInstall);
            buttonInstall.AccessibleName = "Install selected item";
            buttonInstall.AccessibleDescription = "Installs the selected agent or tool to the local GitHub agents directory.";

            progressBar1.Style = ProgressBarStyle.Continuous;
            labelStatus.ForeColor = AppTheme.TextMuted;
            labelStatus.Font = new Font("Segoe UI", 9F);
        }

        private void InitializeHostedViews()
        {
            _buttonMcp.Click += buttonMcp_Click;
            splitContainer1.Panel1.Controls.Add(_buttonMcp);
            _buttonMcp.BringToFront();

            splitContainer1.Panel2.Controls.Clear();
            splitContainer1.Panel2.Padding = new Padding(20, 0, 20, 20);

            labelHeader.Dock = DockStyle.Top;
            labelHeader.Height = 72;

            _catalogBrowser.Dock = DockStyle.Fill;
            _catalogBrowser.SelectionChanged += (_, _) => UpdateInstallAvailability();

            _mcpMonitor.Dock = DockStyle.Fill;
            _mcpMonitor.Visible = false;
            _mcpMonitor.StartRequested += async (_, _) => await StartMcpAsync();
            _mcpMonitor.StopRequested += async (_, _) => await StopMcpAsync();
            _mcpMonitor.RefreshRequested += async (_, _) => await RefreshMcpMonitorAsync();
            _mcpMonitor.OpenLogRequested += (_, _) => OpenMcpLog();
            _mcpMonitor.ClearLogsRequested += async (_, _) => await ClearMcpLogsAsync();

            splitContainer1.Panel2.Controls.Add(_mcpMonitor);
            splitContainer1.Panel2.Controls.Add(_catalogBrowser);
            splitContainer1.Panel2.Controls.Add(labelHeader);

            _mcpRefreshTimer.Interval = 2500;
            _mcpRefreshTimer.Tick += async (_, _) =>
            {
                if (_mcpMonitor.Visible)
                {
                    await RefreshMcpMonitorAsync();
                }
            };
            _mcpRefreshTimer.Start();

            UpdateInstallAvailability();
        }

        private void LoadCatalog()
        {
            var snapshot = _coordinator.LoadCatalog();
            _agentEntries = snapshot.Entries.Where(e => e.AssetType == CatalogAssetType.Agent).ToList();
            _toolEntries = snapshot.Entries.Where(e => e.AssetType == CatalogAssetType.Tool).ToList();

            if (snapshot.Diagnostics.Count > 0)
            {
                var preview = string.Join(Environment.NewLine, snapshot.Diagnostics.Take(3).Select(d => "- " + d));
                labelStatus.Text = $"Loaded with {snapshot.Diagnostics.Count} warnings";
                labelStatus.AccessibleDescription = preview;
            }
            else
            {
                labelStatus.Text = "Ready";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SetActiveCategory(button1, "Agents");
            panelInstall.Visible = true;
            _catalogBrowser.Visible = true;
            _mcpMonitor.Visible = false;
            _catalogBrowser.SetEntries(_agentEntries);
            UpdateInstallAvailability();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SetActiveCategory(button2, "Tools");
            panelInstall.Visible = true;
            _catalogBrowser.Visible = true;
            _mcpMonitor.Visible = false;
            _catalogBrowser.SetEntries(_toolEntries);
            UpdateInstallAvailability();
        }

        private async void buttonMcp_Click(object? sender, EventArgs e)
        {
            SetActiveCategory(_buttonMcp, "MCP Server Monitor");
            panelInstall.Visible = false;
            _catalogBrowser.Visible = false;
            _mcpMonitor.Visible = true;
            await RefreshMcpMonitorAsync();
        }

        private async void buttonInstall_Click(object sender, EventArgs e)
        {
            var selected = _catalogBrowser.SelectedEntry;
            if (selected is null)
            {
                MessageBox.Show(this, "Select an agent or tool to install.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            buttonInstall.Enabled = false;
            progressBar1.Value = 0;
            labelStatus.ForeColor = AppTheme.TextBody;
            labelStatus.Text = $"Installing {selected.Name}...";

            using var cts = new CancellationTokenSource();
            var progress = new Progress<InstallProgress>(p =>
            {
                var pct = Math.Min(100, Math.Max(0, p.Percentage));
                progressBar1.Value = pct;
                labelStatus.Text = $"Installing {selected.Name}: {pct}% ({p.BytesWritten}/{p.TotalBytes} bytes)";
            });

            try
            {
                await _coordinator.InstallAsync(selected, progress, cts.Token);
                progressBar1.Value = 100;
                labelStatus.ForeColor = AppTheme.Success;
                labelStatus.Text = $"Installed {selected.DisplayPrefix.ToLowerInvariant()} '{selected.Name}'.";
            }
            catch (Exception ex)
            {
                progressBar1.Value = 0;
                labelStatus.ForeColor = AppTheme.Danger;
                labelStatus.Text = $"Install failed for {selected.Name}.";
                MessageBox.Show(this, "Install failed: " + ex.Message, "Install", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonInstall.Enabled = true;
            }
        }

        private async Task StartMcpAsync()
        {
            _mcpMonitor.SetBusy(true);
            try
            {
                await _coordinator.StartMcpAsync();
                await RefreshMcpMonitorAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to start MCP server: " + ex.Message, "MCP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _mcpMonitor.SetBusy(false);
            }
        }

        private async Task StopMcpAsync()
        {
            _mcpMonitor.SetBusy(true);
            try
            {
                await _coordinator.StopMcpAsync();
                await RefreshMcpMonitorAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to stop MCP server: " + ex.Message, "MCP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _mcpMonitor.SetBusy(false);
            }
        }

        private async Task ClearMcpLogsAsync()
        {
            try
            {
                _coordinator.ClearMcpLogs();
                await RefreshMcpMonitorAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to clear logs: " + ex.Message, "MCP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenMcpLog()
        {
            try
            {
                if (!File.Exists(_coordinator.McpLogFilePath))
                {
                    File.WriteAllText(_coordinator.McpLogFilePath, string.Empty);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{_coordinator.McpLogFilePath}\"",
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
            if (!await _mcpRefreshGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                var status = _coordinator.GetMcpStatus();
                var logs = _coordinator.ReadRecentMcpLogLines(200);
                _mcpMonitor.UpdateStatus(status, logs);
            }
            finally
            {
                _mcpRefreshGate.Release();
            }
        }

        private void UpdateInstallAvailability()
        {
            buttonInstall.Enabled = _catalogBrowser.SelectedEntry is not null;
        }

        private void SetActiveCategory(Button active, string header)
        {
            foreach (var button in new[] { button1, button2, _buttonMcp })
            {
                var isActive = button == active;
                button.BackColor = isActive ? AppTheme.SidebarActive : AppTheme.SidebarInk;
                button.ForeColor = isActive ? Color.White : Color.FromArgb(224, 231, 255);
            }

            labelHeader.Text = header;
        }

        private void Main_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _mcpRefreshTimer.Stop();
            _mcpRefreshTimer.Dispose();
            _mcpRefreshGate.Dispose();
            _coordinator.Dispose();
        }
    }
}
