using Netch.Models;
using Netch.Servers;

namespace Netch.Forms;

[Fody.ConfigureAwait(true)]
internal class ModernProxyForm : ServerForm
{
    public ModernProxyForm(ModernProxyServer? server = default)
    {
        server ??= new ModernProxyServer();
        Server = server;

        var node = server.Node;
        if (string.IsNullOrWhiteSpace(node.Protocol))
            node.Protocol = "vless";

        CreateComboBox("Protocol",
            "Protocol",
            new List<string> { "anytls", "vless", "hysteria2", "tuic" },
            s =>
            {
                node.Protocol = s;
                server.Remark = string.IsNullOrWhiteSpace(server.Remark) ? $"{s}-{server.Hostname}:{server.Port}" : server.Remark;
            },
            node.Protocol);
        CreateTextBox("Uuid", "UUID", s => true, s => node.Uuid = EmptyToNull(s), node.Uuid ?? "");
        CreateTextBox("Password", "Password/Auth", s => true, s => node.Password = EmptyToNull(s), node.Password ?? "");
        CreateTextBox("Flow", "Flow", s => true, s => node.Flow = EmptyToNull(s), node.Flow ?? "");
        CreateTextBox("PacketEncoding", "Packet Encoding", s => true, s => node.PacketEncoding = EmptyToNull(s), node.PacketEncoding ?? "");
        CreateComboBox("Transport", "Transport", new List<string> { "", "ws", "grpc" }, s => node.Transport = EmptyToNull(s), node.Transport ?? "");
        CreateTextBox("Host", "Host", s => true, s => node.Host = EmptyToNull(s), node.Host ?? "");
        CreateTextBox("Path", "Path/Service", s => true, s => node.Path = EmptyToNull(s), node.Path ?? "");

        CreateCheckBox("TlsEnabled", "TLS Enabled", s => node.Tls.Enabled = s, node.Tls.Enabled);
        CreateTextBox("Sni", "SNI", s => true, s => node.Tls.ServerName = EmptyToNull(s), node.Tls.ServerName ?? "");
        CreateTextBox("Fingerprint", "Fingerprint", s => true, s => node.Tls.Fingerprint = EmptyToNull(s), node.Tls.Fingerprint ?? "");
        CreateTextBox("Alpn", "ALPN", s => true, s => node.Tls.Alpn = SplitList(s), string.Join(",", node.Tls.Alpn));
        CreateCheckBox("Insecure", "Allow Insecure", s => node.Tls.Insecure = s, node.Tls.Insecure);

        CreateCheckBox("RealityEnabled", "Reality Enabled", s => node.Reality.Enabled = s, node.Reality.Enabled);
        CreateTextBox("RealityPublicKey", "Reality Public Key", s => true, s => node.Reality.PublicKey = EmptyToNull(s), node.Reality.PublicKey ?? "");
        CreateTextBox("RealityShortId", "Reality Short ID", s => true, s => node.Reality.ShortId = EmptyToNull(s), node.Reality.ShortId ?? "");
        CreateTextBox("RealitySpiderX", "Reality SpiderX", s => true, s => node.Reality.SpiderX = EmptyToNull(s), node.Reality.SpiderX ?? "");

        CreateTextBox("Hy2Up", "Hy2 Up Mbps", IsEmptyOrInt, s => node.Hysteria2.UpMbps = IntOrNull(s), node.Hysteria2.UpMbps?.ToString() ?? "");
        CreateTextBox("Hy2Down", "Hy2 Down Mbps", IsEmptyOrInt, s => node.Hysteria2.DownMbps = IntOrNull(s), node.Hysteria2.DownMbps?.ToString() ?? "");
        CreateTextBox("Hy2Obfs", "Hy2 Obfs Password", s => true, s => node.Hysteria2.ObfsPassword = EmptyToNull(s), node.Hysteria2.ObfsPassword ?? "");

        CreateTextBox("TuicCongestion", "TUIC Congestion", s => true, s => node.Tuic.CongestionControl = EmptyToNull(s), node.Tuic.CongestionControl ?? "");
        CreateTextBox("TuicUdpRelay", "TUIC UDP Relay", s => true, s => node.Tuic.UdpRelayMode = EmptyToNull(s), node.Tuic.UdpRelayMode ?? "");

        CreateTextBox("Notes", "Node Summary", s => true, _ => SyncServer(server), BuildSummary(node), 294);
    }

    protected override string TypeName { get; } = "Modern Proxy";

    private static string? EmptyToNull(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static List<string> SplitList(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static bool IsEmptyOrInt(string value)
    {
        return string.IsNullOrWhiteSpace(value) || int.TryParse(value, out _);
    }

    private static int? IntOrNull(string value)
    {
        return int.TryParse(value, out var number) ? number : null;
    }

    private static string BuildSummary(ProxyNode node)
    {
        var parts = new List<string> { node.Protocol };
        if (node.Reality.Enabled)
            parts.Add("reality");
        if (!string.IsNullOrWhiteSpace(node.Flow))
            parts.Add(node.Flow);
        if (!string.IsNullOrWhiteSpace(node.Transport))
            parts.Add(node.Transport);
        return string.Join(" / ", parts);
    }

    private static void SyncServer(ModernProxyServer server)
    {
        server.Node.Name = server.Remark;
        server.Node.Server = server.Hostname;
        server.Node.ServerPort = server.Port;
    }
}
