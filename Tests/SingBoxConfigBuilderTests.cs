using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Netch.Servers;
using Netch.Services;

namespace Tests;

[TestClass]
public class SingBoxConfigBuilderTests
{
    [TestMethod]
    public void BuildClientConfig_GeneratesVlessRealityOutbound()
    {
        var server = new ModernProxyServer
        {
            Hostname = "reality.example.com",
            Port = 443,
            Node = new ProxyNode
            {
                Protocol = "vless",
                Server = "reality.example.com",
                ServerPort = 443,
                Uuid = "22222222-2222-2222-2222-222222222222",
                Flow = "xtls-rprx-vision",
                PacketEncoding = "xudp",
                Tls =
                {
                    Enabled = true,
                    ServerName = "www.example.com",
                    Fingerprint = "chrome"
                },
                Reality =
                {
                    Enabled = true,
                    PublicKey = "public-key",
                    ShortId = "short-id"
                }
            }
        };

        var config = SingBoxConfigBuilder.BuildClientConfig(server, "127.0.0.1", 2801);
        Assert.IsFalse(config.Contains("null"));

        using var document = JsonDocument.Parse(config);
        var outbound = document.RootElement.GetProperty("outbounds")[0];
        var tls = outbound.GetProperty("tls");

        Assert.AreEqual("vless", outbound.GetProperty("type").GetString());
        Assert.AreEqual("xtls-rprx-vision", outbound.GetProperty("flow").GetString());
        Assert.AreEqual("public-key", tls.GetProperty("reality").GetProperty("public_key").GetString());
        Assert.AreEqual("chrome", tls.GetProperty("utls").GetProperty("fingerprint").GetString());
    }

    [TestMethod]
    public void BuildClientConfig_GeneratesHysteria2Outbound()
    {
        var server = new ModernProxyServer
        {
            Hostname = "hy2.example.com",
            Port = 443,
            Node = new ProxyNode
            {
                Protocol = "hysteria2",
                Server = "hy2.example.com",
                ServerPort = 443,
                Password = "secret",
                Hysteria2 =
                {
                    UpMbps = 100,
                    DownMbps = 200,
                    ObfsPassword = "obfs-secret"
                },
                Tls =
                {
                    Enabled = true,
                    ServerName = "hy2.example.com"
                }
            }
        };

        using var document = JsonDocument.Parse(SingBoxConfigBuilder.BuildClientConfig(server, "127.0.0.1", 2801));
        var outbound = document.RootElement.GetProperty("outbounds")[0];

        Assert.AreEqual("hysteria2", outbound.GetProperty("type").GetString());
        Assert.AreEqual("secret", outbound.GetProperty("password").GetString());
        Assert.AreEqual(100, outbound.GetProperty("up_mbps").GetInt32());
        Assert.AreEqual("salamander", outbound.GetProperty("obfs").GetProperty("type").GetString());
    }

    [TestMethod]
    public void BuildClientConfig_GeneratesTuicOutbound()
    {
        var server = new ModernProxyServer
        {
            Hostname = "tuic.example.com",
            Port = 443,
            Node = new ProxyNode
            {
                Protocol = "tuic",
                Server = "tuic.example.com",
                ServerPort = 443,
                Uuid = "33333333-3333-3333-3333-333333333333",
                Password = "secret",
                Tuic =
                {
                    CongestionControl = "bbr",
                    UdpRelayMode = "native"
                },
                Tls =
                {
                    Enabled = true,
                    ServerName = "tuic.example.com"
                }
            }
        };

        using var document = JsonDocument.Parse(SingBoxConfigBuilder.BuildClientConfig(server, "127.0.0.1", 2801));
        var outbound = document.RootElement.GetProperty("outbounds")[0];

        Assert.AreEqual("tuic", outbound.GetProperty("type").GetString());
        Assert.AreEqual("33333333-3333-3333-3333-333333333333", outbound.GetProperty("uuid").GetString());
        Assert.AreEqual("secret", outbound.GetProperty("password").GetString());
        Assert.AreEqual("bbr", outbound.GetProperty("congestion_control").GetString());
        Assert.AreEqual("native", outbound.GetProperty("udp_relay_mode").GetString());
    }

    [TestMethod]
    public void BuildClientConfig_GeneratesAnyTlsOutbound()
    {
        var server = new ModernProxyServer
        {
            Hostname = "anytls.example.com",
            Port = 443,
            Node = new ProxyNode
            {
                Protocol = "anytls",
                Server = "anytls.example.com",
                ServerPort = 443,
                Password = "secret",
                Tls =
                {
                    Enabled = true,
                    ServerName = "sni.example.com"
                }
            }
        };

        using var document = JsonDocument.Parse(SingBoxConfigBuilder.BuildClientConfig(server, "127.0.0.1", 2801));
        var outbound = document.RootElement.GetProperty("outbounds")[0];

        Assert.AreEqual("anytls", outbound.GetProperty("type").GetString());
        Assert.AreEqual("secret", outbound.GetProperty("password").GetString());
        var tls = outbound.GetProperty("tls");
        Assert.AreEqual("sni.example.com", tls.GetProperty("server_name").GetString());
        Assert.AreEqual("h2", tls.GetProperty("alpn")[0].GetString());
    }
}
