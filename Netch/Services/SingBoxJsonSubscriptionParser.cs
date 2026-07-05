using System.Text.Json;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers;

namespace Netch.Services;

public class SingBoxJsonSubscriptionParser : ISubscriptionParser
{
    public string Name => "sing-box-json";

    public bool CanParse(string text)
    {
        return text.Contains("\"outbounds\"", StringComparison.OrdinalIgnoreCase);
    }

    public SubscriptionParseResult Parse(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            if (!document.RootElement.TryGetProperty("outbounds", out var outbounds) || outbounds.ValueKind != JsonValueKind.Array)
                return SubscriptionParseResult.Empty;

            var servers = new List<Server>();
            var warnings = new List<string>();

            foreach (var outbound in outbounds.EnumerateArray())
            {
                var type = StringValue(outbound, "type")?.ToLowerInvariant();
                var tag = StringValue(outbound, "tag") ?? type ?? "sing-box";

                try
                {
                    switch (type)
                    {
                        case "vless":
                            servers.Add(ParseVless(outbound, tag));
                            break;
                        case "hysteria2":
                            servers.Add(ParseHysteria2(outbound, tag));
                            break;
                        case "tuic":
                            servers.Add(ParseTuic(outbound, tag));
                            break;
                        case "anytls":
                            servers.Add(ParseAnyTls(outbound, tag));
                            break;
                        case "selector":
                        case "urltest":
                        case "direct":
                        case "block":
                        case "dns":
                            break;
                        case null:
                            warnings.Add($"{tag}: missing outbound type.");
                            break;
                        default:
                            warnings.Add($"{tag}: sing-box outbound type \"{type}\" is not imported yet.");
                            break;
                    }
                }
                catch (Exception e) when (e is FormatException or InvalidOperationException or OverflowException)
                {
                    warnings.Add($"{tag}: {e.Message}");
                }
            }

            return new SubscriptionParseResult(servers, warnings: warnings);
        }
        catch (JsonException e)
        {
            return new SubscriptionParseResult(Array.Empty<Server>(), new[] { e.Message });
        }
    }

    private static ModernProxyServer ParseVless(JsonElement outbound, string tag)
    {
        var node = BaseModernNode("vless", outbound, tag);
        node.Uuid = RequiredString(outbound, "uuid");
        node.Flow = StringValue(outbound, "flow");
        node.PacketEncoding = StringValue(outbound, "packet_encoding");
        ApplyTls(node, outbound);
        ApplyTransport(node, outbound);
        return ToModernServer(node);
    }

    private static ModernProxyServer ParseHysteria2(JsonElement outbound, string tag)
    {
        var node = BaseModernNode("hysteria2", outbound, tag);
        node.Password = RequiredString(outbound, "password");
        node.Hysteria2.UpMbps = IntValue(outbound, "up_mbps");
        node.Hysteria2.DownMbps = IntValue(outbound, "down_mbps");

        if (outbound.TryGetProperty("obfs", out var obfs))
            node.Hysteria2.ObfsPassword = StringValue(obfs, "password");

        ApplyTls(node, outbound);
        return ToModernServer(node);
    }

    private static ModernProxyServer ParseTuic(JsonElement outbound, string tag)
    {
        var node = BaseModernNode("tuic", outbound, tag);
        node.Uuid = RequiredString(outbound, "uuid");
        node.Password = RequiredString(outbound, "password");
        node.Tuic.CongestionControl = StringValue(outbound, "congestion_control");
        node.Tuic.UdpRelayMode = StringValue(outbound, "udp_relay_mode");
        ApplyTls(node, outbound);
        return ToModernServer(node);
    }

    private static ModernProxyServer ParseAnyTls(JsonElement outbound, string tag)
    {
        var node = BaseModernNode("anytls", outbound, tag);
        node.Password = RequiredString(outbound, "password");
        ApplyTls(node, outbound);
        return ToModernServer(node);
    }

    private static ProxyNode BaseModernNode(string protocol, JsonElement outbound, string tag)
    {
        return new ProxyNode
        {
            Protocol = protocol,
            Name = tag,
            Server = RequiredString(outbound, "server"),
            ServerPort = RequiredPort(outbound)
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

    private static void ApplyTls(ProxyNode node, JsonElement outbound)
    {
        if (!outbound.TryGetProperty("tls", out var tls) || tls.ValueKind != JsonValueKind.Object)
            return;

        node.Tls.Enabled = BoolValue(tls, "enabled");
        node.Tls.Insecure = BoolValue(tls, "insecure");
        node.Tls.ServerName = StringValue(tls, "server_name");

        if (tls.TryGetProperty("alpn", out var alpn) && alpn.ValueKind == JsonValueKind.Array)
            node.Tls.Alpn = alpn.EnumerateArray().Select(item => item.GetString()).Where(item => !string.IsNullOrWhiteSpace(item)).ToList()!;

        if (tls.TryGetProperty("utls", out var utls))
            node.Tls.Fingerprint = StringValue(utls, "fingerprint");

        if (tls.TryGetProperty("reality", out var reality))
        {
            node.Reality.Enabled = BoolValue(reality, "enabled");
            node.Reality.PublicKey = StringValue(reality, "public_key");
            node.Reality.ShortId = StringValue(reality, "short_id");
        }
    }

    private static void ApplyTransport(ProxyNode node, JsonElement outbound)
    {
        if (!outbound.TryGetProperty("transport", out var transport) || transport.ValueKind != JsonValueKind.Object)
            return;

        node.Transport = StringValue(transport, "type");
        node.Path = StringValue(transport, "path") ?? StringValue(transport, "service_name");

        if (transport.TryGetProperty("headers", out var headers))
            node.Host = StringValue(headers, "Host") ?? StringValue(headers, "host");
    }

    private static string RequiredString(JsonElement element, string key)
    {
        return StringValue(element, key) ?? throw new FormatException($"Missing required field \"{key}\".");
    }

    private static ushort RequiredPort(JsonElement element)
    {
        var port = IntValue(element, "server_port") ?? throw new FormatException("Missing required field \"server_port\".");
        if (port is <= 0 or > ushort.MaxValue)
            throw new FormatException($"Invalid server_port \"{port}\".");

        return (ushort)port;
    }

    private static string? StringValue(JsonElement element, string key)
    {
        if (!element.TryGetProperty(key, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? IntValue(JsonElement element, string key)
    {
        if (!element.TryGetProperty(key, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;

        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number) ? number : null;
    }

    private static bool BoolValue(JsonElement element, string key)
    {
        if (!element.TryGetProperty(key, out var value))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var b) && b,
            _ => false
        };
    }
}
