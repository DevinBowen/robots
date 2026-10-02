using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using agents_tools.controllers;
using agents_tools.models;
using System.Threading;
using System.Threading.Tasks;

namespace agents_tools.views
{
    public partial class Main : Form
    {
        private readonly AssetCatalogService _catalogService = new AssetCatalogService();
        private readonly InstallService _installService = new InstallService();
        private IReadOnlyList<AgentManifest> _agents = Array.Empty<AgentManifest>();
        private IReadOnlyList<ToolManifest> _tools = Array.Empty<ToolManifest>();
        private readonly List<Tuple<string, string>> _currentDisplayed = new List<Tuple<string, string>>();

        public Main()
        {
            InitializeComponent();
            LoadCatalog();
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
            var lines = _tools.Select(t => "Tool: " + t.Name).ToArray();
            Render(lines);

            _currentDisplayed.Clear();
            _currentDisplayed.AddRange(_tools.Select(t => Tuple.Create("tool", t.Id)));
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
                    //this.Invoke(() => MessageBox.Show(this, $"Agent '{agent.Name}' installed.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Information));
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
                    //this.Invoke(() => MessageBox.Show(this, $"Tool '{tool.Name}' installed.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Information));
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

        private void Render(string[] lines)
        {
            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            listBox1.Items.AddRange(lines);
            if (listBox1.Items.Count > 0) listBox1.SelectedIndex = 0;
            listBox1.EndUpdate();
        }
    }
}