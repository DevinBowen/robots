namespace agents_tools.views.components;

using System.Drawing;
using System.Windows.Forms;

internal static class AppTheme
{
    public static readonly Color SidebarInk = Color.FromArgb(24, 32, 44);
    public static readonly Color SidebarActive = Color.FromArgb(37, 61, 88);
    public static readonly Color SidebarHover = Color.FromArgb(31, 48, 67);
    public static readonly Color Canvas = Color.FromArgb(248, 250, 252);
    public static readonly Color Surface = Color.White;
    public static readonly Color Accent = Color.FromArgb(0, 113, 188);
    public static readonly Color AccentMuted = Color.FromArgb(225, 238, 250);
    public static readonly Color Success = Color.FromArgb(21, 128, 61);
    public static readonly Color Danger = Color.FromArgb(185, 28, 28);
    public static readonly Color TextStrong = Color.FromArgb(17, 24, 39);
    public static readonly Color TextBody = Color.FromArgb(51, 65, 85);
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    public static readonly Color Border = Color.FromArgb(226, 232, 240);

    public static readonly Font Heading = new("Segoe UI Semibold", 16F, FontStyle.Bold);
    public static readonly Font SectionHeading = new("Segoe UI Semibold", 11F, FontStyle.Bold);
    public static readonly Font Body = new("Segoe UI", 10F, FontStyle.Regular);
    public static readonly Font Mono = new("Cascadia Mono", 9F, FontStyle.Regular);

    public static void StyleNavButton(Button button, string name, string text, int tabIndex)
    {
        button.BackColor = SidebarInk;
        button.Cursor = Cursors.Hand;
        button.Dock = DockStyle.Top;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = SidebarActive;
        button.FlatAppearance.MouseOverBackColor = SidebarHover;
        button.FlatStyle = FlatStyle.Flat;
        button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        button.ForeColor = Color.FromArgb(224, 231, 255);
        button.Name = name;
        button.Padding = new Padding(20, 0, 0, 0);
        button.Size = new Size(250, 44);
        button.TabIndex = tabIndex;
        button.Text = text;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.UseVisualStyleBackColor = false;
        button.AccessibleName = text;
    }

    public static void StylePrimaryButton(Button button)
    {
        button.BackColor = Accent;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 87, 145);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(8, 126, 202);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        button.UseVisualStyleBackColor = false;
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.BackColor = Surface;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = AccentMuted;
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = TextBody;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        button.UseVisualStyleBackColor = false;
    }
}
