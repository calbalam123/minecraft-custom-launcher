using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Installers;
using CmlLib.Core.ProcessBuilder;
using System.ComponentModel;
using System.Diagnostics;
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

    public MainForm()
    {
        Text = "CALBALAM Minecraft Launcher";
        Width = 820;
        Height = 720;
        MinimumSize = new Size(780, 660);
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

    private void BuildUi()
    {
        var title = new Label
        {
            Text = "CALBALAM",
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(36, 22)
        };
        var subtitle = new Label
        {
            Text = $"Minecraft Custom Launcher v{CurrentVersion}",
            ForeColor = Color.Silver,
            AutoSize = true,
            Location = new Point(39, 66)
        };

        accountStatus.Text = "계정: 오프라인";
        accountStatus.ForeColor = Color.Silver;
        accountStatus.AutoSize = true;
        accountStatus.Location = new Point(430, 30);

        loginButton.Text = "Microsoft 로그인";
        loginButton.Location = new Point(430, 55);
        loginButton.Width = 120;
        StyleButton(loginButton);
        loginButton.Click += async (_, _) => await LoginAsync();

        logoutButton.Text = "로그아웃";
        logoutButton.Location = new Point(560, 55);
        logoutButton.Width = 85;
        StyleButton(logoutButton);
        logoutButton.Enabled = false;
        logoutButton.Click += async (_, _) => await LogoutAsync();

        updateButton.Text = "업데이트 확인";
        updateButton.Location = new Point(655, 55);
        updateButton.Width = 115;
        StyleButton(updateButton);
        updateButton.Click += async (_, _) => await CheckForUpdateAsync(true);

        AddLabel("Minecraft 버전", 40, 105);
        versionBox.Location = new Point(40, 130);
        versionBox.Width = 450;
        versionBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Style(versionBox);
        versionBox.SelectedIndexChanged += (_, _) => SaveSettingsSafely();

        refreshButton.Text = "새로고침";
        refreshButton.Location = new Point(505, 129);
        refreshButton.Width = 100;
        StyleButton(refreshButton);
        refreshButton.Click += async (_, _) => await RefreshVersionsAsync();

        AddLabel("프로필 / 로더", 40, 175);
        profileBox.Location = new Point(40, 200);
        profileBox.Width = 565;
        profileBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Style(profileBox);

        AddLabel("서버", 40, 245);
        serverBox.Location = new Point(40, 270);
        serverBox.Width = 565;
        serverBox.DropDownStyle = ComboBoxStyle.DropDownList;
        Style(serverBox);
        serverBox.SelectedIndexChanged += (_, _) => LoadSelectedServer();

        addServerButton.Text = "추가";
        addServerButton.Location = new Point(615, 269);
        addServerButton.Width = 65;
        StyleButton(addServerButton);
        addServerButton.Click += (_, _) => AddServer();

        removeServerButton.Text = "삭제";
        removeServerButton.Location = new Point(690, 269);
        removeServerButton.Width = 65;
        StyleButton(removeServerButton);
        removeServerButton.Click += (_, _) => RemoveServer();

        AddLabel("서버 이름", 40, 315);
        serverNameBox.Location = new Point(40, 340);
        serverNameBox.Width = 230;
        Style(serverNameBox);

        AddLabel("주소", 285, 315);
        serverAddressBox.Location = new Point(285, 340);
        serverAddressBox.Width = 275;
        Style(serverAddressBox);

        AddLabel("포트", 575, 315);
        serverPortBox.Location = new Point(575, 340);
        serverPortBox.Width = 85;
        serverPortBox.Minimum = 1;
        serverPortBox.Maximum = 65535;
        serverPortBox.Value = 25565;
        Style(serverPortBox);

        AddLabel("플레이어 이름", 40, 390);
        usernameBox.Location = new Point(40, 415);
        usernameBox.Width = 620;
        Style(usernameBox);
        usernameBox.TextChanged += (_, _) => SaveSettingsSafely();

        AddLabel("RAM (MB)", 40, 460);
        ramBox.Location = new Point(40, 485);
        ramBox.Width = 180;
        ramBox.Minimum = 1024;
        ramBox.Maximum = 32768;
        ramBox.Increment = 512;
        ramBox.Value = 4096;
        Style(ramBox);

        autoRamButton.Text = "자동";
        autoRamButton.Location = new Point(225, 484);
        autoRamButton.Width = 65;
        StyleButton(autoRamButton);
        autoRamButton.Click += (_, _) => SetAutoRam();

        AddLabel("Java 경로 (선택)", 315, 460);
        javaBox.Location = new Point(315, 485);
        javaBox.Width = 315;
        Style(javaBox);

        javaBrowseButton.Text = "찾기";
        javaBrowseButton.Location = new Point(640, 484);
        javaBrowseButton.Width = 65;
        StyleButton(javaBrowseButton);
        javaBrowseButton.Click += (_, _) => BrowseJava();

        launchButton.Text = "게임 실행";
        launchButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        launchButton.Location = new Point(40, 535);
        launchButton.Size = new Size(530, 48);
        launchButton.BackColor = Color.FromArgb(80, 160, 90);
        launchButton.ForeColor = Color.White;
        launchButton.FlatStyle = FlatStyle.Flat;
        launchButton.Click += async (_, _) => await LaunchAsync();

        cancelButton.Text = "취소";
        cancelButton.Location = new Point(580, 535);
        cancelButton.Size = new Size(125, 48);
        StyleButton(cancelButton);
        cancelButton.Enabled = false;
        cancelButton.Click += (_, _) => launchCancellation?.Cancel();

        advancedButton.Text = "고급 관리";
        advancedButton.Location = new Point(710, 535);
        advancedButton.Size = new Size(90, 48);
        StyleButton(advancedButton);
        advancedButton.Click += async (_, _) => await OpenAdvancedToolsAsync();

        progress.Location = new Point(40, 595);
        progress.Size = new Size(665, 12);
        status.Text = "준비 중...";
        status.ForeColor = Color.Silver;
        status.AutoSize = true;
        status.Location = new Point(40, 615);

        Controls.AddRange([
            title, subtitle, accountStatus, loginButton, logoutButton, updateButton,
            versionBox, refreshButton, profileBox, serverBox, addServerButton, removeServerButton,
            serverNameBox, serverAddressBox, serverPortBox, usernameBox, ramBox, autoRamButton,
            javaBox, javaBrowseButton, launchButton, cancelButton, advancedButton, progress, status
        ]);
    }

    private void AddLabel(string text, int x, int y) => Controls.Add(new Label
    {
        Text = text,
        AutoSize = true,
        Location = new Point(x, y),
        ForeColor = Color.Silver
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
        b.FlatAppearance.BorderSize = 0;
    }

    private async Task InitializeLauncherAsync()
    {
        try
        {
            LoadSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

            launcher = new MinecraftLauncher(new MinecraftPath(gameDirectory));
            loginHandler = JELoginHandlerBuilder.BuildDefault();

            await RefreshVersionsAsync();
            status.Text = "준비 완료";
            _ = CheckForUpdateAsync(false);
        }
        catch (Exception ex)
        {
            status.Text = "초기화 실패";
            MessageBox.Show(
                ex.Message,
                "Launcher Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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

            accountStatus.Text = $"계정: {microsoftSession.Username}";
            accountStatus.ForeColor = Color.LightGreen;
            logoutButton.Enabled = true;

            SaveSettings();
            status.Text = "로그인 완료";
        }
        catch (Exception ex)
        {
            status.Text = "로그인 실패";
            MessageBox.Show(
                ex.ToString(),
                "Microsoft Login Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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
        if (launcher is null)
        {
            MessageBox.Show("런처가 아직 초기화되지 않았습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(versionBox.Text))
        {
            MessageBox.Show("Minecraft 버전을 선택하세요.");
            return;
        }

        if (string.IsNullOrWhiteSpace(usernameBox.Text))
        {
            MessageBox.Show("플레이어 이름을 입력하세요.");
            usernameBox.Focus();
            return;
        }

        if (usernameBox.Text.Trim().Length > 16)
        {
            MessageBox.Show("오프라인 플레이어 이름은 16자 이하로 입력하세요.");
            usernameBox.Focus();
            return;
        }

        if (!string.IsNullOrWhiteSpace(javaBox.Text))
        {
            var javaPath = javaBox.Text.Trim();
            if (!File.Exists(javaPath))
            {
                MessageBox.Show("지정한 Java 실행 파일을 찾을 수 없습니다.");
                javaBox.Focus();
                return;
            }
        }

        if (serverBox.SelectedItem is ServerEntry selectedServer)
        {
            if (!IsValidServerAddress(selectedServer.Address))
            {
                MessageBox.Show("서버 주소가 올바르지 않습니다.");
                return;
            }
        }

        launchCancellation?.Dispose();
        launchCancellation = new CancellationTokenSource();

        try
        {
            SetLaunchUi(false);
            progress.Style = ProgressBarStyle.Blocks;
            progress.Minimum = 0;
            progress.Maximum = 100;
            progress.Value = 0;
            status.Text = "Minecraft 파일을 확인/설치하는 중...";

            SaveSettings();

            var selectedVersion =
                profileBox.Text == "Vanilla"
                    ? versionBox.Text
                    : profileBox.Text;

            var session = microsoftSession ??
                          MSession.CreateOfflineSession(usernameBox.Text.Trim());

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

            var fileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(e =>
            {
                if (e.TotalTasks > 0)
                {
                    var percent = (int)Math.Clamp(
                        e.ProgressedTasks * 100.0 / e.TotalTasks, 0, 100);
                    BeginInvoke(() =>
                    {
                        progress.Value = percent;
                        status.Text = $"설치: {e.ProgressedTasks}/{e.TotalTasks}  {e.Name}";
                    });
                }
            });

            var byteProgress = new SyncProgress<ByteProgress>(e =>
            {
                if (e.TotalBytes <= 0) return;
                var percent = (int)Math.Clamp(
                    e.ProgressedBytes * 100.0 / e.TotalBytes, 0, 100);

                BeginInvoke(() =>
                {
                    progress.Value = percent;
                    status.Text = $"다운로드: {FormatBytes(e.ProgressedBytes)} / {FormatBytes(e.TotalBytes)}";
                });
            });

            var process = await launcher.InstallAndBuildProcessAsync(
                selectedVersion,
                option,
                fileProgress,
                byteProgress,
                launchCancellation.Token);

            status.Text = "Minecraft 실행 중...";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    LauncherLogger.Write("OUT " + e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    LauncherLogger.Write("ERR " + e.Data);
            };
            process.Exited += (_, _) =>
            {
                LauncherLogger.Write("Minecraft process exited.");
                if (!IsDisposed)
                    BeginInvoke(() => status.Text = "Minecraft 종료됨");
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (OperationCanceledException)
        {
            status.Text = "실행 취소됨";
            progress.Value = 0;
        }
        catch (Win32Exception ex)
        {
            status.Text = "Java 실행 실패";
            MessageBox.Show(
                ex.Message + Environment.NewLine + Environment.NewLine +
                "Java 경로를 확인하거나 Java 경로를 비워 자동 검색을 사용하세요.",
                "Java Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            status.Text = "실행 실패";
            MessageBox.Show(
                ex.ToString(),
                "Launch Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
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
        var name = serverNameBox.Text.Trim();
        var address = serverAddressBox.Text.Trim();
        var port = (int)serverPortBox.Value;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
        {
            MessageBox.Show("서버 이름과 주소를 입력하세요.");
            return;
        }

        if (!IsValidServerAddress(address))
        {
            MessageBox.Show("서버 주소가 올바르지 않습니다.");
            return;
        }

        if (servers.Any(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("같은 이름의 서버가 이미 있습니다.");
            return;
        }

        servers.Add(new ServerEntry
        {
            Name = name,
            Address = address,
            Port = port
        });

        RefreshServerBox();
        serverBox.SelectedIndex = servers.Count;
        SaveSettings();
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
