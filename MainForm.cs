using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Installers;
using CmlLib.Core.ProcessBuilder;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CalbalamLauncher;

public sealed class MainForm : Form
{
    private const string CurrentVersion = "1.1.0";
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
    private readonly Button javaBrowseButton = new();
    private readonly Button autoRamButton = new();
    private readonly Button launchButton = new();
    private readonly Button cancelButton = new();
    private readonly Button loginButton = new();
    private readonly Button logoutButton = new();
    private readonly Button refreshButton = new();
    private readonly Button addServerButton = new();
    private readonly Button removeServerButton = new();
    private readonly Button updateButton = new();
    private readonly Button advancedButton = new();
    private readonly ProgressBar progress = new();
    private readonly Label status = new();
    private readonly Label accountStatus = new();

    private MinecraftLauncher? launcher;
    private JELoginHandler? loginHandler;
    private MSession? microsoftSession;
    private CancellationTokenSource? launchCancellation;
    private string gameDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
    private readonly List<ServerEntry> servers = [];
    private readonly string settingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CalbalamLauncher", "settings.json");

    public MainForm(LoadingForm? loadingScreen = null)
    {
        splash = loadingScreen;
        Text = "CALBALAM Minecraft Launcher";
        ClientSize = new Size(1400, 820);
        MinimumSize = new Size(1180, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 18, 22);
        ForeColor = Color.White;

        BuildUi();
        FormClosing += (_, _) =>
        {
            launchCancellation?.Cancel();
            SaveSettingsSafely();
        };
        Shown += async (_, _) => await InitializeLauncherAsync();
    }

    private Panel contentPanel = new();
    private Panel rightPanel = new();
    private Label heroTitle = new();
    private Label heroSubtitle = new();
    private Label selectedServerLabel = new();
    private Label selectedVersionLabel = new();
    private PictureBox? background;
    private PictureBox? accountAvatar;
    private LoadingForm? splash;

    private void BuildUi()
    {
        Controls.Clear();
        DoubleBuffered = true;

        var root = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 11, 16) };
        background = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(8, 11, 16) };
        background.Paint += (_, e) => { using var shade = new SolidBrush(Color.FromArgb(128, 0, 0, 0)); e.Graphics.FillRectangle(shade, background.ClientRectangle); };
        root.Controls.Add(background);

        var left = new Panel { Dock = DockStyle.Left, Width = 230, Padding = new Padding(22, 26, 14, 20), BackColor = Color.FromArgb(218, 7, 11, 17) };
        left.Controls.Add(new Label { Text = $"Minecraft Launcher  •  v{CurrentVersion}", Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 8), ForeColor = Color.FromArgb(160, 180, 200) });
        left.Controls.Add(new Label { Text = "◆  CALBALAM", Dock = DockStyle.Top, Height = 42, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.White });
        var nav = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 395, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 20, 0, 0), BackColor = Color.Transparent };
        Button Nav(string icon, string label, Action? action = null, bool active = false)
        {
            var b = new Button { Text = $"{icon}   {label}", Width = 192, Height = 50, Margin = new Padding(0, 0, 0, 7), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = active ? Color.FromArgb(35, 50, 72) : Color.Transparent, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = active ? 1 : 0; b.FlatAppearance.BorderColor = Color.FromArgb(30, 130, 235);
            if (action is not null) b.Click += (_, _) => action();
            return b;
        }
        nav.Controls.Add(Nav("⌂", "홈", null, true));
        nav.Controls.Add(Nav("♟", "프로필", OpenProfile));
        nav.Controls.Add(Nav("▣", "서버", () => { serverBox.Focus(); serverBox.DroppedDown = true; }));
        nav.Controls.Add(Nav("⚒", "모드", () => _ = OpenAdvancedToolsAsync()));
        nav.Controls.Add(Nav("▤", "게임 관리", () => _ = OpenAdvancedToolsAsync()));
        nav.Controls.Add(Nav("⚙", "설정", () => _ = OpenAdvancedToolsAsync()));
        left.Controls.Add(nav);
        var leftBottom = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        leftBottom.Controls.Add(new Label { Text = "●  온라인   |   " + CurrentVersion, Dock = DockStyle.Bottom, Height = 30, Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(70, 225, 125) });
        leftBottom.Controls.Add(new Label { Text = "■  Minecraft\n   더 넓은 세상을 경험하세요.", Dock = DockStyle.Bottom, Height = 62, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White });
        left.Controls.Add(leftBottom);

        var right = new Panel { Dock = DockStyle.Right, Width = 265, Padding = new Padding(18, 22, 18, 20), BackColor = Color.FromArgb(220, 7, 11, 17) };
        var account = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.Transparent };
        accountAvatar = new PictureBox { Location = new Point(0, 0), Size = new Size(54, 54), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(30, 45, 58), Image = CreateAvatarPlaceholder() };
        account.Controls.Add(accountAvatar);
        account.Controls.Add(new Label { Text = "CALBALAM", Location = new Point(66, 2), Size = new Size(170, 24), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White });
        accountStatus.AutoSize = false; accountStatus.Location = new Point(66, 29); accountStatus.Size = new Size(175, 36); accountStatus.Text = "Microsoft 계정 연결 안 됨"; accountStatus.Font = new Font("Segoe UI", 8); accountStatus.ForeColor = Color.FromArgb(155, 170, 185);
        account.Controls.Add(accountStatus); right.Controls.Add(account);
        loginButton.Text = "Microsoft 계정 로그인"; loginButton.Dock = DockStyle.Top; loginButton.Height = 42; StyleButton(loginButton, true); loginButton.Click += async (_, _) => await LoginAsync(); right.Controls.Add(loginButton);
        logoutButton.Text = "로그아웃"; logoutButton.Dock = DockStyle.Top; logoutButton.Height = 36; logoutButton.Enabled = false; logoutButton.Margin = new Padding(0, 6, 0, 0); StyleButton(logoutButton); logoutButton.Click += async (_, _) => await LogoutAsync(); right.Controls.Add(logoutButton);
        advancedButton.Text = "⚙   고급 관리"; advancedButton.Dock = DockStyle.Top; advancedButton.Height = 42; advancedButton.Margin = new Padding(0, 18, 0, 0); StyleButton(advancedButton); advancedButton.Click += async (_, _) => await OpenAdvancedToolsAsync(); right.Controls.Add(advancedButton);
        updateButton.Text = "↻   업데이트 확인"; updateButton.Dock = DockStyle.Top; updateButton.Height = 42; updateButton.Margin = new Padding(0, 6, 0, 0); StyleButton(updateButton); updateButton.Click += async (_, _) => await CheckForUpdateAsync(true); right.Controls.Add(updateButton);
        var rightFill = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        rightFill.Controls.Add(new Label { Text = "CALBALAM\n\nPlay Minecraft\nwith Calbalam", Dock = DockStyle.Bottom, Height = 112, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.White });
        right.Controls.Add(rightFill);

        var center = new Panel { Dock = DockStyle.Fill, Padding = new Padding(52, 28, 52, 22), BackColor = Color.Transparent };
        var hero = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = Color.Transparent };
        hero.Controls.Add(new Label { Text = "오늘도 즐거운 마인크래프트 되세요!", Dock = DockStyle.Bottom, Height = 32, Font = new Font("Segoe UI", 10), ForeColor = Color.White });
        heroTitle.Text = "CALBALAM  ♛"; heroTitle.Dock = DockStyle.Bottom; heroTitle.Height = 52; heroTitle.Font = new Font("Segoe UI", 28, FontStyle.Bold); heroTitle.ForeColor = Color.White;
        heroSubtitle.Text = "안녕하세요,"; heroSubtitle.Dock = DockStyle.Bottom; heroSubtitle.Height = 27; heroSubtitle.Font = new Font("Segoe UI", 11); heroSubtitle.ForeColor = Color.White;
        hero.Controls.Add(heroTitle); hero.Controls.Add(heroSubtitle); center.Controls.Add(hero);

        var card = new Panel { Dock = DockStyle.Top, Height = 400, Padding = new Padding(16), BackColor = Color.FromArgb(198, 12, 16, 22) };
        var selectorLabels = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Color.Transparent };
        selectorLabels.Controls.Add(new Label { Text = "버전 선택", Dock = DockStyle.Left, Width = 300, Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(125, 195, 250) });
        selectorLabels.Controls.Add(new Label { Text = "프로필 선택", Dock = DockStyle.Left, Width = 300, Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(125, 195, 250) });
        refreshButton.Text = "↻"; refreshButton.Dock = DockStyle.Right; refreshButton.Width = 36; StyleButton(refreshButton); refreshButton.Click += async (_, _) => await RefreshVersionsAsync();
        selectorLabels.Controls.Add(refreshButton); card.Controls.Add(selectorLabels);
        var selector = new TableLayoutPanel { Dock = DockStyle.Top, Height = 52, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        versionBox.Dock = DockStyle.Fill; versionBox.DropDownStyle = ComboBoxStyle.DropDownList; Style(versionBox);
        profileBox.Dock = DockStyle.Fill; profileBox.DropDownStyle = ComboBoxStyle.DropDownList; Style(profileBox);
        selector.Controls.Add(versionBox, 0, 0); selector.Controls.Add(profileBox, 1, 0); card.Controls.Add(selector);

        var loaders = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70, WrapContents = false, Padding = new Padding(0, 8, 0, 0), BackColor = Color.Transparent };
        foreach (var pair in new[] { ("■", "Vanilla"), ("✦", "Fabric"), ("⚒", "Forge"), ("✥", "NeoForge"), ("◇", "Quilt"), ("OF", "OptiFine") })
        {
            var b = new Button { Text = pair.Item1 + Environment.NewLine + pair.Item2, Width = 108, Height = 56, Margin = new Padding(3), Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.White, BackColor = pair.Item2 == "Vanilla" ? Color.FromArgb(32, 70, 115) : Color.FromArgb(27, 33, 42), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = pair.Item2 == "Vanilla" ? Color.FromArgb(35, 145, 245) : Color.FromArgb(50, 58, 70);
            b.Click += (_, _) => { var item = pair.Item2 == "Vanilla" ? "Vanilla" : profileBox.Items.Cast<object?>().FirstOrDefault(x => x?.ToString()?.Contains(pair.Item2, StringComparison.OrdinalIgnoreCase) == true)?.ToString(); if (item is not null) profileBox.SelectedItem = item; };
            loaders.Controls.Add(b);
        }
        card.Controls.Add(loaders);

        var serverTitle = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.Transparent };
        serverTitle.Controls.Add(new Label { Text = "서버 선택", Dock = DockStyle.Left, Width = 110, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(120, 195, 255) });
        addServerButton.Text = "+ 서버 추가"; addServerButton.Dock = DockStyle.Right; addServerButton.Width = 105; StyleButton(addServerButton); addServerButton.Click += (_, _) => AddServer();
        removeServerButton.Text = "× 서버 삭제"; removeServerButton.Dock = DockStyle.Right; removeServerButton.Width = 105; removeServerButton.Margin = new Padding(5, 0, 0, 0); StyleButton(removeServerButton); removeServerButton.Click += (_, _) => RemoveServer();
        serverTitle.Controls.Add(addServerButton); serverTitle.Controls.Add(removeServerButton); card.Controls.Add(serverTitle);
        serverBox.Dock = DockStyle.Top; serverBox.Height = 36; serverBox.DropDownStyle = ComboBoxStyle.DropDownList; Style(serverBox); serverBox.SelectedIndexChanged += (_, _) => { LoadSelectedServer(); UpdateServerSummary(); }; card.Controls.Add(serverBox);

        var fields = new TableLayoutPanel { Dock = DockStyle.Top, Height = 76, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent };
        for (var i = 0; i < 3; i++) fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        ramBox.Dock = DockStyle.Fill; ramBox.Minimum = 1024; ramBox.Maximum = 32768; ramBox.Increment = 512; ramBox.Value = 4096; Style(ramBox);
        javaBox.Dock = DockStyle.Fill; Style(javaBox); usernameBox.Dock = DockStyle.Fill; Style(usernameBox);
        autoRamButton.Text = "자동"; autoRamButton.Width = 48; StyleButton(autoRamButton); autoRamButton.Click += (_, _) => SetAutoRam();
        javaBrowseButton.Text = "..."; javaBrowseButton.Width = 40; StyleButton(javaBrowseButton); javaBrowseButton.Click += (_, _) => BrowseJava();
        Panel Field(string title, Control c, Button? extra = null)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4), BackColor = Color.Transparent };
            p.Controls.Add(c); if (extra is not null) { extra.Dock = DockStyle.Right; p.Controls.Add(extra); }
            p.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 21, Font = new Font("Segoe UI", 7, FontStyle.Bold), ForeColor = Color.FromArgb(145, 175, 200) }); return p;
        }
        fields.Controls.Add(Field("RAM 설정", ramBox, autoRamButton), 0, 0); fields.Controls.Add(Field("Java 경로", javaBox, javaBrowseButton), 1, 0); fields.Controls.Add(Field("플레이어 이름", usernameBox), 2, 0); card.Controls.Add(fields);

        serverNameBox.Visible = false; serverAddressBox.Visible = false; serverPortBox.Visible = false;
        var launch = new Panel { Dock = DockStyle.Fill, Padding = new Padding(195, 10, 195, 0), BackColor = Color.Transparent };
        launchButton.Text = "▶   게임 실행"; launchButton.Dock = DockStyle.Top; launchButton.Height = 56; launchButton.Font = new Font("Segoe UI", 13, FontStyle.Bold); launchButton.BackColor = Color.FromArgb(24, 125, 235); launchButton.ForeColor = Color.White; launchButton.FlatStyle = FlatStyle.Flat; launchButton.FlatAppearance.BorderSize = 0; launchButton.Click += async (_, _) => await LaunchAsync(); launch.Controls.Add(launchButton);
        selectedServerLabel.Text = "●  서버를 선택하지 않음"; selectedServerLabel.Dock = DockStyle.Bottom; selectedServerLabel.Height = 28; selectedServerLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold); selectedServerLabel.ForeColor = Color.FromArgb(175, 195, 212);
        selectedVersionLabel.Text = "Minecraft"; selectedVersionLabel.Dock = DockStyle.Bottom; selectedVersionLabel.Height = 22; selectedVersionLabel.TextAlign = ContentAlignment.MiddleRight; selectedVersionLabel.Font = new Font("Segoe UI", 8, FontStyle.Bold); selectedVersionLabel.ForeColor = Color.FromArgb(175, 195, 212);
        launch.Controls.Add(selectedVersionLabel); launch.Controls.Add(selectedServerLabel); card.Controls.Add(launch);
        center.Controls.Add(card);

        var bottom = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0), BackColor = Color.Transparent };
        var rec = new Panel { Dock = DockStyle.Left, Width = 610, BackColor = Color.FromArgb(198, 12, 16, 22), Padding = new Padding(12) };
        rec.Controls.Add(new Label { Text = "추천 서버", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White });
        var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.Transparent };
        foreach (var item in new[] { ("Hypixel", "mc.hypixel.net"), ("Mineplex", "us.mineplex.com"), ("SkyBlock", "skyblock.example.com") })
        {
            var c = new Panel { Width = 185, Height = 100, Margin = new Padding(3), BackColor = Color.FromArgb(30, 40, 52), Padding = new Padding(7) };
            var icon = new PictureBox { Dock = DockStyle.Left, Width = 70, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(20, 25, 32) };
            var textPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            textPanel.Controls.Add(new Label { Text = "● 서버", Dock = DockStyle.Bottom, Height = 18, Font = new Font("Segoe UI", 7, FontStyle.Bold), ForeColor = Color.FromArgb(85, 215, 125) });
            textPanel.Controls.Add(new Label { Text = item.Item2, Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 7), ForeColor = Color.FromArgb(160, 180, 198) });
            textPanel.Controls.Add(new Label { Text = item.Item1, Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White });
            c.Controls.Add(textPanel); c.Controls.Add(icon); cards.Controls.Add(c); _ = LoadServerIconAsync(icon, item.Item2);
        }
        rec.Controls.Add(cards);
        var news = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(198, 12, 16, 22), Padding = new Padding(12) };
        news.Controls.Add(new Label { Text = "공지사항", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White });
        news.Controls.Add(new Label { Text = "• 런처 v1.1.0 정식 출시\n• Fabric / Forge / NeoForge / Quilt 설치 지원\n• 서버 · 모드 · Java 관리 업데이트\n• Minecraft 버전 자동 동기화", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8), ForeColor = Color.FromArgb(190, 205, 220), Padding = new Padding(0, 7, 0, 0) });
        bottom.Controls.Add(news); bottom.Controls.Add(rec); center.Controls.Add(bottom);
        root.Controls.Add(center); root.Controls.Add(right); root.Controls.Add(left); Controls.Add(root);
        Shown += async (_, _) => await LoadBackgroundAsync();
    }

    private async Task InitializeLauncherAsync()
    {
        try
        {
            splash?.SetStatus("설정을 불러오는 중...", 18);
            LoadSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            splash?.SetStatus("Minecraft 런처를 초기화하는 중...", 42);
            launcher = new MinecraftLauncher(new MinecraftPath(gameDirectory));
            loginHandler = JELoginHandlerBuilder.BuildDefault();
            splash?.SetStatus("Minecraft 버전 목록을 확인하는 중...", 65);
            await RefreshVersionsAsync();
            splash?.SetStatus("서버와 UI를 준비하는 중...", 88);
            UpdateServerSummary();
            status.Text = "준비 완료";
            _ = CheckForUpdateAsync(false);
            await Task.Delay(180);
        }
        catch (Exception ex)
        {
            status.Text = "초기화 실패";
            MessageBox.Show(ex.Message, "Launcher Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            splash?.SetStatus("완료", 100);
            if (splash is not null && !splash.IsDisposed)
            {
                splash.Close();
                splash.Dispose();
            }
            splash = null;
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

            var previousVersion = versionBox.Text;
            var previousProfile = profileBox.Text;

            versionBox.Items.Clear();
            profileBox.Items.Clear();
            profileBox.Items.Add("Vanilla");

            foreach (var v in versions)
            {
                versionBox.Items.Add(v.Name);
                if (IsCustomProfile(v.Name))
                    profileBox.Items.Add(v.Name);
            }

            if (!string.IsNullOrWhiteSpace(previousVersion) &&
                versionBox.Items.Contains(previousVersion))
                versionBox.SelectedItem = previousVersion;
            else if (!string.IsNullOrWhiteSpace(launcher.Versions.LatestReleaseName) &&
                     versionBox.Items.Contains(launcher.Versions.LatestReleaseName))
                versionBox.SelectedItem = launcher.Versions.LatestReleaseName;
            else if (versionBox.Items.Count > 0)
                versionBox.SelectedIndex = 0;

            if (!string.IsNullOrWhiteSpace(previousProfile) &&
                profileBox.Items.Contains(previousProfile))
                profileBox.SelectedItem = previousProfile;
            else
                profileBox.SelectedIndex = 0;

            status.Text = $"버전 {versionBox.Items.Count}개 준비 완료";
        }
        catch (Exception ex)
        {
            status.Text = "버전 목록 실패";
            MessageBox.Show(
                ex.Message,
                "Version Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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
            status.Text = "Microsoft 계정 로그인 중...";
            microsoftSession = await loginHandler.Authenticate();
            usernameBox.Text = microsoftSession.Username;
            accountStatus.Text = $"Microsoft 계정 연결됨\n{microsoftSession.Username}";
            accountStatus.ForeColor = Color.LightGreen;
            _ = LoadAvatarAsync(microsoftSession.Username);
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
            status.Text = "Microsoft 로그아웃 중...";

            await loginHandler.SignoutWithBrowser();
            microsoftSession = null;

            accountStatus.Text = "계정: 오프라인";
            accountStatus.ForeColor = Color.Silver;
            logoutButton.Enabled = false;
            status.Text = "로그아웃 완료";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Logout Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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
        updateButton.Enabled = enabled;
    }

    private async Task LaunchAsync()
    {
        if (launcher is null) { MessageBox.Show("런처가 아직 초기화되지 않았습니다."); return; }
        if (string.IsNullOrWhiteSpace(versionBox.Text)) { MessageBox.Show("Minecraft 버전을 선택하세요."); return; }
        if (string.IsNullOrWhiteSpace(usernameBox.Text)) { MessageBox.Show("플레이어 이름을 입력하세요."); usernameBox.Focus(); return; }
        if (usernameBox.Text.Trim().Length > 16) { MessageBox.Show("플레이어 이름은 16자 이하로 입력하세요."); usernameBox.Focus(); return; }
        if (!string.IsNullOrWhiteSpace(javaBox.Text) && !File.Exists(javaBox.Text.Trim())) { MessageBox.Show("지정한 Java 실행 파일을 찾을 수 없습니다."); javaBox.Focus(); return; }
        if (serverBox.SelectedItem is ServerEntry selectedServer && !IsValidServerAddress(selectedServer.Address)) { MessageBox.Show("서버 주소가 올바르지 않습니다."); return; }

        launchCancellation?.Dispose();
        launchCancellation = new CancellationTokenSource();
        using var overlay = new LaunchOverlay(this, background?.Image);
        try
        {
            SetLaunchUi(false);
            progress.Style = ProgressBarStyle.Blocks;
            progress.Minimum = 0; progress.Maximum = 100; progress.Value = 0;
            status.Text = "Minecraft를 준비하는 중...";
            overlay.Show(this);
            SaveSettings();

            var selectedVersion = profileBox.Text == "Vanilla" ? versionBox.Text : profileBox.Text;
            var session = microsoftSession ?? MSession.CreateOfflineSession(usernameBox.Text.Trim());
            var option = new MLaunchOption { Session = session, MaximumRamMb = (int)ramBox.Value };
            if (!string.IsNullOrWhiteSpace(javaBox.Text)) option.JavaPath = javaBox.Text.Trim();
            if (serverBox.SelectedItem is ServerEntry server) { option.ServerIp = server.Address; option.ServerPort = server.Port; }

            var fileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(e =>
            {
                if (e.TotalTasks <= 0) return;
                var percent = (int)Math.Clamp(e.ProgressedTasks * 100.0 / e.TotalTasks, 0, 100);
                BeginInvoke(() => { progress.Value = percent; status.Text = $"설치: {e.ProgressedTasks}/{e.TotalTasks}  {e.Name}"; overlay.SetProgress(percent, e.Name); });
            });
            var byteProgress = new SyncProgress<ByteProgress>(e =>
            {
                if (e.TotalBytes <= 0) return;
                var percent = (int)Math.Clamp(e.ProgressedBytes * 100.0 / e.TotalBytes, 0, 100);
                BeginInvoke(() => { progress.Value = percent; status.Text = $"다운로드: {FormatBytes(e.ProgressedBytes)} / {FormatBytes(e.TotalBytes)}"; overlay.SetProgress(percent, "Minecraft 파일 다운로드"); });
            });
            var process = await launcher.InstallAndBuildProcessAsync(selectedVersion, option, fileProgress, byteProgress, launchCancellation.Token);
            overlay.SetProgress(100, "Minecraft 실행 중...");
            status.Text = "Minecraft 실행 중...";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) LauncherLogger.Write("OUT " + e.Data); };
            process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) LauncherLogger.Write("ERR " + e.Data); };
            process.Exited += (_, _) => { LauncherLogger.Write("Minecraft process exited."); if (!IsDisposed) BeginInvoke(() => status.Text = "Minecraft 종료됨"); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Delay(650);
        }
        catch (OperationCanceledException) { status.Text = "실행 취소됨"; progress.Value = 0; }
        catch (Win32Exception ex) { status.Text = "Java 실행 실패"; MessageBox.Show(ex.Message + Environment.NewLine + Environment.NewLine + "Java 경로를 확인하거나 비워 자동 검색을 사용하세요.", "Java Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        catch (Exception ex) { status.Text = "실행 실패"; MessageBox.Show(ex.ToString(), "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally
        {
            if (!overlay.IsDisposed) overlay.Close();
            SetLaunchUi(true);
            launchCancellation?.Dispose();
            launchCancellation = null;
        }
    }

    private void SetLaunchUi(bool enabled)
    {
        launchButton.Enabled = enabled;
        cancelButton.Enabled = !enabled;
        refreshButton.Enabled = enabled;
        loginButton.Enabled = enabled;
        logoutButton.Enabled = enabled && microsoftSession is not null;
        updateButton.Enabled = enabled;
        addServerButton.Enabled = enabled;
        removeServerButton.Enabled = enabled;
        versionBox.Enabled = enabled;
        profileBox.Enabled = enabled;
        serverBox.Enabled = enabled;
    }

    private static bool IsValidServerAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return false;
        if (address.Contains(' ') || address.Contains('\\')) return false;
        return address.Length <= 253;
    }

    private void AddServer()
    {
        using var dialog = new ServerEditorForm();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var name = dialog.ServerName.Trim();
        var address = dialog.ServerAddress.Trim();
        var port = dialog.ServerPort;
        if (!IsValidServerAddress(address)) { MessageBox.Show("서버 주소가 올바르지 않습니다."); return; }
        if (servers.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) { MessageBox.Show("같은 이름의 서버가 이미 있습니다."); return; }
        servers.Add(new ServerEntry { Name = name, Address = address, Port = port });
        RefreshServerBox();
        serverBox.SelectedIndex = servers.Count;
        SaveSettings();
        UpdateServerSummary();
    }

    private void RemoveServer()
    {
        if (serverBox.SelectedItem is not ServerEntry server)
            return;

        var result = MessageBox.Show(
            $"'{server.Name}' 서버를 삭제할까요?",
            "서버 삭제",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        servers.Remove(server);
        RefreshServerBox();
        SaveSettings();
    }

    private void RefreshServerBox()
    {
        serverBox.Items.Clear();
        serverBox.Items.Add("서버 접속 안 함");

        foreach (var s in servers)
            serverBox.Items.Add(s);

        serverBox.SelectedIndex = 0;
    }

    private void LoadSelectedServer()
    {
        if (serverBox.SelectedItem is not ServerEntry s)
            return;

        serverNameBox.Text = s.Name;
        serverAddressBox.Text = s.Address;
        serverPortBox.Value = Math.Clamp(s.Port, 1, 65535);
    }

    private async Task LoadServerIconAsync(PictureBox box, string address)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CalbalamLauncher/1.1");
            var bytes = await http.GetByteArrayAsync($"https://api.mcstatus.io/v2/icon/{Uri.EscapeDataString(address)}");
            using var ms = new MemoryStream(bytes);
            using var img = Image.FromStream(ms);
            box.Image = new Bitmap(img);
        }
        catch { }
    }

    private async Task LoadAvatarAsync(string username)
    {
        if (accountAvatar is null || string.IsNullOrWhiteSpace(username)) return;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CalbalamLauncher/1.1");
            var bytes = await http.GetByteArrayAsync($"https://mc-heads.net/avatar/{Uri.EscapeDataString(username)}/64");
            using var ms = new MemoryStream(bytes);
            using var img = Image.FromStream(ms);
            var copy = new Bitmap(img);
            if (accountAvatar.IsDisposed) { copy.Dispose(); return; }
            if (accountAvatar.InvokeRequired) accountAvatar.BeginInvoke(() => accountAvatar.Image = copy);
            else accountAvatar.Image = copy;
        }
        catch { }
    }

    private static Image CreateAvatarPlaceholder()
    {
        var bmp = new Bitmap(54, 54);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(35, 80, 120));
        using var brush = new SolidBrush(Color.White);
        g.FillEllipse(brush, 17, 7, 20, 20);
        g.FillEllipse(brush, 9, 29, 36, 20);
        return bmp;
    }

    private async Task LoadBackgroundAsync()
    {
        if (background is null) return;
        try
        {
            var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalbalamLauncher", "cache");
            Directory.CreateDirectory(cacheDir);
            var cache = Path.Combine(cacheDir, "background.png");
            if (File.Exists(cache) && new FileInfo(cache).Length > 100_000)
            {
                var cachedBytes = await File.ReadAllBytesAsync(cache);
                using var cachedStream = new MemoryStream(cachedBytes);
                using var cachedImage = Image.FromStream(cachedStream);
                background.Image = new Bitmap(cachedImage);
                return;
            }
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CalbalamLauncher/1.1");
            var bytes = await http.GetByteArrayAsync("https://raw.githubusercontent.com/peunsu/MRSLauncher/master/app/assets/images/backgrounds/0.png");
            await File.WriteAllBytesAsync(cache, bytes);
            using var ms = new MemoryStream(bytes);
            using var image = Image.FromStream(ms);
            background.Image = new Bitmap(image);
        }
        catch { }
    }

    private void OpenProfile()
    {
        if (microsoftSession is null)
        {
            MessageBox.Show("프로필/스킨 관리를 사용하려면 먼저 Microsoft 계정으로 로그인하세요.");
            return;
        }
        using var form = new ProfileForm(microsoftSession);
        form.ShowDialog(this);
    }

    private void UpdateServerSummary()
    {
        selectedVersionLabel.Text = string.IsNullOrWhiteSpace(versionBox.Text) ? "Minecraft" : versionBox.Text;
        selectedServerLabel.Text = serverBox.SelectedItem is ServerEntry server
            ? $"●  {server.Name}  •  {server.Address}:{server.Port}"
            : "●  서버를 선택하지 않음";
    }

    private static void Style(Control c)
    {
        c.BackColor = Color.FromArgb(38, 43, 52);
        c.ForeColor = Color.White;
        c.Font = new Font("Segoe UI", 9);
    }

    private static void StyleButton(Button b, bool accent = false)
    {
        b.BackColor = accent ? Color.FromArgb(31, 105, 185) : Color.FromArgb(29, 35, 44);
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(55, 65, 78);
        b.FlatAppearance.BorderSize = 1;
        b.Cursor = Cursors.Hand;
        b.Font = new Font("Segoe UI", 8, FontStyle.Bold);
    }

    private void SetAutoRam()
    {
        try
        {
            var totalMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024;
            var recommended = Math.Clamp((int)(totalMb / 2), 2048, 8192);

            recommended -= recommended % 512;
            if (recommended < 2048) recommended = 2048;

            ramBox.Value = Math.Clamp(
                recommended,
                (int)ramBox.Minimum,
                (int)ramBox.Maximum);

            status.Text = $"권장 RAM: {ramBox.Value:N0} MB";
            SaveSettingsSafely();
        }
        catch
        {
            MessageBox.Show("컴퓨터 메모리를 확인하지 못했습니다.");
        }
    }

    private void BrowseJava()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Java 실행 파일 선택",
            Filter = "Java 실행 파일 (java.exe)|java.exe|모든 파일 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            javaBox.Text = dialog.FileName;
            SaveSettingsSafely();
        }
    }

    private async Task CheckForUpdateAsync(bool interactive)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(8)
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                $"CalbalamLauncher/{CurrentVersion}");

            var json = await client.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("tag_name", out var tagElement))
                throw new InvalidOperationException("GitHub release tag not found.");

            var tag = tagElement.GetString()?.Trim().TrimStart('v');

            if (Version.TryParse(tag, out var remote) &&
                Version.TryParse(CurrentVersion, out var local) &&
                remote > local)
            {
                var result = MessageBox.Show(
                    $"새 버전 {tag}이 있습니다.\n현재 버전: {CurrentVersion}\n\n다운로드 페이지를 열까요?",
                    "업데이트",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo(ReleasesUrl)
                    {
                        UseShellExecute = true
                    });
            }
            else if (interactive)
            {
                MessageBox.Show(
                    $"현재 버전 {CurrentVersion}이 최신입니다.",
                    "업데이트 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            if (interactive)
            {
                MessageBox.Show(
                    $"업데이트 서버에 연결하지 못했습니다.\n\n{ex.Message}",
                    "업데이트 확인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
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

            var settings = JsonSerializer.Deserialize<LauncherSettings>(
                File.ReadAllText(settingsPath));

            if (settings is null)
            {
                RefreshServerBox();
                return;
            }

            usernameBox.Text = settings.Username ?? "";
            ramBox.Value = Math.Clamp(settings.RamMb, 1024, 32768);
            javaBox.Text = settings.JavaPath ?? "";
            if (!string.IsNullOrWhiteSpace(settings.GameDirectory) && Directory.Exists(settings.GameDirectory))
                gameDirectory = settings.GameDirectory;

            servers.Clear();
            servers.AddRange(settings.Servers ?? []);

            RefreshServerBox();

            if (!string.IsNullOrWhiteSpace(settings.SelectedVersion))
                versionBox.SelectedItem = settings.SelectedVersion;

            if (!string.IsNullOrWhiteSpace(settings.SelectedProfile))
                profileBox.SelectedItem = settings.SelectedProfile;
        }
        catch
        {
            servers.Clear();
            RefreshServerBox();
        }
    }

    private void SaveSettingsSafely()
    {
        try
        {
            SaveSettings();
        }
        catch
        {
            // Settings must never prevent the launcher from running.
        }
    }

    private void SaveSettings()
    {
        var directory = Path.GetDirectoryName(settingsPath)!;
        Directory.CreateDirectory(directory);

        var settings = new LauncherSettings
        {
            Username = usernameBox.Text.Trim(),
            RamMb = (int)ramBox.Value,
            JavaPath = javaBox.Text.Trim(),
            GameDirectory = gameDirectory,
            SelectedVersion = versionBox.Text,
            SelectedProfile = profileBox.Text,
            Servers = servers
                .Select(s => new ServerEntry
                {
                    Name = s.Name,
                    Address = s.Address,
                    Port = s.Port
                })
                .ToList()
        };

        var tempPath = settingsPath + ".tmp";
        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(
            tempPath,
            json,
            new UTF8Encoding(false));

        File.Move(tempPath, settingsPath, true);
    }

    private async Task OpenAdvancedToolsAsync()
    {
        if (launcher is null)
        {
            MessageBox.Show("런처가 아직 초기화되지 않았습니다.");
            return;
        }

        using var form = new AdvancedToolsForm(
            launcher,
            gameDirectory,
            async path =>
            {
                await ApplyGameDirectoryAsync(path);
            },
            () => microsoftSession);

        form.ShowDialog(this);
        await RefreshVersionsAsync();
    }

    private async Task ApplyGameDirectoryAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        Directory.CreateDirectory(path);
        gameDirectory = path;
        launcher = new MinecraftLauncher(new MinecraftPath(gameDirectory));
        SaveSettingsSafely();
        status.Text = $"게임 폴더: {gameDirectory}";
        await RefreshVersionsAsync();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F1} MB";
        return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB";
    }

    private sealed class LauncherSettings
    {
        public string? Username { get; set; }
        public int RamMb { get; set; } = 4096;
        public string? JavaPath { get; set; }
        public string? SelectedVersion { get; set; }
        public string? SelectedProfile { get; set; }
        public string? GameDirectory { get; set; }
        public List<ServerEntry> Servers { get; set; } = [];
    }
}
