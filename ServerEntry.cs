namespace CalbalamLauncher;

public sealed class ServerEntry
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public int Port { get; set; } = 25565;

    public override string ToString() => $"{Name}  •  {Address}:{Port}";
}