using CmlLib.Core.Auth;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CalbalamLauncher;

public sealed class ProfileForm : Form
{
    private readonly MSession session;
    private readonly Label info = new();
    private readonly TextBox skinUrl = new();
    private readonly ComboBox variant = new();
    private readonly Button upload = new();
    private readonly Button reset = new();

    public ProfileForm(MSession session)
    {
        this.session = session;
        Text = "CALBALAM 스킨 / 프로필";
        Width = 650;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(18, 18, 22);
        ForeColor = Color.White;

        info.Location = new Point(30, 25);
        info.Size = new Size(570, 110);
        info.ForeColor = Color.Silver;
        info.Text = "프로필을 불러오는 중...";

        skinUrl.Location = new Point(30, 160);
        skinUrl.Size = new Size(450, 32);
        skinUrl.PlaceholderText = "스킨 PNG URL";
        Style(skinUrl);

        variant.Location = new Point(490, 160);
        variant.Size = new Size(100, 32);
        variant.DropDownStyle = ComboBoxStyle.DropDownList;
        variant.Items.AddRange(["classic", "slim"]);
        variant.SelectedIndex = 0;
        Style(variant);

        upload.Text = "URL 스킨 적용";
        upload.Location = new Point(30, 210);
        upload.Size = new Size(180, 40);
        StyleButton(upload);
        upload.Click += async (_, _) => await ApplyUrlSkinAsync();

        var file = new Button { Text = "PNG 업로드", Location = new Point(225, 210), Size = new Size(150, 40) };
        StyleButton(file);
        file.Click += async (_, _) => await UploadFileAsync();

        reset.Text = "스킨 초기화";
        reset.Location = new Point(390, 210);
        reset.Size = new Size(150, 40);
        StyleButton(reset);
        reset.Click += async (_, _) => await ResetSkinAsync();

        Controls.AddRange([info, skinUrl, variant, upload, file, reset]);
        Shown += async (_, _) => await LoadProfileAsync();
    }

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

    private async Task LoadProfileAsync()
    {
        try
        {
            using var http = CreateClient();
            var json = await http.GetStringAsync("https://api.minecraftservices.com/minecraft/profile");
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;
            var name = root.TryGetProperty("name", out var n) ? n.GetString() : session.Username;
            var id = root.TryGetProperty("id", out var i) ? i.GetString() : session.UUID;
            var skins = root.TryGetProperty("skins", out var s) ? s.GetArrayLength() : 0;

            info.Text = $"이름: {name}\r\nUUID: {id}\r\n등록된 스킨: {skins}개";
            LauncherLogger.Write($"Minecraft profile loaded: {name}");
        }
        catch (Exception ex)
        {
            info.Text = "프로필을 불러오지 못했습니다.";
            LauncherLogger.Write($"Profile load error: {ex}");
        }
    }

    private async Task ApplyUrlSkinAsync()
    {
        var url = skinUrl.Text.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            MessageBox.Show("올바른 HTTP/HTTPS 스킨 URL을 입력하세요.");
            return;
        }

        try
        {
            using var http = CreateClient();
            using var body = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("variant", variant.Text),
                new KeyValuePair<string, string>("url", url)
            ]);
            var response = await http.PostAsync("https://api.minecraftservices.com/minecraft/profile/skins", body);
            response.EnsureSuccessStatusCode();
            MessageBox.Show("스킨 적용 완료.");
            await LoadProfileAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "스킨 적용 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task UploadFileAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PNG 이미지 (*.png)|*.png",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var http = CreateClient();
            await using var stream = File.OpenRead(dialog.FileName);
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(variant.Text), "variant");

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "file", Path.GetFileName(dialog.FileName));

            var response = await http.PostAsync("https://api.minecraftservices.com/minecraft/profile/skins", content);
            response.EnsureSuccessStatusCode();
            MessageBox.Show("스킨 업로드 완료.");
            await LoadProfileAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "스킨 업로드 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ResetSkinAsync()
    {
        if (MessageBox.Show("현재 스킨을 초기화할까요?", "스킨 초기화",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            using var http = CreateClient();
            var response = await http.DeleteAsync("https://api.minecraftservices.com/minecraft/profile/skins/active");
            response.EnsureSuccessStatusCode();
            MessageBox.Show("스킨 초기화 완료.");
            await LoadProfileAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "스킨 초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", session.AccessToken);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CalbalamLauncher/1.1");
        return client;
    }
}
