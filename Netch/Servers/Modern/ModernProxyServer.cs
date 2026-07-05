using Netch.Models;

namespace Netch.Servers;

public class ModernProxyServer : Server
{
    public override string Type { get; } = "Modern";

    public ProxyNode Node { get; set; } = new();

    public override string MaskedData()
    {
        return $"{Node.Protocol} + {Node.Transport ?? "tcp"}";
    }
}
