using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using System.Text.Json;

namespace CalbalamLauncher;

public sealed class MainForm : Form
{
    private readonly ComboBox versionBox = new();
    private readonly TextBox usernameBox = new();
    private readonly NumericUpDown ramBox = new();
    private readonly TextBox javaBox = new();
    private readonly Button launchButton = new();
    private readonly Button refreshButton = new();
    private readonly ProgressBar progress = new();
    private readonly Label status = new();
    private MinecraftLauncher? launcher;
    private readonly string settingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CalbalamLauncher", "settings.json");

    public MainForm()
    {
        Text = "CALBALAM Minecraft Launcher";
        Width = 760;
        Height = 520;
        MinimumSize = new Size(700, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 18, 22);
        ForeColor = Color.White;

        BuildUi();
        Shown += async (_, _) => await InitializeLauncherAsync();
    }

    private void BuildUi()
    {
        var title = new Label
        {
            Text = "CALBALAM",
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(36, 28)
        };

        var subtitle = new Label
        {
            Text = "Minecraft Custom Launcher",
            ForeColor = Color.Silver,
            AutoSize = true,
            Location = new Point(39, 72)
        };

        AddLabel("Minecraft Version", 40, 125);
        versionBox.Location = new Point(40, 150);
        versionBox.Width = 470;
        versionBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Style(versionBox);

        refreshButton.Text = "새로고침";
        refreshButton.Location = new Point(525, 149);
        refreshButton.Width = 110;
        Style(refreshButton);
        refreshButton.Click += async (_, _) => await RefreshVersionsAsync();

        AddLabel("플레이어 이름", 40, 205);
        usernameBox.Location = new Point(40, 230);
        usernameBox.Width = 595;
        Style(usernameBox);

        AddLabel("RAM (MB)", 40, 285);
        ramBox.Location = new Point(40, 310);
        ramBox.Width = 180;
        ramBox.Minimum = 1024;
        ramBox.Maximum = 32768;
        ramBox.Increment = 512;
        ramBox.Value = 4096;
        Style(ramBox);

        AddLabel("Java 경로 (선택)", 255, 285);
        javaBox.Location = new Point(255, 310);
        javaBox.Width = 380;
        Style(javaBox);

        launchButton.Text = "게임 실행";
        launchButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        launchButton.Location = new Point(40, 365);
        launchButton.Size = new Size(595, 52);
        launchButton.BackColor = Color.FromArgb(80, 160, 90);
        launchButton.ForeColor = Color.White;
        launchButton.FlatStyle = FlatStyle.Flat;
        launchButton.Click += async (_, _) => await LaunchAsync();

        progress.Location = new Point(40, 435);
        progress.Size = new Size(595, 12);

        status.Text = "준비 중...";
        status.ForeColor = Color.Silver;
        status.AutoSize = true;
        status.Location = new Point(40, 455);

        Controls.AddRange([title, subtitle, versionBox, refreshButton, usernameBox,
            ramBox, javaBox, launchButton, progress, status]);
    }

    private void AddLabel(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y),
            ForeColor = Color.Silver
        });
    }

    private static void Style(Control control)
    {
        control.BackColor = Color.FromArgb(35, 35, 42);
        control.ForeColor = Color.White;
        control.Font = new Font("Segoe UI", 10);
    }

    private async Task InitializeLauncherAsync()
    {
        try
        {
            LoadSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            launcher = new MinecraftLauncher(new MinecraftPath());
            await RefreshVersionsAsync();
            status.Text = "준비 완료";
        }
        catch (Exception ex)
        {
            status.Text = "초기화 실패";
            MessageBox.Show(ex.Message, "Launcher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshVersionsAsync()
    {
        if (launcher is null) return;

        try
        {
            refreshButton.Enabled = false;
            status.Text = "Minecraft 버전 목록을 불러오는 중...";
            var versions = await launcher.GetAllVersionsAsync();

            versionBox.Items.Clear();
            foreach (var version in versions)
                versionBox.Items.Add(version.Name);

            if (versionBox.Items.Count > 0)
                versionBox.SelectedItem = launcher.Versions.LatestReleaseName ?? versionBox.Items[0];

            status.Text = "버전 목록 준비 완료";
        }
        catch (Exception ex)
        {
            status.Text = "버전 목록 불러오기 실패";
            MessageBox.Show(ex.Message, "Version Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            refreshButton.Enabled = true;
        }
    }

    private async Task LaunchAsync()
    {
        if (launcher is null || string.IsNullOrWhiteSpace(versionBox.Text))
        {
            MessageBox.Show("Minecraft 버전을 선택하세요.");
            return;
        }

        var username = string.IsNullOrWhiteSpace(usernameBox.Text)
            ? "CalbalamPlayer"
            : usernameBox.Text.Trim();

        try
        {
            launchButton.Enabled = false;
            refreshButton.Enabled = false;
            progress.Style = ProgressBarStyle.Marquee;
            status.Text = "Minecraft 파일을 확인/설치하는 중...";

            SaveSettings();

            var option = new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(username),
                MaximumRamMb = (int)ramBox.Value
            };

            if (!string.IsNullOrWhiteSpace(javaBox.Text))
                option.JavaPath = javaBox.Text.Trim();

            var process = await launcher.InstallAndBuildProcessAsync(versionBox.Text, option);
            status.Text = "Minecraft 실행 중...";
            process.Start();
        }
        catch (Exception ex)
        {
            status.Text = "실행 실패";
            MessageBox.Show(ex.ToString(), "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progress.Style = ProgressBarStyle.Blocks;
            launchButton.Enabled = true;
            refreshButton.Enabled = true;
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsPath)) return;
            var settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(settingsPath));
            if (settings is null) return;

            usernameBox.Text = settings.Username ?? "";
            ramBox.Value = Math.Clamp(settings.RamMb, 1024, 32768);
            javaBox.Text = settings.JavaPath ?? "";
        }
        catch { }
    }

    private void SaveSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        var settings = new LauncherSettings
        {
            Username = usernameBox.Text.Trim(),
            RamMb = (int)ramBox.Value,
            JavaPath = javaBox.Text.Trim()
        };
        File.WriteAllText(settingsPath,
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class LauncherSettings
    {
        public string? Username { get; set; }
        public int RamMb { get; set; } = 4096;
        public string? JavaPath { get; set; }
    }
}
