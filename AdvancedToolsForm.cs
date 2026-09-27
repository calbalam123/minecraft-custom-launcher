using CmlLib.Core;
using CmlLib.Core.Installers;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Auth;
using System.Diagnostics;
using System.Text.Json;

namespace CalbalamLauncher;

public sealed class AdvancedToolsForm : Form
{
    private readonly MinecraftLauncher launcher;
    private string gameDirectory;
    private readonly Func<string, Task> changeDirectory;
    private readonly Func<MSession?> getSession;
    private readonly ComboBox loaderBox = new();
    private readonly TextBox versionBox = new();
    private readonly ListBox modList = new();
    private readonly Label directoryLabel = new();
    private readonly Label statusLabel = new();
    private readonly ProgressBar progress = new();
    private CancellationTokenSource? cancellation;

    public AdvancedToolsForm(MinecraftLauncher launcher, string gameDirectory, Func<string, Task> changeDirectory, Func<MSession?> getSession)
    {
        this.launcher = launcher;
        this.gameDirectory = gameDirectory;
        this.changeDirectory = changeDirectory;
        this.getSession = getSession;
        Text = "CALBALAM 고급 관리";
        Width = 760;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(18, 18, 22);
        ForeColor = Color.White;
        BuildUi();
        RefreshMods();
    }

    private void BuildUi()
    {
        var title = new Label { Text = "고급 관리", Font = new Font("Segoe UI", 20, FontStyle.Bold), AutoSize = true, Location = new Point(30, 22) };
        directoryLabel.Location = new Point(30, 68);
        directoryLabel.Size = new Size(675, 38);
        directoryLabel.ForeColor = Color.Silver;
        directoryLabel.Text = $"게임 폴더: {gameDirectory}";

        var choose = Button("게임 폴더 변경", 30, 115, 135, 38);
        choose.Click += async (_, _) => await ChooseDirectoryAsync();
        var open = Button("폴더 열기", 175, 115, 110, 38);
        open.Click += (_, _) => OpenDirectory(gameDirectory);

        AddLabel("모드 로더 자동 설치", 30, 175);
        loaderBox.Location = new Point(30, 200);
        loaderBox.Size = new Size(150, 32);
        loaderBox.DropDownStyle = ComboBoxStyle.DropDownList;
        loaderBox.Items.AddRange(["Fabric", "Forge", "NeoForge", "Quilt"]);
        loaderBox.SelectedIndex = 0;
        Style(loaderBox);

        versionBox.Location = new Point(195, 200);
        versionBox.Size = new Size(150, 32);
        versionBox.PlaceholderText = "예: 1.21.1";
        Style(versionBox);

        var install = Button("자동 설치", 355, 200, 110, 32);
        install.Click += async (_, _) => await InstallLoaderAsync();

        var profile = Button("스킨 / 프로필", 595, 200, 110, 32);
        profile.Click += (_, _) => OpenProfile();

        var log = Button("런처 로그", 475, 200, 110, 32);
        log.Click += (_, _) => new LogViewerForm().Show(this);

        AddLabel("mods 폴더 관리", 30, 255);
        modList.Location = new Point(30, 280);
        modList.Size = new Size(555, 190);
        modList.BackColor = Color.FromArgb(30, 30, 36);
        modList.ForeColor = Color.White;

        var add = Button("모드 추가", 600, 280, 105, 38);
        add.Click += (_, _) => AddMods();
        var remove = Button("선택 삭제", 600, 330, 105, 38);
        remove.Click += (_, _) => RemoveSelectedMod();
        var refresh = Button("새로고침", 600, 380, 105, 38);
        refresh.Click += (_, _) => RefreshMods();

        progress.Location = new Point(30, 500);
        progress.Size = new Size(675, 12);
        statusLabel.Location = new Point(30, 525);
        statusLabel.AutoSize = true;
        statusLabel.ForeColor = Color.Silver;
        statusLabel.Text = "준비 완료";

        Controls.AddRange([title, directoryLabel, choose, open, loaderBox, versionBox, install, profile, log,
            modList, add, remove, refresh, progress, statusLabel]);
    }

    private Button Button(string text, int x, int y, int w, int h)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h) };
        b.BackColor = Color.FromArgb(45, 45, 55);
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private void AddLabel(string text, int x, int y) => Controls.Add(new Label
    {
        Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = Color.Silver
    });

    private static void Style(Control c)
    {
        c.BackColor = Color.FromArgb(35, 35, 42);
        c.ForeColor = Color.White;
        c.Font = new Font("Segoe UI", 10);
    }

    private void OpenProfile()
    {
        var session = getSession();
        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            MessageBox.Show("먼저 Microsoft 계정으로 로그인하세요.", "프로필", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        new ProfileForm(session).Show(this);
    }

    private async Task ChooseDirectoryAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Minecraft 게임 폴더를 선택하세요.",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(gameDirectory) ? gameDirectory : ""
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        gameDirectory = dialog.SelectedPath;
        await changeDirectory(gameDirectory);
        directoryLabel.Text = $"게임 폴더: {gameDirectory}";
        RefreshMods();
    }

    private async Task InstallLoaderAsync()
    {
        var mcVersion = versionBox.Text.Trim();
        var loader = loaderBox.Text;
        if (string.IsNullOrWhiteSpace(mcVersion))
        {
            MessageBox.Show("Minecraft 버전을 입력하세요. 예: 1.21.1");
            return;
        }

        cancellation?.Dispose();
        cancellation = new CancellationTokenSource();
        try
        {
            SetBusy(false);
            statusLabel.Text = $"{loader} 설치 준비 중...";
            string installedVersion;

            if (loader == "Forge")
            {
                var installer = new ForgeInstaller(launcher);
                installedVersion = await installer.Install(mcVersion, new ForgeInstallOptions
                {
                    CancellationToken = cancellation.Token,
                    SkipIfAlreadyInstalled = true,
                    InstallerOutput = new Progress<string>(SetStatus)
                });
            }
            else if (loader == "NeoForge")
            {
                var installer = new NeoForgeInstaller(launcher);
                installedVersion = await installer.Install(mcVersion);
            }
            else
            {
                installedVersion = await InstallProfileLoaderAsync(loader, mcVersion, cancellation.Token);
            }

            await launcher.InstallAsync(installedVersion, cancellation.Token);
            progress.Value = 100;
            statusLabel.Text = $"{loader} 설치 완료: {installedVersion}";
            LauncherLogger.Write($"{loader} installed: {installedVersion}");
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "설치 취소됨";
        }
        catch (Exception ex)
        {
            statusLabel.Text = "설치 실패";
            LauncherLogger.Write($"Loader install error: {ex}");
            MessageBox.Show(ex.Message, "로더 설치 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(true);
            cancellation?.Dispose();
            cancellation = null;
        }
    }

    private async Task<string> InstallProfileLoaderAsync(string loader, string mcVersion, CancellationToken token)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CalbalamLauncher/1.1");

        string profileUrl;
        if (loader == "Fabric")
        {
            var json = await http.GetStringAsync($"https://meta.fabricmc.net/v2/versions/loader/{Uri.EscapeDataString(mcVersion)}", token);
            using var doc = JsonDocument.Parse(json);
            var first = doc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException($"Fabric이 {mcVersion}을 지원하지 않습니다.");
            var v = first.GetProperty("loader").GetProperty("version").GetString()!;
            profileUrl = $"https://meta.fabricmc.net/v2/versions/loader/{Uri.EscapeDataString(mcVersion)}/{Uri.EscapeDataString(v)}/profile/json";
        }
        else
        {
            var json = await http.GetStringAsync($"https://meta.quiltmc.org/v3/versions/loader/{Uri.EscapeDataString(mcVersion)}", token);
            using var doc = JsonDocument.Parse(json);
            var first = doc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException($"Quilt가 {mcVersion}을 지원하지 않습니다.");
            var v = first.GetProperty("loader").GetProperty("version").GetString()!;
            profileUrl = $"https://meta.quiltmc.org/v3/versions/loader/{Uri.EscapeDataString(mcVersion)}/{Uri.EscapeDataString(v)}/profile/json";
        }

        SetStatus($"{loader} 프로필 다운로드...");
        var profile = await http.GetStringAsync(profileUrl, token);
        using var profileDoc = JsonDocument.Parse(profile);
        var id = profileDoc.RootElement.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("로더 프로필 ID를 찾지 못했습니다.");

        var dir = Path.Combine(gameDirectory, "versions", id);
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, id + ".json"), profile, new System.Text.UTF8Encoding(false), token);
        await launcher.InstallAsync(id, token);
        return id;
    }

    private void RefreshMods()
    {
        var path = Path.Combine(gameDirectory, "mods");
        Directory.CreateDirectory(path);
        modList.Items.Clear();
        foreach (var file in Directory.EnumerateFiles(path, "*.jar").OrderBy(Path.GetFileName))
            modList.Items.Add(file);
        statusLabel.Text = $"mods: {modList.Items.Count}개";
    }

    private void AddMods()
    {
        using var dialog = new OpenFileDialog { Title = "Minecraft 모드 선택", Filter = "Minecraft Mod (*.jar)|*.jar", Multiselect = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var path = Path.Combine(gameDirectory, "mods");
        Directory.CreateDirectory(path);
        foreach (var file in dialog.FileNames)
        {
            var destination = Path.Combine(path, Path.GetFileName(file));
            File.Copy(file, destination, true);
            LauncherLogger.Write($"Mod added: {destination}");
        }
        RefreshMods();
    }

    private void RemoveSelectedMod()
    {
        foreach (var item in modList.SelectedItems.Cast<object>().ToList())
        {
            var file = item.ToString();
            if (string.IsNullOrWhiteSpace(file)) continue;
            File.Delete(file);
            LauncherLogger.Write($"Mod removed: {file}");
        }
        RefreshMods();
    }

    private static void OpenDirectory(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{path}\"", UseShellExecute = true });
    }

    private void SetStatus(string text)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => SetStatus(text)); return; }
        statusLabel.Text = text.Length > 100 ? text[..100] : text;
        LauncherLogger.Write(text);
    }

    private void SetBusy(bool enabled)
    {
        loaderBox.Enabled = enabled;
        versionBox.Enabled = enabled;
        foreach (Control c in Controls)
            if (c is Button b) b.Enabled = enabled;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        cancellation?.Cancel();
        base.OnFormClosing(e);
    }
}

public static class LauncherLogger
{
    private static readonly object Sync = new();
    public static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CalbalamLauncher", "launcher.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            lock (Sync)
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    public static string ReadAll()
    {
        try { return File.Exists(LogPath) ? File.ReadAllText(LogPath) : "아직 로그가 없습니다."; }
        catch (Exception ex) { return $"로그를 읽을 수 없습니다: {ex.Message}"; }
    }
}

public sealed class LogViewerForm : Form
{
    private readonly TextBox logBox = new();

    public LogViewerForm()
    {
        Text = "CALBALAM 런처 로그";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(12, 12, 15);
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Both;
        logBox.Dock = DockStyle.Fill;
        logBox.BackColor = Color.FromArgb(18, 18, 22);
        logBox.ForeColor = Color.White;
        logBox.Font = new Font("Consolas", 9);
        logBox.Text = LauncherLogger.ReadAll();
        Controls.Add(logBox);
        Shown += (_, _) => logBox.SelectionStart = logBox.TextLength;
    }
}
