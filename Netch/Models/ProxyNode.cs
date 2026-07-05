namespace Netch.Models;

public class ProxyNode
{
    public string Protocol { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Server { get; set; } = string.Empty;

    public ushort ServerPort { get; set; }

    public string? Uuid { get; set; }

    public string? Password { get; set; }

    public string? Flow { get; set; }

    public string? PacketEncoding { get; set; }

    public string? Transport { get; set; }

    public string? Path { get; set; }

    public string? Host { get; set; }

    public TlsOptions Tls { get; set; } = new();

    public RealityOptions Reality { get; set; } = new();

    public Hysteria2Options Hysteria2 { get; set; } = new();

    public TuicOptions Tuic { get; set; } = new();
}

public class TlsOptions
{
    public bool Enabled { get; set; }

    public bool Insecure { get; set; }

    public string? ServerName { get; set; }

    public string? Fingerprint { get; set; }

    public List<string> Alpn { get; set; } = new();
}

public class RealityOptions
{
    public bool Enabled { get; set; }

    public string? PublicKey { get; set; }

    public string? ShortId { get; set; }

    public string? SpiderX { get; set; }
}

public class Hysteria2Options
{
    public int? UpMbps { get; set; }

    public int? DownMbps { get; set; }

    public string? ObfsPassword { get; set; }
}

public class TuicOptions
{
    public string? CongestionControl { get; set; }

    public string? UdpRelayMode { get; set; }
}
