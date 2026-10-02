namespace agents_tools.views
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            labelTitle = new Label();
            button1 = new Button();
            button2 = new Button();
            panelInstall = new Panel();
            labelStatus = new Label();
            progressBar1 = new ProgressBar();
            buttonInstall = new Button();
            labelHeader = new Label();
            listBox1 = new ListBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panelInstall.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Panel1.BackColor = Color.FromArgb(32, 36, 44);
            splitContainer1.Panel1.Controls.Add(panelInstall);
            splitContainer1.Panel1.Controls.Add(button2);
            splitContainer1.Panel1.Controls.Add(button1);
            splitContainer1.Panel1.Controls.Add(labelTitle);
            splitContainer1.Panel1MinSize = 240;
            splitContainer1.Panel2.BackColor = Color.White;
            splitContainer1.Panel2.Controls.Add(listBox1);
            splitContainer1.Panel2.Controls.Add(labelHeader);
            splitContainer1.Panel2.Padding = new Padding(24, 0, 24, 24);
            splitContainer1.Size = new Size(900, 520);
            splitContainer1.SplitterDistance = 250;
            splitContainer1.SplitterWidth = 1;
            splitContainer1.TabIndex = 0;
            // 
            // labelTitle
            // 
            labelTitle.Dock = DockStyle.Top;
            labelTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            labelTitle.ForeColor = Color.White;
            labelTitle.Name = "labelTitle";
            labelTitle.Padding = new Padding(20, 0, 0, 0);
            labelTitle.Size = new Size(250, 72);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "Agents && Tools";
            labelTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // button1
            // 
            ConfigureNavButton(button1, "button1", "Agents", 1);
            button1.Click += button1_Click;
            // 
            // button2
            // 
            ConfigureNavButton(button2, "button2", "Tools", 2);
            button2.Click += button2_Click;
            // 
            // panelInstall
            // 
            panelInstall.Controls.Add(buttonInstall);
            panelInstall.Controls.Add(progressBar1);
            panelInstall.Controls.Add(labelStatus);
            panelInstall.Dock = DockStyle.Bottom;
            panelInstall.Name = "panelInstall";
            panelInstall.Size = new Size(250, 140);
            panelInstall.TabIndex = 3;
            // 
            // labelStatus
            // 
            labelStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            labelStatus.AutoEllipsis = true;
            labelStatus.Font = new Font("Segoe UI", 9F);
            labelStatus.ForeColor = Color.FromArgb(170, 178, 190);
            labelStatus.Location = new Point(20, 8);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(210, 20);
            labelStatus.TabIndex = 0;
            labelStatus.Text = "Ready";
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(20, 32);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(210, 6);
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.TabIndex = 1;
            // 
            // buttonInstall
            // 
            buttonInstall.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buttonInstall.BackColor = Color.FromArgb(0, 120, 212);
            buttonInstall.Cursor = Cursors.Hand;
            buttonInstall.FlatAppearance.BorderSize = 0;
            buttonInstall.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 84, 150);
            buttonInstall.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 137, 232);
            buttonInstall.FlatStyle = FlatStyle.Flat;
            buttonInstall.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            buttonInstall.ForeColor = Color.White;
            buttonInstall.Location = new Point(20, 56);
            buttonInstall.Name = "buttonInstall";
            buttonInstall.Size = new Size(210, 44);
            buttonInstall.TabIndex = 2;
            buttonInstall.Text = "Install Selected";
            buttonInstall.UseVisualStyleBackColor = false;
            buttonInstall.Click += buttonInstall_Click;
            // 
            // labelHeader
            // 
            labelHeader.Dock = DockStyle.Top;
            labelHeader.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            labelHeader.ForeColor = Color.FromArgb(32, 36, 44);
            labelHeader.Name = "labelHeader";
            labelHeader.Size = new Size(650, 72);
            labelHeader.TabIndex = 1;
            labelHeader.Text = "Select a category";
            labelHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // listBox1
            // 
            listBox1.BackColor = Color.White;
            listBox1.BorderStyle = BorderStyle.None;
            listBox1.Dock = DockStyle.Fill;
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.Font = new Font("Segoe UI", 10.5F);
            listBox1.FormattingEnabled = true;
            listBox1.IntegralHeight = false;
            listBox1.ItemHeight = 40;
            listBox1.Name = "listBox1";
            listBox1.TabIndex = 0;
            listBox1.DrawItem += listBox1_DrawItem;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 520);
            Controls.Add(splitContainer1);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(640, 440);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Agents & Tools";
            panelInstall.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        private static void ConfigureNavButton(Button b, string name, string text, int tabIndex)
        {
            b.BackColor = Color.FromArgb(32, 36, 44);
            b.Cursor = Cursors.Hand;
            b.Dock = DockStyle.Top;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(62, 68, 82);
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 52, 64);
            b.FlatStyle = FlatStyle.Flat;
            b.Font = new Font("Segoe UI", 11F);
            b.ForeColor = Color.FromArgb(220, 224, 232);
            b.Name = name;
            b.Padding = new Padding(20, 0, 0, 0);
            b.Size = new Size(250, 48);
            b.TabIndex = tabIndex;
            b.Text = text;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.UseVisualStyleBackColor = false;
        }
        #endregion

        private SplitContainer splitContainer1;
        private Button button1;
        private Button button2;
        private Button buttonInstall;
        private ListBox listBox1;
        private ProgressBar progressBar1;
        private Label labelStatus;
        private Label labelTitle;
        private Label labelHeader;
        private Panel panelInstall;
    }
}