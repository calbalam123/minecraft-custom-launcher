using System.Net.Http;

namespace CalbalamLauncher;

public sealed class LoadingForm : Form
{
    private const string BackgroundUrl = "https://raw.githubusercontent.com/peunsu/MRSLauncher/master/app/assets/images/backgrounds/0.png";
    private readonly PictureBox background = new();
    private readonly Label title = new();
    private readonly Label detail = new();
    private readonly ProgressBar progress = new();

    public LoadingForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 430);
        MinimumSize = ClientSize;
        MaximumSize = ClientSize;
        BackColor = Color.FromArgb(8, 11, 16);
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;

        background.Dock = DockStyle.Fill;
        background.SizeMode = PictureBoxSizeMode.Zoom;
        background.BackColor = Color.FromArgb(8, 11, 16);
        background.Paint += (_, e) =>
        {
            using var overlay = new SolidBrush(Color.FromArgb(145, 0, 0, 0));
            e.Graphics.FillRectangle(overlay, background.ClientRectangle);
        };
        Controls.Add(background);

        var glass = new Panel
        {
            Size = new Size(560, 250),
            Location = new Point(100, 90),
            BackColor = Color.FromArgb(205, 8, 12, 18),
            Padding = new Padding(34)
        };
        glass.Paint += (_, e) => DrawBorder(e.Graphics, glass.ClientRectangle);

        title.Text = "CALBALAM";
        title.Dock = DockStyle.Top;
        title.Height = 55;
        title.Font = new Font("Segoe UI", 30, FontStyle.Bold);
        title.ForeColor = Color.White;

        detail.Text = "Minecraft Launcher를 준비하는 중...";
        detail.Dock = DockStyle.Top;
        detail.Height = 42;
        detail.Font = new Font("Segoe UI", 10);
        detail.ForeColor = Color.FromArgb(200, 215, 228);

        progress.Dock = DockStyle.Bottom;
        progress.Height = 8;
        progress.Minimum = 0;
        progress.Maximum = 100;
        progress.Value = 0;
        progress.Style = ProgressBarStyle.Continuous;

        glass.Controls.Add(progress);
        glass.Controls.Add(detail);
        glass.Controls.Add(title);
        Controls.Add(glass);
        Shown += async (_, _) => await LoadBackgroundAsync();
    }

    public void SetStatus(string text, int percent)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(text, percent));
            return;
        }
        detail.Text = text;
        progress.Value = Math.Clamp(percent, 0, 100);
    }

    private async Task LoadBackgroundAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CalbalamLauncher/1.1");
            var bytes = await http.GetByteArrayAsync(BackgroundUrl);
            using var ms = new MemoryStream(bytes);
            using var img = Image.FromStream(ms);
            background.Image = new Bitmap(img);
        }
        catch
        {
            // Loading screen remains usable with the fallback color.
        }
    }

    private static void DrawBorder(Graphics g, Rectangle rect)
    {
        using var pen = new Pen(Color.FromArgb(75, 255, 255, 255), 1);
        g.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
    }
}
