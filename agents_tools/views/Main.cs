using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace agents_tools.views
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Displays the list of agents in the list box when button1 is clicked.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            // Example agent list — replace with your real data source as needed.
            var agents = new[]
            {
                "Agent: Sentinel",
                "Agent: Pathfinder",
                "Agent: Observer",
                "Agent: Scout",
                "Agent: Operator"
            };

            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            listBox1.Items.AddRange(agents);
            if (listBox1.Items.Count > 0) listBox1.SelectedIndex = 0;
            listBox1.EndUpdate();
        }

        /// <summary>
        /// Displays the list of tools in the list box when button2 is clicked.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            // Example tools list — replace with your real data source as needed.
            var tools = new[]
            {
                "Tool: Logger",
                "Tool: Profiler",
                "Tool: Deployer",
                "Tool: Monitor",
                "Tool: Analyzer"
            };

            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            listBox1.Items.AddRange(tools);
            if (listBox1.Items.Count > 0) listBox1.SelectedIndex = 0;
            listBox1.EndUpdate();
        }
    }
}