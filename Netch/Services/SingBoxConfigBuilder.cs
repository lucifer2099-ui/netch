using System.Text.Json;
using Netch.Models;
using Netch.Servers;

namespace Netch.Services;

public static class SingBoxConfigBuilder
{
    public static string BuildClientConfig(ModernProxyServer server, string listenAddress, ushort listenPort)
    {
        var config = new Dictionary<string, object?>
        {
            ["log"] = new Dictionary<string, object?>
            {
                ["level"] = "info",
                ["timestamp"] = true
            },
            ["inbounds"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "mixed",
                    ["tag"] = "mixed-in",
                    ["listen"] = listenAddress,
                    ["listen_port"] = listenPort
                }
            },
            ["outbounds"] = new object[]
            {
                BuildOutbound(server.Node)
            }
        };

        return JsonSerializer.Serialize(WithoutNulls(config), Global.NewCustomJsonSerializerOptions());
    }

    private static Dictionary<string, object?> BuildOutbound(ProxyNode node)
    {
        var outbound = new Dictionary<string, object?>
        {
            ["type"] = node.Protocol.ToLowerInvariant(),
            ["tag"] = "proxy",
            ["server"] = node.Server,
            ["server_port"] = node.ServerPort
        };

        switch (node.Protocol.ToLowerInvariant())
        {
            case "vless":
                outbound["uuid"] = Required(node.Uuid, "uuid");
                outbound["flow"] = node.Flow;
                outbound["packet_encoding"] = node.PacketEncoding;
                outbound["network"] = "tcp";
                break;
            case "hysteria2":
                outbound["password"] = Required(node.Password, "password");
                outbound["up_mbps"] = node.Hysteria2.UpMbps;
                outbound["down_mbps"] = node.Hysteria2.DownMbps;
                if (!string.IsNullOrWhiteSpace(node.Hysteria2.ObfsPassword))
                {
                    outbound["obfs"] = new Dictionary<string, object?>
                    {
                        ["type"] = "salamander",
                        ["password"] = node.Hysteria2.ObfsPassword
                    };
                }

                break;
            case "tuic":
                outbound["uuid"] = Required(node.Uuid, "uuid");
                outbound["password"] = Required(node.Password, "password");
                outbound["congestion_control"] = node.Tuic.CongestionControl;
                outbound["udp_relay_mode"] = node.Tuic.UdpRelayMode;
                break;
            case "anytls":
                outbound["password"] = Required(node.Password, "password");
                break;
            default:
                throw new NotSupportedException($"sing-box outbound protocol \"{node.Protocol}\" is not supported yet.");
        }

        if (node.Tls.Enabled || node.Reality.Enabled)
            outbound["tls"] = BuildTls(node);

        if (!string.IsNullOrWhiteSpace(node.Transport) && node.Transport != "tcp")
            outbound["transport"] = BuildTransport(node);

        return outbound;
    }

    private static Dictionary<string, object?> BuildTls(ProxyNode node)
    {
        var tls = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["disable_sni"] = false,
            ["server_name"] = node.Tls.ServerName,
            ["insecure"] = node.Tls.Insecure
        };

        if (node.Tls.Alpn.Any())
            tls["alpn"] = node.Tls.Alpn;
        else if (string.Equals(node.Protocol, "anytls", StringComparison.OrdinalIgnoreCase))
            tls["alpn"] = new[] { "h2" };

        if (!string.IsNullOrWhiteSpace(node.Tls.Fingerprint))
        {
            tls["utls"] = new Dictionary<string, object?>
            {
                ["enabled"] = true,
                ["fingerprint"] = node.Tls.Fingerprint
            };
        }

        if (node.Reality.Enabled)
        {
            tls["reality"] = new Dictionary<string, object?>
            {
                ["enabled"] = true,
                ["public_key"] = Required(node.Reality.PublicKey, "reality public key"),
                ["short_id"] = node.Reality.ShortId
            };
        }

        return tls;
    }

    private static Dictionary<string, object?> BuildTransport(ProxyNode node)
    {
        return node.Transport switch
        {
            "ws" => new Dictionary<string, object?>
            {
                ["type"] = "ws",
                ["path"] = node.Path,
                ["headers"] = string.IsNullOrWhiteSpace(node.Host)
                    ? null
                    : new Dictionary<string, object?> { ["Host"] = node.Host }
            },
            "grpc" => new Dictionary<string, object?>
            {
                ["type"] = "grpc",
                ["service_name"] = node.Path
            },
            _ => throw new NotSupportedException($"sing-box transport \"{node.Transport}\" is not supported yet.")
        };
    }

    private static string Required(string? value, string field)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new FormatException($"Missing required field \"{field}\".") : value;
    }

    private static object? WithoutNulls(object? value)
    {
        return value switch
        {
            null => null,
            Dictionary<string, object?> dictionary => dictionary
                .Select(item => new KeyValuePair<string, object?>(item.Key, WithoutNulls(item.Value)))
                .Where(item => item.Value != null)
                .ToDictionary(item => item.Key, item => item.Value),
            IEnumerable<object?> list when value is not string => list
                .Select(WithoutNulls)
                .Where(item => item != null)
                .ToArray(),
            _ => value
        };
    }
}
