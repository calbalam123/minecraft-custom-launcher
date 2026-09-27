using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.ProcessBuilder;
using System.Diagnostics;
using System.Text.Json;

namespace CalbalamLauncher;

public sealed class MainForm : Form
{
    private const string CurrentVersion = "1.0.0";
    private const string ReleasesUrl = "https://github.com/calbalam123/minecraft-custom-launcher/releases/latest";
    private const string ApiUrl = "https://api.github.com/repos/calbalam123/minecraft-custom-launcher/releases/latest";

    private readonly ComboBox versionBox = new();
    private readonly ComboBox profileBox = new();
    private readonly ComboBox serverBox = new();
    private readonly TextBox serverNameBox = new();
    private readonly TextBox serverAddressBox = new();
    private readonly NumericUpDown serverPortBox = new();
    private readonly TextBox usernameBox = new();
    private readonly NumericUpDown ramBox = new();
    private readonly TextBox javaBox = new();
    private readonly Button launchButton = new();
    private readonly Button loginButton = new();
    private readonly Button logoutButton = new();
    private readonly Button refreshButton = new();
    private readonly Button addServerButton = new();
    private readonly Button removeServerButton = new();
    private readonly Button updateButton = new();
    private readonly ProgressBar progress = new();
    private readonly Label status = new();
    private readonly Label accountStatus = new();

    private MinecraftLauncher? launcher;
    private JELoginHandler? loginHandler;
    private MSession? microsoftSession;
    private readonly List<ServerEntry> servers = [];
    private readonly string settingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CalbalamLauncher", "settings.json");

    public MainForm()
    {
        Text = "CALBALAM Minecraft Launcher";
        Width = 800;
        Height = 700;
        MinimumSize = new Size(760, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 18, 22);
        ForeColor = Color.White;

        BuildUi();
        Shown += async (_, _) => await InitializeLauncherAsync();
    }

    private void BuildUi()
    {
        var title = new Label { Text = "CALBALAM", Font = new Font("Segoe UI", 26, FontStyle.Bold), AutoSize = true, Location = new Point(36, 22) };
        var subtitle = new Label { Text = $"Minecraft Custom Launcher v{CurrentVersion}", ForeColor = Color.Silver, AutoSize = true, Location = new Point(39, 66) };

        accountStatus.Text = "계정: 오프라인";
        accountStatus.ForeColor = Color.Silver;
        accountStatus.AutoSize = true;
        accountStatus.Location = new Point(430, 30);

        loginButton.Text = "Microsoft 로그인"; loginButton.Location = new Point(430, 55); loginButton.Width = 120; StyleButton(loginButton);
        loginButton.Click += async (_, _) => await LoginAsync();

        logoutButton.Text = "로그아웃"; logoutButton.Location = new Point(560, 55); logoutButton.Width = 85; StyleButton(logoutButton);
        logoutButton.Enabled = false; logoutButton.Click += async (_, _) => await LogoutAsync();

        updateButton.Text = "업데이트 확인"; updateButton.Location = new Point(655, 55); updateButton.Width = 100; StyleButton(updateButton);
        updateButton.Click += async (_, _) => await CheckForUpdateAsync(true);

        AddLabel("Minecraft 버전", 40, 105);
        versionBox.Location = new Point(40, 130); versionBox.Width = 450; versionBox.DropDownStyle = ComboBoxStyle.DropDownList; Style(versionBox);

        refreshButton.Text = "새로고침"; refreshButton.Location = new Point(505, 129); refreshButton.Width = 100; StyleButton(refreshButton);
        refreshButton.Click += async (_, _) => await RefreshVersionsAsync();

        AddLabel("프로필 / 로더", 40, 175);
        profileBox.Location = new Point(40, 200); profileBox.Width = 565; profileBox.DropDownStyle = ComboBoxStyle.DropDownList; Style(profileBox);

        AddLabel("서버", 40, 245);
        serverBox.Location = new Point(40, 270); serverBox.Width = 565; serverBox.DropDownStyle = ComboBoxStyle.DropDownList; Style(serverBox);
        serverBox.SelectedIndexChanged += (_, _) => LoadSelectedServer();

        addServerButton.Text = "추가"; addServerButton.Location = new Point(615, 269); addServerButton.Width = 65; StyleButton(addServerButton);
        addServerButton.Click += (_, _) => AddServer();

        removeServerButton.Text = "삭제"; removeServerButton.Location = new Point(690, 269); removeServerButton.Width = 65; StyleButton(removeServerButton);
        removeServerButton.Click += (_, _) => RemoveServer();

        AddLabel("서버 이름", 40, 315);
        serverNameBox.Location = new Point(40, 340); serverNameBox.Width = 230; Style(serverNameBox);

        AddLabel("주소", 285, 315);
        serverAddressBox.Location = new Point(285, 340); serverAddressBox.Width = 275; Style(serverAddressBox);

        AddLabel("포트", 575, 315);
        serverPortBox.Location = new Point(575, 340); serverPortBox.Width = 85; serverPortBox.Minimum = 1; serverPortBox.Maximum = 65535; serverPortBox.Value = 25565; Style(serverPortBox);

        AddLabel("플레이어 이름", 40, 390);
        usernameBox.Location = new Point(40, 415); usernameBox.Width = 620; Style(usernameBox);

        AddLabel("RAM (MB)", 40, 460);
        ramBox.Location = new Point(40, 485); ramBox.Width = 180; ramBox.Minimum = 1024; ramBox.Maximum = 32768; ramBox.Increment = 512; ramBox.Value = 4096; Style(ramBox);

        AddLabel("Java 경로 (선택)", 245, 460);
        javaBox.Location = new Point(245, 485); javaBox.Width = 415; Style(javaBox);

        launchButton.Text = "게임 실행"; launchButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        launchButton.Location = new Point(40, 535); launchButton.Size = new Size(620, 48);
        launchButton.BackColor = Color.FromArgb(80, 160, 90); launchButton.ForeColor = Color.White; launchButton.FlatStyle = FlatStyle.Flat;
        launchButton.Click += async (_, _) => await LaunchAsync();

        progress.Location = new Point(40, 595); progress.Size = new Size(620, 12);
        status.Text = "준비 중..."; status.ForeColor = Color.Silver; status.AutoSize = true; status.Location = new Point(40, 615);

        Controls.AddRange([title, subtitle, accountStatus, loginButton, logoutButton, updateButton,
            versionBox, refreshButton, profileBox, serverBox, addServerButton, removeServerButton,
            serverNameBox, serverAddressBox, serverPortBox, usernameBox, ramBox, javaBox,
            launchButton, progress, status]);
    }

    private void AddLabel(string text, int x, int y) => Controls.Add(new Label {
        Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = Color.Silver
    });

    private static void Style(Control c)
    {
        c.BackColor = Color.FromArgb(35, 35, 42);
        c.ForeColor = Color.White;
        c.Font = new Font("Segoe UI", 10);
    }

    private static void StyleButton(Button b)
    {
        b.BackColor = Color.FromArgb(45, 45, 55);
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
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
            _ = CheckForUpdateAsync(false);
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
            status.Text = "버전 목록을 불러오는 중...";
            var versions = await launcher.GetAllVersionsAsync();

            versionBox.Items.Clear();
            profileBox.Items.Clear();
            profileBox.Items.Add("Vanilla");

            foreach (var v in versions)
            {
                versionBox.Items.Add(v.Name);
                if (IsCustomProfile(v.Name)) profileBox.Items.Add(v.Name);
            }

            if (versionBox.Items.Count > 0)
                versionBox.SelectedItem = launcher.Versions.LatestReleaseName ?? versionBox.Items[0];
            profileBox.SelectedIndex = 0;
            status.Text = "버전 목록 준비 완료";
        }
        catch (Exception ex)
        {
            status.Text = "버전 목록 실패";
            MessageBox.Show(ex.Message, "Version Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { refreshButton.Enabled = true; }
    }

    private static bool IsCustomProfile(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("fabric") || n.Contains("forge") || n.Contains("neoforge") || n.Contains("quilt") || n.Contains("optifine");
    }

    private async Task LoginAsync()
    {
        if (loginHandler is null) return;
        try
        {
            SetAuthUi(false);
            status.Text = "Microsoft 계정 로그인 중...";
            microsoftSession = await loginHandler.Authenticate();
            usernameBox.Text = microsoftSession.Username;
            accountStatus.Text = $"계정: {microsoftSession.Username}";
            accountStatus.ForeColor = Color.LightGreen;
            logoutButton.Enabled = true;
            SaveSettings();
            status.Text = "로그인 완료";
        }
        catch (Exception ex)
        {
            status.Text = "로그인 실패";
            MessageBox.Show(ex.ToString(), "Microsoft Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetAuthUi(true); }
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
            MessageBox.Show(ex.ToString(), "Logout Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { SetAuthUi(true); }
    }

    private void SetAuthUi(bool enabled)
    {
        loginButton.Enabled = enabled;
        logoutButton.Enabled = enabled && microsoftSession is not null;
        updateButton.Enabled = enabled;
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

            var selectedVersion = profileBox.Text == "Vanilla" ? versionBox.Text : profileBox.Text;
            var session = microsoftSession ?? MSession.CreateOfflineSession(
                string.IsNullOrWhiteSpace(usernameBox.Text) ? "CalbalamPlayer" : usernameBox.Text.Trim());

            var option = new MLaunchOption
            {
                Session = session,
                MaximumRamMb = (int)ramBox.Value
            };

            if (!string.IsNullOrWhiteSpace(javaBox.Text))
                option.JavaPath = javaBox.Text.Trim();

            if (serverBox.SelectedItem is ServerEntry server)
            {
                option.ServerIp = server.Address;
                option.ServerPort = server.Port;
            }

            var process = await launcher.InstallAndBuildProcessAsync(selectedVersion, option);
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

    private void AddServer()
    {
        var name = serverNameBox.Text.Trim();
        var address = serverAddressBox.Text.Trim();
        var port = (int)serverPortBox.Value;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
        {
            MessageBox.Show("서버 이름과 주소를 입력하세요.");
            return;
        }

        servers.Add(new ServerEntry { Name = name, Address = address, Port = port });
        RefreshServerBox();
        serverBox.SelectedIndex = servers.Count - 1;
        SaveSettings();
    }

    private void RemoveServer()
    {
        if (serverBox.SelectedIndex < 0 || serverBox.SelectedIndex >= servers.Count) return;
        servers.RemoveAt(serverBox.SelectedIndex);
        RefreshServerBox();
        SaveSettings();
    }

    private void RefreshServerBox()
    {
        serverBox.Items.Clear();
        serverBox.Items.Add("서버 접속 안 함");
        foreach (var s in servers) serverBox.Items.Add(s);
        serverBox.SelectedIndex = 0;
    }

    private void LoadSelectedServer()
    {
        if (serverBox.SelectedItem is not ServerEntry s) return;
        serverNameBox.Text = s.Name;
        serverAddressBox.Text = s.Address;
        serverPortBox.Value = Math.Clamp(s.Port, 1, 65535);
    }

    private async Task CheckForUpdateAsync(bool interactive)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CalbalamLauncher/1.0");
            var json = await client.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);

            var tag = doc.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v');
            if (Version.TryParse(tag, out var remote) &&
                Version.TryParse(CurrentVersion, out var local) &&
                remote > local)
            {
                if (MessageBox.Show($"새 버전 {tag}이 있습니다. 업데이트 페이지를 열까요?",
                    "업데이트", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo(ReleasesUrl) { UseShellExecute = true });
            }
            else if (interactive)
                MessageBox.Show("현재 최신 버전입니다.", "업데이트 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch
        {
            if (interactive)
                MessageBox.Show("업데이트 서버에 연결하지 못했습니다.", "업데이트 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                RefreshServerBox();
                return;
            }

            var settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(settingsPath));
            if (settings is null) return;

            usernameBox.Text = settings.Username ?? "";
            ramBox.Value = Math.Clamp(settings.RamMb, 1024, 32768);
            javaBox.Text = settings.JavaPath ?? "";
            servers.Clear();
            servers.AddRange(settings.Servers ?? []);
            RefreshServerBox();
        }
        catch { RefreshServerBox(); }
    }

    private void SaveSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        var settings = new LauncherSettings
        {
            Username = usernameBox.Text.Trim(),
            RamMb = (int)ramBox.Value,
            JavaPath = javaBox.Text.Trim(),
            Servers = servers
        };
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class LauncherSettings
    {
        public string? Username { get; set; }
        public int RamMb { get; set; } = 4096;
        public string? JavaPath { get; set; }
        public List<ServerEntry> Servers { get; set; } = [];
    }
}
