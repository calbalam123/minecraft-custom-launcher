namespace CalbalamLauncher;

public sealed class LaunchOverlay : Form
{
    private readonly Label title = new();
    private readonly Label detail = new();
    private readonly ProgressBar progress = new();
    private readonly PictureBox background = new();

    public LaunchOverlay(Form owner, Image? source)
    {
        Owner = owner;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 360);
        BackColor = Color.FromArgb(8, 11, 16);
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;

        background.Dock = DockStyle.Fill;
        background.SizeMode = PictureBoxSizeMode.Zoom;
        background.Image = source is null ? null : new Bitmap(source);
        background.Paint += (_, e) =>
        {
            using var overlay = new SolidBrush(Color.FromArgb(178, 0, 0, 0));
            e.Graphics.FillRectangle(overlay, background.ClientRectangle);
        };
        Controls.Add(background);

        var glass = new Panel
        {
            Size = new Size(480, 220),
            Location = new Point(70, 70),
            BackColor = Color.FromArgb(220, 8, 12, 18),
            Padding = new Padding(28)
        };
        glass.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, glass.Width - 1, glass.Height - 1);
        };

        title.Text = "CALBALAM";
        title.Dock = DockStyle.Top;
        title.Height = 48;
        title.Font = new Font("Segoe UI", 24, FontStyle.Bold);
        title.ForeColor = Color.White;

        detail.Text = "Minecraft를 준비하는 중...";
        detail.Dock = DockStyle.Top;
        detail.Height = 38;
        detail.Font = new Font("Segoe UI", 10);
        detail.ForeColor = Color.FromArgb(205, 218, 230);

        progress.Dock = DockStyle.Bottom;
        progress.Height = 9;
        progress.Minimum = 0;
        progress.Maximum = 100;
        progress.Style = ProgressBarStyle.Continuous;

        glass.Controls.Add(progress);
        glass.Controls.Add(detail);
        glass.Controls.Add(title);
        Controls.Add(glass);
    }

    public void SetProgress(int percent, string message)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => SetProgress(percent, message));
            return;
        }
        progress.Value = Math.Clamp(percent, 0, 100);
        detail.Text = message;
    }
}
