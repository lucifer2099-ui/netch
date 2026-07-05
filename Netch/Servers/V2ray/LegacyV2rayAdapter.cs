using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers;

public class LegacyV2rayAdapter : ICoreAdapter
{
    private V2rayController? _controller;

    public string Name => "V2Ray (SagerNet)";

    public string CoreId => "legacy-v2ray";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public IReadOnlyCollection<string> SupportedServerTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "SOCKS",
        "SS",
        "SSR",
        "Trojan",
        "VMess",
        "VLESS",
        "WireGuard",
        "SSH"
    };

    public bool CanStart(Server server)
    {
        return SupportedServerTypes.Contains(server.Type);
    }

    public async Task<Socks5Server> StartAsync(Server s)
    {
        _controller = new V2rayController
        {
            Socks5LocalPort = Socks5LocalPort,
            LocalAddress = LocalAddress
        };

        return await _controller.StartAsync(s);
    }

    public async Task StopAsync()
    {
        if (_controller != null)
            await _controller.StopAsync();
    }
}
