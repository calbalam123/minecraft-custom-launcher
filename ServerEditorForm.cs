namespace CalbalamLauncher;

public sealed class ServerEditorForm : Form
{
    private readonly TextBox nameBox = new();
    private readonly TextBox addressBox = new();
    private readonly NumericUpDown portBox = new();

    public string ServerName => nameBox.Text.Trim();
    public string ServerAddress => addressBox.Text.Trim();
    public int ServerPort => (int)portBox.Value;

    public ServerEditorForm()
    {
        Text = "서버 추가";
        ClientSize = new Size(430, 245);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(18, 22, 29);
        ForeColor = Color.White;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

        AddRow(layout, 0, "서버 이름", nameBox);
        AddRow(layout, 1, "서버 주소", addressBox);
        AddRow(layout, 2, "포트", portBox);

        portBox.Minimum = 1;
        portBox.Maximum = 65535;
        portBox.Value = 25565;
        Style(nameBox);
        Style(addressBox);
        Style(portBox);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent
        };
        var ok = new Button { Text = "추가", Width = 90, Height = 32, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "취소", Width = 90, Height = 32, DialogResult = DialogResult.Cancel };
        StyleButton(ok, true);
        StyleButton(cancel);
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(170, 195, 220)
        }, 0, row);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, row);
    }

    private static void Style(Control c)
    {
        c.BackColor = Color.FromArgb(39, 45, 54);
        c.ForeColor = Color.White;
        c.Font = new Font("Segoe UI", 9);
    }

    private static void StyleButton(Button b, bool accent = false)
    {
        b.BackColor = accent ? Color.FromArgb(31, 110, 190) : Color.FromArgb(34, 40, 49);
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
    }
}
