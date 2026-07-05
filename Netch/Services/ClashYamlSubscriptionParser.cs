using Netch.Interfaces;
using Netch.Models;
using Netch.Servers;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Netch.Services;

public class ClashYamlSubscriptionParser : ISubscriptionParser
{
    public string Name => "clash-yaml";

    public bool CanParse(string text)
    {
        return text.Contains("proxies:", StringComparison.OrdinalIgnoreCase);
    }

    public SubscriptionParseResult Parse(string text)
    {
        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var root = deserializer.Deserialize<Dictionary<object, object>>(text);
            if (root == null || !root.TryGetValue("proxies", out var proxiesValue) || proxiesValue is not IEnumerable<object> proxies)
                return SubscriptionParseResult.Empty;

            var servers = new List<Server>();
            var warnings = new List<string>();

            foreach (var proxy in proxies)
            {
                if (proxy is not IDictionary<object, object> map)
                    continue;

                var name = StringValue(map, "name") ?? "";
                var type = StringValue(map, "type")?.ToLowerInvariant();

                try
                {
                    switch (type)
                    {
                        case "ss":
                            servers.Add(ParseShadowsocks(map, name));
                            break;
                        case "trojan":
                            servers.Add(ParseTrojan(map, name));
                            break;
                        case "vmess":
                            servers.Add(ParseVMess(map, name));
                            break;
                        case "vless":
                            servers.Add(ParseVLESS(map, name));

                            break;
                        case "hysteria2":
                        case "hysteria":
                            servers.Add(ParseHysteria2(map, name));
                            break;
                        case "tuic":
                            servers.Add(ParseTuic(map, name));
                            break;
                        case "anytls":
                            servers.Add(ParseAnyTls(map, name));
                            break;
                        case "mieru":
                        case "socks5":
                        case "http":
                            warnings.Add($"{name}: {type} nodes require a modern core adapter and are not imported yet.");
                            break;
                    }
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
                {
                    warnings.Add($"{name}: {e.Message}");
                }
            }

            return new SubscriptionParseResult(servers, warnings: warnings);
        }
        catch (YamlException e)
        {
            return new SubscriptionParseResult(Array.Empty<Server>(), new[] { e.Message });
        }
    }

    private static ShadowsocksServer ParseShadowsocks(IDictionary<object, object> map, string name)
    {
        return new ShadowsocksServer
        {
            Remark = name,
            Hostname = RequiredString(map, "server"),
            Port = RequiredPort(map),
            EncryptMethod = RequiredString(map, "cipher"),
            Password = RequiredString(map, "password"),
            Plugin = StringValue(map, "plugin"),
            PluginOption = StringValue(map, "plugin-opts")
        };
    }

    private static TrojanServer ParseTrojan(IDictionary<object, object> map, string name)
    {
        return new TrojanServer
        {
            Remark = name,
            Hostname = RequiredString(map, "server"),
            Port = RequiredPort(map),
            Password = RequiredString(map, "password"),
            Host = StringValue(map, "sni") ?? StringValue(map, "servername")
        };
    }

    private static VMessServer ParseVMess(IDictionary<object, object> map, string name)
    {
        var server = new VMessServer
        {
            Remark = name,
            Hostname = RequiredString(map, "server"),
            Port = RequiredPort(map),
            UserID = RequiredString(map, "uuid"),
            AlterID = IntValue(map, "alterId") ?? IntValue(map, "alter-id") ?? 0,
            EncryptMethod = StringValue(map, "cipher") ?? "auto",
            TransferProtocol = MapNetwork(StringValue(map, "network")),
            TLSSecureType = BoolValue(map, "tls") ? "tls" : "none",
            ServerName = StringValue(map, "servername") ?? StringValue(map, "sni")
        };

        ApplyTransportOptions(server, map);
        return server;
    }

    private static Server ParseVLESS(IDictionary<object, object> map, string name)
    {
        var flow = StringValue(map, "flow");
        if (map.ContainsKey("reality-opts") || !string.IsNullOrWhiteSpace(flow))
            return ParseModernVLESS(map, name);

        var server = new VLESSServer
        {
            Remark = name,
            Hostname = RequiredString(map, "server"),
            Port = RequiredPort(map),
            UserID = RequiredString(map, "uuid"),
            EncryptMethod = "none",
            TransferProtocol = MapNetwork(StringValue(map, "network")),
            TLSSecureType = BoolValue(map, "tls") ? "tls" : "none",
            ServerName = StringValue(map, "servername") ?? StringValue(map, "sni")
        };

        ApplyTransportOptions(server, map);
        return server;
    }

    private static ModernProxyServer ParseModernVLESS(IDictionary<object, object> map, string name)
    {
        var node = BaseModernNode("vless", map, name);
        node.Uuid = RequiredString(map, "uuid");
        node.Flow = StringValue(map, "flow");
        node.PacketEncoding = StringValue(map, "packet-encoding") ?? StringValue(map, "packet_encoding") ?? "xudp";
        node.Transport = MapNetwork(StringValue(map, "network"));
        node.Tls.Enabled = BoolValue(map, "tls") || map.ContainsKey("reality-opts");
        node.Tls.ServerName = StringValue(map, "servername") ?? StringValue(map, "sni");
        node.Tls.Fingerprint = StringValue(map, "client-fingerprint") ?? StringValue(map, "fingerprint");
        ApplyAlpn(node.Tls, map);

        if (TryGetMap(map, "reality-opts", out var realityOptions))
        {
            node.Reality.Enabled = true;
            node.Reality.PublicKey = StringValue(realityOptions, "public-key") ?? StringValue(realityOptions, "public_key");
            node.Reality.ShortId = StringValue(realityOptions, "short-id") ?? StringValue(realityOptions, "short_id");
            node.Reality.SpiderX = StringValue(realityOptions, "spider-x") ?? StringValue(realityOptions, "spider_x");
        }

        ApplyModernTransportOptions(node, map);
        return ToModernServer(node);
    }

    private static ModernProxyServer ParseHysteria2(IDictionary<object, object> map, string name)
    {
        var node = BaseModernNode("hysteria2", map, name);
        node.Password = RequiredString(map, "password");
        node.Hysteria2.UpMbps = IntValue(map, "up") ?? IntValue(map, "upmbps");
        node.Hysteria2.DownMbps = IntValue(map, "down") ?? IntValue(map, "downmbps");
        node.Tls.Enabled = true;
        node.Tls.ServerName = StringValue(map, "sni");
        node.Tls.Insecure = BoolValue(map, "skip-cert-verify");
        ApplyAlpn(node.Tls, map);

        if (TryGetMap(map, "obfs", out var obfs))
            node.Hysteria2.ObfsPassword = StringValue(obfs, "password");
        else
            node.Hysteria2.ObfsPassword = StringValue(map, "obfs-password");

        return ToModernServer(node);
    }

    private static ModernProxyServer ParseTuic(IDictionary<object, object> map, string name)
    {
        var node = BaseModernNode("tuic", map, name);
        node.Uuid = RequiredString(map, "uuid");
        node.Password = RequiredString(map, "password");
        node.Tuic.CongestionControl = StringValue(map, "congestion-controller") ?? StringValue(map, "congestion-control");
        node.Tuic.UdpRelayMode = StringValue(map, "udp-relay-mode");
        node.Tls.Enabled = true;
        node.Tls.ServerName = StringValue(map, "sni");
        node.Tls.Insecure = BoolValue(map, "skip-cert-verify");
        ApplyAlpn(node.Tls, map);
        return ToModernServer(node);
    }

    private static ModernProxyServer ParseAnyTls(IDictionary<object, object> map, string name)
    {
        var node = BaseModernNode("anytls", map, name);
        node.Password = RequiredString(map, "password");
        node.Tls.Enabled = true;
        node.Tls.ServerName = StringValue(map, "sni") ?? StringValue(map, "servername");
        node.Tls.Insecure = BoolValue(map, "skip-cert-verify") || BoolValue(map, "skip-cert-check");
        ApplyAlpn(node.Tls, map);
        return ToModernServer(node);
    }

    private static ProxyNode BaseModernNode(string protocol, IDictionary<object, object> map, string name)
    {
        return new ProxyNode
        {
            Protocol = protocol,
            Name = name,
            Server = RequiredString(map, "server"),
            ServerPort = RequiredPort(map)
        };
    }

    private static ModernProxyServer ToModernServer(ProxyNode node)
    {
        return new ModernProxyServer
        {
            Remark = node.Name,
            Hostname = node.Server,
            Port = node.ServerPort,
            Node = node
        };
    }

    private static void ApplyTransportOptions(VMessServer server, IDictionary<object, object> map)
    {
        switch (server.TransferProtocol)
        {
            case "ws":
                if (TryGetMap(map, "ws-opts", out var wsOptions))
                {
                    server.Path = StringValue(wsOptions, "path");
                    if (TryGetMap(wsOptions, "headers", out var headers))
                        server.Host = StringValue(headers, "Host") ?? StringValue(headers, "host");
                }

                break;
            case "grpc":
                if (TryGetMap(map, "grpc-opts", out var grpcOptions))
                {
                    server.Path = StringValue(grpcOptions, "grpc-service-name") ?? StringValue(grpcOptions, "serviceName");
                    server.FakeType = BoolValue(grpcOptions, "grpc-mode") ? "multi" : "gun";
                }

                break;
            case "h2":
                if (TryGetMap(map, "h2-opts", out var h2Options))
                {
                    server.Path = StringValue(h2Options, "path");
                    server.Host = StringValue(h2Options, "host");
                }

                break;
        }
    }

    private static void ApplyModernTransportOptions(ProxyNode node, IDictionary<object, object> map)
    {
        switch (node.Transport)
        {
            case "ws":
                if (TryGetMap(map, "ws-opts", out var wsOptions))
                {
                    node.Path = StringValue(wsOptions, "path");
                    if (TryGetMap(wsOptions, "headers", out var headers))
                        node.Host = StringValue(headers, "Host") ?? StringValue(headers, "host");
                }

                break;
            case "grpc":
                if (TryGetMap(map, "grpc-opts", out var grpcOptions))
                    node.Path = StringValue(grpcOptions, "grpc-service-name") ?? StringValue(grpcOptions, "serviceName");

                break;
        }
    }

    private static void ApplyAlpn(TlsOptions tls, IDictionary<object, object> map)
    {
        if (!map.TryGetValue("alpn", out var value))
            return;

        switch (value)
        {
            case IEnumerable<object> list:
                tls.Alpn = list.Select(item => item.ToString()).Where(item => !string.IsNullOrWhiteSpace(item)).ToList()!;
                break;
            case string s when !string.IsNullOrWhiteSpace(s):
                tls.Alpn = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                break;
        }
    }

    private static string MapNetwork(string? network)
    {
        return network?.ToLowerInvariant() switch
        {
            null or "" => "tcp",
            "http" => "h2",
            "h2" => "h2",
            "ws" => "ws",
            "grpc" => "grpc",
            _ => "tcp"
        };
    }

    private static bool TryGetMap(IDictionary<object, object> map, string key, out IDictionary<object, object> value)
    {
        if (map.TryGetValue(key, out var raw) && raw is IDictionary<object, object> rawMap)
        {
            value = rawMap;
            return true;
        }

        value = new Dictionary<object, object>();
        return false;
    }

    private static string RequiredString(IDictionary<object, object> map, string key)
    {
        return StringValue(map, key) ?? throw new FormatException($"Missing required field \"{key}\".");
    }

    private static ushort RequiredPort(IDictionary<object, object> map)
    {
        var port = IntValue(map, "port") ?? throw new FormatException("Missing required field \"port\".");
        if (port is <= 0 or > ushort.MaxValue)
            throw new FormatException($"Invalid port \"{port}\".");

        return (ushort)port;
    }

    private static string? StringValue(IDictionary<object, object> map, string key)
    {
        return map.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static int? IntValue(IDictionary<object, object> map, string key)
    {
        if (!map.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            int i => i,
            long l => checked((int)l),
            short s => s,
            string s when int.TryParse(s, out var i) => i,
            _ => null
        };
    }

    private static bool BoolValue(IDictionary<object, object> map, string key)
    {
        if (!map.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var b) && b,
            _ => false
        };
    }
}
