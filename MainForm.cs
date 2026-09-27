using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ProcessBuilder;
using System.Diagnostics;
using System.Text.Json;

namespace CalbalamLauncher;

public sealed class MainForm : Form
{
    private readonly ComboBox versionBox = new();
    private readonly ComboBox profileBox = new();
    private readonly TextBox usernameBox = new();
    private readonly NumericUpDown ramBox = new();
    private readonly TextBox javaBox = new();
    private readonly Button launchButton = new();
    private readonly Button loginButton = new();
    private readonly Button logoutButton = new();
    private readonly Button refreshButton = new();
    private readonly Button loaderHelpButton = new();
    private readonly ProgressBar progress = new();
    private readonly Label status = new();
    private readonly Label accountStatus = new();
    private MinecraftLauncher? launcher;
    private JELoginHandler? loginHandler;
    private MSession? microsoftSession;

    private readonly string settingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CalbalamLauncher", "settings.json");

    public MainForm()
    {
        Text = "CALBALAM Minecraft Launcher";
        Width = 780;
        Height = 610;
        MinimumSize = new Size(720, 560);
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
            Location = new Point(36, 25)
        };

        var subtitle = new Label
        {
            Text = "Minecraft Custom Launcher",
            ForeColor = Color.Silver,
            AutoSize = true,
            Location = new Point(39, 69)
        };

        accountStatus.Text = "계정: 오프라인";
        accountStatus.ForeColor = Color.Silver;
        accountStatus.AutoSize = true;
        accountStatus.Location = new Point(405, 42);

        loginButton.Text = "Microsoft 로그인";
        loginButton.Location = new Point(405, 67);
        loginButton.Width = 125;
        StyleButton(loginButton);
        loginButton.Click += async (_, _) => await LoginAsync();

        logoutButton.Text = "로그아웃";
        logoutButton.Location = new Point(540, 67);
        logoutButton.Width = 95;
        StyleButton(logoutButton);
        logoutButton.Enabled = false;
        logoutButton.Click += async (_, _) => await LogoutAsync();

        AddLabel("Minecraft 버전", 40, 115);
        versionBox.Location = new Point(40, 140);
        versionBox.Width = 470;
        versionBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Style(versionBox);

        refreshButton.Text = "새로고침";
        refreshButton.Location = new Point(525, 139);
        refreshButton.Width = 110;
        StyleButton(refreshButton);
        refreshButton.Click += async (_, _) => await RefreshVersionsAsync();

        AddLabel("프로필 / 로더", 40, 195);
        profileBox.Location = new Point(40, 220);
        profileBox.Width = 595;
        profileBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Style(profileBox);

        loaderHelpButton.Text = "로더 안내";
        loaderHelpButton.Location = new Point(40, 257);
        loaderHelpButton.Width = 110;
        StyleButton(loaderHelpButton);
        loaderHelpButton.Click += (_, _) => ShowLoaderInfo();

        AddLabel("플레이어 이름", 40, 300);
        usernameBox.Location = new Point(40, 325);
        usernameBox.Width = 595;
        Style(usernameBox);

        AddLabel("RAM (MB)", 40, 380);
        ramBox.Location = new Point(40, 405);
        ramBox.Width = 180;
        ramBox.Minimum = 1024;
        ramBox.Maximum = 32768;
        ramBox.Increment = 512;
        ramBox.Value = 4096;
        Style(ramBox);

        AddLabel("Java 경로 (선택)", 255, 380);
        javaBox.Location = new Point(255, 405);
        javaBox.Width = 380;
        Style(javaBox);

        launchButton.Text = "게임 실행";
        launchButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        launchButton.Location = new Point(40, 455);
        launchButton.Size = new Size(595, 48);
        launchButton.BackColor = Color.FromArgb(80, 160, 90);
        launchButton.ForeColor = Color.White;
        launchButton.FlatStyle = FlatStyle.Flat;
        launchButton.Click += async (_, _) => await LaunchAsync();

        progress.Location = new Point(40, 515);
        progress.Size = new Size(595, 12);

        status.Text = "준비 중...";
        status.ForeColor = Color.Silver;
        status.AutoSize = true;
        status.Location = new Point(40, 535);

        Controls.AddRange([title, subtitle, accountStatus, loginButton, logoutButton,
            versionBox, refreshButton, profileBox, loaderHelpButton, usernameBox,
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

    private static void StyleButton(Button button)
    {
        button.BackColor = Color.FromArgb(45, 45, 55);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
    }

    private async Task InitializeLauncherAsync()
    {
        try
        {
            LoadSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

            launcher = new MinecraftLauncher(new MinecraftPath());
            loginHandler = JELoginHandlerBuilder.BuildDefault();

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
            var names = versions.Select(v => v.Name).ToList();

            versionBox.Items.Clear();
            profileBox.Items.Clear();

            foreach (var name in names)
                versionBox.Items.Add(name);

            var customProfiles = names.Where(IsCustomProfile).ToList();
            profileBox.Items.Add("Vanilla");
            foreach (var name in customProfiles)
                profileBox.Items.Add(name);

            if (versionBox.Items.Count > 0)
                versionBox.SelectedItem =
                    launcher.Versions.LatestReleaseName ?? versionBox.Items[0];

            profileBox.SelectedIndex = 0;
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

    private static bool IsCustomProfile(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("fabric") ||
               n.Contains("forge") ||
               n.Contains("neoforge") ||
               n.Contains("quilt") ||
               n.Contains("optifine");
    }

    private async Task LoginAsync()
    {
        if (loginHandler is null) return;

        try
        {
            SetAuthUi(false);
            status.Text = "Microsoft 계정 로그인 창을 여는 중...";

            microsoftSession = await loginHandler.Authenticate();

            usernameBox.Text = microsoftSession.Username;
            accountStatus.Text = $"계정: {microsoftSession.Username}";
            accountStatus.ForeColor = Color.LightGreen;
            logoutButton.Enabled = true;
            status.Text = "Microsoft 로그인 완료";
            SaveSettings();
        }
        catch (Exception ex)
        {
            status.Text = "로그인 실패";
            MessageBox.Show(ex.ToString(), "Microsoft Login Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetAuthUi(true);
        }
    }

    private async Task LogoutAsync()
    {
        if (loginHandler is null) return;

        try
        {
            SetAuthUi(false);
            await loginHandler.SignoutWithBrowser();
            microsoftSession = null;
            accountStatus.Text = "계정: 오프라인";
            accountStatus.ForeColor = Color.Silver;
            logoutButton.Enabled = false;
            status.Text = "로그아웃 완료";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Logout Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetAuthUi(true);
        }
    }

    private void SetAuthUi(bool enabled)
    {
        loginButton.Enabled = enabled;
        logoutButton.Enabled = enabled && microsoftSession is not null;
        refreshButton.Enabled = enabled;
        launchButton.Enabled = enabled;
    }

    private async Task LaunchAsync()
    {
        if (launcher is null || string.IsNullOrWhiteSpace(versionBox.Text))
        {
            MessageBox.Show("Minecraft 버전을 선택하세요.");
            return;
        }

        try
        {
            launchButton.Enabled = false;
            refreshButton.Enabled = false;
            progress.Style = ProgressBarStyle.Marquee;
            status.Text = "Minecraft 파일을 확인/설치하는 중...";

            SaveSettings();

            var selectedVersion = versionBox.Text;
            var profile = profileBox.Text;

            if (profile != "Vanilla" && !string.IsNullOrWhiteSpace(profile))
                selectedVersion = profile;

            var session = microsoftSession ??
                MSession.CreateOfflineSession(
                    string.IsNullOrWhiteSpace(usernameBox.Text)
                        ? "CalbalamPlayer"
                        : usernameBox.Text.Trim());

            var option = new MLaunchOption
            {
                Session = session,
                MaximumRamMb = (int)ramBox.Value
            };

            if (!string.IsNullOrWhiteSpace(javaBox.Text))
                option.JavaPath = javaBox.Text.Trim();

            var process = await launcher.InstallAndBuildProcessAsync(selectedVersion, option);
            status.Text = "Minecraft 실행 중...";
            process.Start();
        }
        catch (Exception ex)
        {
            status.Text = "실행 실패";
            MessageBox.Show(ex.ToString(), "Launch Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progress.Style = ProgressBarStyle.Blocks;
            launchButton.Enabled = true;
            refreshButton.Enabled = true;
        }
    }

    private void ShowLoaderInfo()
    {
        MessageBox.Show(
            "Fabric / Forge / NeoForge / Quilt 등은 이미 설치되어 있는 커스텀 프로필을 자동으로 찾아 실행할 수 있습니다.\n\n" +
            "Fabric은 공식 Fabric Installer를 사용해 먼저 설치한 뒤 이 런처의 프로필 목록에서 선택하세요.\n" +
            "Forge는 버전별 설치 방식 차이가 있어 공식 Forge Installer로 설치한 뒤 프로필을 선택하는 방식을 사용합니다.",
            "모드 로더 안내",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsPath)) return;

            var settings = JsonSerializer.Deserialize<LauncherSettings>(
                File.ReadAllText(settingsPath));

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
            JsonSerializer.Serialize(settings,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class LauncherSettings
    {
        public string? Username { get; set; }
        public int RamMb { get; set; } = 4096;
        public string? JavaPath { get; set; }
    }
}
