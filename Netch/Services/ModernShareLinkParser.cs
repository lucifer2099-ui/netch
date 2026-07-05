using System.Web;
using Netch.Models;
using Netch.Servers;

namespace Netch.Services;

public static class ModernShareLinkParser
{
    public static bool TryParse(string text, out IEnumerable<Server> servers)
    {
        servers = Array.Empty<Server>();

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
            return false;

        var scheme = uri.Scheme.ToLowerInvariant();
        try
        {
            servers = scheme switch
            {
                "anytls" => new[] { ParseAnyTls(uri) },
                "hy2" or "hysteria2" => new[] { ParseHysteria2(uri) },
                "tuic" => new[] { ParseTuic(uri) },
                "vless" when IsModernVless(uri) => new[] { ParseVless(uri) },
                _ => Array.Empty<Server>()
            };

            return servers.Any();
        }
        catch (Exception e) when (e is FormatException or UriFormatException or OverflowException)
        {
            Log.Warning(e, "Parse modern share link failed");
            return false;
        }
    }

    private static ModernProxyServer ParseHysteria2(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        var node = BaseNode("hysteria2", uri);

        node.Password = Uri.UnescapeDataString(uri.UserInfo);
        if (string.IsNullOrWhiteSpace(node.Password))
            node.Password = query.Get("auth") ?? query.Get("password");
        if (string.IsNullOrWhiteSpace(node.Password))
            throw new FormatException("Hysteria2 link must contain password.");

        node.Hysteria2.UpMbps = IntValue(query.Get("upmbps")) ?? IntValue(query.Get("up")) ?? IntValue(query.Get("up_mbps"));
        node.Hysteria2.DownMbps = IntValue(query.Get("downmbps")) ?? IntValue(query.Get("down")) ?? IntValue(query.Get("down_mbps"));

        var obfs = query.Get("obfs");
        if (string.Equals(obfs, "salamander", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(query.Get("obfs-password")))
            node.Hysteria2.ObfsPassword = query.Get("obfs-password") ?? query.Get("obfs_password");

        ApplyTls(node, query, defaultEnabled: true);
        return ToModernServer(node);
    }

    private static ModernProxyServer ParseAnyTls(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        var node = BaseNode("anytls", uri);

        node.Password = Uri.UnescapeDataString(uri.UserInfo);
        if (string.IsNullOrWhiteSpace(node.Password))
            node.Password = query.Get("password");
        if (string.IsNullOrWhiteSpace(node.Password))
            throw new FormatException("AnyTLS link must contain password.");

        ApplyTls(node, query, defaultEnabled: true);
        return ToModernServer(node);
    }

    private static ModernProxyServer ParseTuic(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        var node = BaseNode("tuic", uri);
        var userInfo = Uri.UnescapeDataString(uri.UserInfo);
        var separator = userInfo.IndexOf(':');
        if (separator <= 0 || separator == userInfo.Length - 1)
            throw new FormatException("TUIC link must contain uuid:password user info.");

        node.Uuid = userInfo[..separator];
        node.Password = userInfo[(separator + 1)..];
        node.Tuic.CongestionControl = query.Get("congestion_control") ?? query.Get("congestion-control");
        node.Tuic.UdpRelayMode = query.Get("udp_relay_mode") ?? query.Get("udp-relay-mode");

        ApplyTls(node, query, defaultEnabled: true);
        return ToModernServer(node);
    }

    private static ModernProxyServer ParseVless(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        var node = BaseNode("vless", uri);

        node.Uuid = Uri.UnescapeDataString(uri.UserInfo);
        node.Flow = query.Get("flow");
        node.PacketEncoding = query.Get("packetEncoding") ?? query.Get("packet-encoding") ?? query.Get("packet_encoding") ?? "xudp";
        node.Transport = MapTransport(query.Get("type"));
        node.Path = query.Get("path") ?? query.Get("serviceName") ?? query.Get("service_name");
        node.Host = query.Get("host");

        var security = query.Get("security");
        node.Tls.Enabled = string.Equals(security, "tls", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(security, "reality", StringComparison.OrdinalIgnoreCase);
        node.Tls.ServerName = query.Get("sni") ?? query.Get("servername") ?? query.Get("serverName");
        node.Tls.Fingerprint = query.Get("fp") ?? query.Get("fingerprint");
        ApplyAlpn(node.Tls, query.Get("alpn"));

        if (string.Equals(security, "reality", StringComparison.OrdinalIgnoreCase))
        {
            node.Reality.Enabled = true;
            node.Reality.PublicKey = query.Get("pbk") ?? query.Get("publicKey") ?? query.Get("public-key");
            node.Reality.ShortId = query.Get("sid") ?? query.Get("shortId") ?? query.Get("short-id");
            node.Reality.SpiderX = query.Get("spx") ?? query.Get("spiderX") ?? query.Get("spider-x");
        }

        return ToModernServer(node);
    }

    private static bool IsModernVless(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        return string.Equals(query.Get("security"), "reality", StringComparison.OrdinalIgnoreCase) ||
               !string.IsNullOrWhiteSpace(query.Get("flow")) ||
               !string.IsNullOrWhiteSpace(query.Get("pbk")) ||
               !string.IsNullOrWhiteSpace(query.Get("publicKey")) ||
               !string.IsNullOrWhiteSpace(query.Get("public-key"));
    }

    private static ProxyNode BaseNode(string protocol, Uri uri)
    {
        return new ProxyNode
        {
            Protocol = protocol,
            Name = Uri.UnescapeDataString(uri.Fragment.TrimStart('#')),
            Server = uri.Host,
            ServerPort = RequiredPort(uri)
        };
    }

    private static ushort RequiredPort(Uri uri)
    {
        if (uri.Port <= 0 || uri.Port > ushort.MaxValue)
            throw new FormatException("Missing or invalid port.");

        return (ushort)uri.Port;
    }

    private static ModernProxyServer ToModernServer(ProxyNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Name))
            node.Name = $"{node.Protocol}-{node.Server}:{node.ServerPort}";

        return new ModernProxyServer
        {
            Remark = node.Name,
            Hostname = node.Server,
            Port = node.ServerPort,
            Node = node
        };
    }

    private static void ApplyTls(ProxyNode node, System.Collections.Specialized.NameValueCollection query, bool defaultEnabled)
    {
        node.Tls.Enabled = BoolValue(query.Get("tls")) ?? defaultEnabled;
        node.Tls.Insecure = BoolValue(query.Get("insecure")) ?? BoolValue(query.Get("allowInsecure")) ?? BoolValue(query.Get("allow_insecure")) ?? false;
        node.Tls.ServerName = query.Get("sni") ?? query.Get("peer") ?? query.Get("servername") ?? query.Get("serverName");
        node.Tls.Fingerprint = query.Get("fp") ?? query.Get("fingerprint");
        ApplyAlpn(node.Tls, query.Get("alpn"));
    }

    private static void ApplyAlpn(TlsOptions tls, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        tls.Alpn = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static string? MapTransport(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            null or "" or "tcp" => null,
            "http" => "h2",
            "ws" => "ws",
            "grpc" => "grpc",
            "h2" => "h2",
            _ => value?.ToLowerInvariant()
        };
    }

    private static int? IntValue(string? value)
    {
        return int.TryParse(value, out var number) ? number : null;
    }

    private static bool? BoolValue(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "1" or "true" or "yes" => true,
            "0" or "false" or "no" => false,
            _ => null
        };
    }
}
