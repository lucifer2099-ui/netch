using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Servers;
using Netch.Services;

namespace Tests;

[TestClass]
public class SubscriptionParserRegistryTests
{
    [TestMethod]
    public void Parse_UsesLegacyShareLinkParser_ForPlainUri()
    {
        var result = SubscriptionParserRegistry.Parse("trojan://secret@example.com:443?sni=example.com#demo");

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(1, result.Servers.Count);

        var server = (TrojanServer)result.Servers[0];
        Assert.AreEqual("secret", server.Password);
        Assert.AreEqual("example.com", server.Hostname);
        Assert.AreEqual((ushort)443, server.Port);
        Assert.AreEqual("example.com", server.Host);
        Assert.AreEqual("demo", server.Remark);
    }

    [TestMethod]
    public void Parse_UsesLegacyShareLinkParser_ForBase64Subscription()
    {
        const string links =
            "ss://YWVzLTEyOC1nY206c2VjcmV0QHNzLmV4YW1wbGUuY29tOjgzODg#ss-demo\n" +
            "vless://22222222-2222-2222-2222-222222222222@vless.example.com:443?type=tcp&security=tls&sni=vless-sni.example.com&encryption=none#vless-demo\n" +
            "vmess://33333333-3333-3333-3333-333333333333@vmess.example.com:443?type=tcp&security=tls&sni=vmess-sni.example.com&encryption=auto#vmess-demo";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(links));

        var result = SubscriptionParserRegistry.Parse(encoded);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual(3, result.Servers.Count);
        Assert.IsInstanceOfType(result.Servers[0], typeof(ShadowsocksServer));
        Assert.IsInstanceOfType(result.Servers[1], typeof(VLESSServer));
        Assert.IsInstanceOfType(result.Servers[2], typeof(VMessServer));
    }

    [TestMethod]
    public void Parse_ImportsModernShareLinks()
    {
        const string links =
            "anytls://anytls-secret@anytls.example.com:443?sni=anytls-sni.example.com&insecure=0#anytls-demo\n" +
            "hysteria2://hy2-secret@hy2.example.com:443?sni=hy2-sni.example.com&insecure=1&obfs=salamander&obfs-password=obfs-secret&upmbps=100&downmbps=200#hy2-demo\n" +
            "hy2://hy2-alt-secret@hy2-alt.example.com:443?auth=hy2-alt-secret&sni=hy2-alt.example.com#hy2-alt-demo\n" +
            "tuic://33333333-3333-3333-3333-333333333333:tuic-secret@tuic.example.com:443?congestion_control=bbr&udp_relay_mode=native&sni=tuic.example.com&alpn=h3#tuic-demo\n" +
            "vless://22222222-2222-2222-2222-222222222222@reality.example.com:443?type=tcp&security=reality&flow=xtls-rprx-vision&encryption=none&sni=www.example.com&fp=chrome&pbk=public-key&sid=0123&spx=%2F#reality-demo";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(links));

        var result = SubscriptionParserRegistry.Parse(encoded);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual(5, result.Servers.Count);

        var anyTls = (ModernProxyServer)result.Servers[0];
        Assert.AreEqual("anytls", anyTls.Node.Protocol);
        Assert.AreEqual("anytls-secret", anyTls.Node.Password);
        Assert.AreEqual("anytls-sni.example.com", anyTls.Node.Tls.ServerName);

        var hysteria2 = (ModernProxyServer)result.Servers[1];
        Assert.AreEqual("hysteria2", hysteria2.Node.Protocol);
        Assert.AreEqual("hy2-secret", hysteria2.Node.Password);
        Assert.AreEqual(100, hysteria2.Node.Hysteria2.UpMbps);
        Assert.AreEqual(200, hysteria2.Node.Hysteria2.DownMbps);
        Assert.AreEqual("obfs-secret", hysteria2.Node.Hysteria2.ObfsPassword);
        Assert.IsTrue(hysteria2.Node.Tls.Insecure);
        Assert.AreEqual("hy2-sni.example.com", hysteria2.Node.Tls.ServerName);

        var hy2Alias = (ModernProxyServer)result.Servers[2];
        Assert.AreEqual("hysteria2", hy2Alias.Node.Protocol);
        Assert.AreEqual("hy2-alt-secret", hy2Alias.Node.Password);
        Assert.AreEqual("hy2-alt.example.com", hy2Alias.Node.Tls.ServerName);

        var tuic = (ModernProxyServer)result.Servers[3];
        Assert.AreEqual("tuic", tuic.Node.Protocol);
        Assert.AreEqual("33333333-3333-3333-3333-333333333333", tuic.Node.Uuid);
        Assert.AreEqual("tuic-secret", tuic.Node.Password);
        Assert.AreEqual("bbr", tuic.Node.Tuic.CongestionControl);
        Assert.AreEqual("native", tuic.Node.Tuic.UdpRelayMode);
        Assert.AreEqual("h3", tuic.Node.Tls.Alpn[0]);

        var reality = (ModernProxyServer)result.Servers[4];
        Assert.AreEqual("vless", reality.Node.Protocol);
        Assert.AreEqual("xtls-rprx-vision", reality.Node.Flow);
        Assert.AreEqual("xudp", reality.Node.PacketEncoding);
        Assert.IsTrue(reality.Node.Reality.Enabled);
        Assert.AreEqual("public-key", reality.Node.Reality.PublicKey);
        Assert.AreEqual("0123", reality.Node.Reality.ShortId);
        Assert.AreEqual("/", reality.Node.Reality.SpiderX);
        Assert.AreEqual("www.example.com", reality.Node.Tls.ServerName);
        Assert.AreEqual("chrome", reality.Node.Tls.Fingerprint);
    }

    [TestMethod]
    public void Parse_ImportsSupportedClashYamlNodes()
    {
        const string yaml =
            "proxies:\n" +
            "  - name: ss-demo\n" +
            "    type: ss\n" +
            "    server: ss.example.com\n" +
            "    port: 8388\n" +
            "    cipher: aes-128-gcm\n" +
            "    password: ss-secret\n" +
            "  - name: trojan-demo\n" +
            "    type: trojan\n" +
            "    server: trojan.example.com\n" +
            "    port: 443\n" +
            "    password: trojan-secret\n" +
            "    sni: cdn.example.com\n" +
            "  - name: vmess-ws-demo\n" +
            "    type: vmess\n" +
            "    server: vmess.example.com\n" +
            "    port: 443\n" +
            "    uuid: 11111111-1111-1111-1111-111111111111\n" +
            "    alterId: 0\n" +
            "    cipher: auto\n" +
            "    tls: true\n" +
            "    servername: vmess-sni.example.com\n" +
            "    network: ws\n" +
            "    ws-opts:\n" +
            "      path: /ws\n" +
            "      headers:\n" +
            "        Host: ws-host.example.com\n";

        var result = SubscriptionParserRegistry.Parse(yaml);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual(3, result.Servers.Count);

        var ss = (ShadowsocksServer)result.Servers[0];
        Assert.AreEqual("ss-demo", ss.Remark);
        Assert.AreEqual("ss.example.com", ss.Hostname);
        Assert.AreEqual((ushort)8388, ss.Port);
        Assert.AreEqual("aes-128-gcm", ss.EncryptMethod);
        Assert.AreEqual("ss-secret", ss.Password);

        var trojan = (TrojanServer)result.Servers[1];
        Assert.AreEqual("trojan-demo", trojan.Remark);
        Assert.AreEqual("trojan.example.com", trojan.Hostname);
        Assert.AreEqual("cdn.example.com", trojan.Host);

        var vmess = (VMessServer)result.Servers[2];
        Assert.AreEqual("vmess-ws-demo", vmess.Remark);
        Assert.AreEqual("ws", vmess.TransferProtocol);
        Assert.AreEqual("tls", vmess.TLSSecureType);
        Assert.AreEqual("vmess-sni.example.com", vmess.ServerName);
        Assert.AreEqual("/ws", vmess.Path);
        Assert.AreEqual("ws-host.example.com", vmess.Host);
    }

    [TestMethod]
    public void Parse_ImportsModernClashNodesAsModernProxyServers()
    {
        const string yaml =
            "proxies:\n" +
            "  - name: reality-demo\n" +
            "    type: vless\n" +
            "    server: reality.example.com\n" +
            "    port: 443\n" +
            "    uuid: 22222222-2222-2222-2222-222222222222\n" +
            "    tls: true\n" +
            "    flow: xtls-rprx-vision\n" +
            "    reality-opts:\n" +
            "      public-key: abc\n" +
            "      short-id: def\n" +
            "  - name: tuic-demo\n" +
            "    type: tuic\n" +
            "    server: tuic.example.com\n" +
            "    port: 443\n" +
            "    uuid: 33333333-3333-3333-3333-333333333333\n" +
            "    password: secret\n" +
            "  - name: hy2-demo\n" +
            "    type: hysteria2\n" +
            "    server: hy2.example.com\n" +
            "    port: 443\n" +
            "    password: hy2-secret\n" +
            "    sni: hy2-sni.example.com\n" +
            "    up: 100\n" +
            "    down: 200\n" +
            "  - name: anytls-demo\n" +
            "    type: anytls\n" +
            "    server: anytls.example.com\n" +
            "    port: 443\n" +
            "    password: anytls-secret\n" +
            "    sni: anytls-sni.example.com\n" +
            "    skip-cert-verify: false\n";

        var result = SubscriptionParserRegistry.Parse(yaml);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(4, result.Servers.Count);
        Assert.AreEqual(0, result.Warnings.Count);

        var reality = (ModernProxyServer)result.Servers[0];
        Assert.AreEqual("vless", reality.Node.Protocol);
        Assert.AreEqual("xtls-rprx-vision", reality.Node.Flow);
        Assert.IsTrue(reality.Node.Reality.Enabled);
        Assert.AreEqual("abc", reality.Node.Reality.PublicKey);

        var tuic = (ModernProxyServer)result.Servers[1];
        Assert.AreEqual("tuic", tuic.Node.Protocol);
        Assert.AreEqual("33333333-3333-3333-3333-333333333333", tuic.Node.Uuid);
        Assert.AreEqual("secret", tuic.Node.Password);

        var hysteria2 = (ModernProxyServer)result.Servers[2];
        Assert.AreEqual("hysteria2", hysteria2.Node.Protocol);
        Assert.AreEqual("hy2-secret", hysteria2.Node.Password);
        Assert.AreEqual(100, hysteria2.Node.Hysteria2.UpMbps);
        Assert.AreEqual(200, hysteria2.Node.Hysteria2.DownMbps);
        Assert.AreEqual("hy2-sni.example.com", hysteria2.Node.Tls.ServerName);

        var anyTls = (ModernProxyServer)result.Servers[3];
        Assert.AreEqual("anytls", anyTls.Node.Protocol);
        Assert.AreEqual("anytls-secret", anyTls.Node.Password);
        Assert.AreEqual("anytls-sni.example.com", anyTls.Node.Tls.ServerName);
    }

    [TestMethod]
    public void Parse_ImportsSingBoxJsonOutbounds()
    {
        const string json =
            "{\n" +
            "  \"outbounds\": [\n" +
            "    { \"type\": \"direct\", \"tag\": \"direct\" },\n" +
            "    {\n" +
            "      \"type\": \"anytls\",\n" +
            "      \"tag\": \"anytls-json\",\n" +
            "      \"server\": \"anytls.example.com\",\n" +
            "      \"server_port\": 443,\n" +
            "      \"password\": \"anytls-secret\",\n" +
            "      \"tls\": { \"enabled\": true, \"server_name\": \"anytls-sni.example.com\" }\n" +
            "    },\n" +
            "    {\n" +
            "      \"type\": \"vless\",\n" +
            "      \"tag\": \"reality-json\",\n" +
            "      \"server\": \"reality.example.com\",\n" +
            "      \"server_port\": 443,\n" +
            "      \"uuid\": \"22222222-2222-2222-2222-222222222222\",\n" +
            "      \"flow\": \"xtls-rprx-vision\",\n" +
            "      \"packet_encoding\": \"xudp\",\n" +
            "      \"tls\": {\n" +
            "        \"enabled\": true,\n" +
            "        \"server_name\": \"www.example.com\",\n" +
            "        \"utls\": { \"fingerprint\": \"chrome\" },\n" +
            "        \"reality\": { \"enabled\": true, \"public_key\": \"pk\", \"short_id\": \"sid\" }\n" +
            "      }\n" +
            "    },\n" +
            "    {\n" +
            "      \"type\": \"hysteria2\",\n" +
            "      \"tag\": \"hy2-json\",\n" +
            "      \"server\": \"hy2.example.com\",\n" +
            "      \"server_port\": 443,\n" +
            "      \"password\": \"hy2-secret\",\n" +
            "      \"up_mbps\": 100,\n" +
            "      \"down_mbps\": 200,\n" +
            "      \"tls\": { \"enabled\": true, \"server_name\": \"hy2.example.com\" }\n" +
            "    },\n" +
            "    {\n" +
            "      \"type\": \"tuic\",\n" +
            "      \"tag\": \"tuic-json\",\n" +
            "      \"server\": \"tuic.example.com\",\n" +
            "      \"server_port\": 443,\n" +
            "      \"uuid\": \"33333333-3333-3333-3333-333333333333\",\n" +
            "      \"password\": \"tuic-secret\",\n" +
            "      \"congestion_control\": \"bbr\",\n" +
            "      \"udp_relay_mode\": \"native\",\n" +
            "      \"tls\": { \"enabled\": true, \"server_name\": \"tuic.example.com\" }\n" +
            "    }\n" +
            "  ]\n" +
            "}";

        var result = SubscriptionParserRegistry.Parse(json);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual(4, result.Servers.Count);

        var anyTls = (ModernProxyServer)result.Servers[0];
        Assert.AreEqual("anytls-json", anyTls.Remark);
        Assert.AreEqual("anytls", anyTls.Node.Protocol);
        Assert.AreEqual("anytls-secret", anyTls.Node.Password);
        Assert.AreEqual("anytls-sni.example.com", anyTls.Node.Tls.ServerName);

        var reality = (ModernProxyServer)result.Servers[1];
        Assert.AreEqual("reality-json", reality.Remark);
        Assert.AreEqual("vless", reality.Node.Protocol);
        Assert.IsTrue(reality.Node.Reality.Enabled);
        Assert.AreEqual("pk", reality.Node.Reality.PublicKey);

        var hysteria2 = (ModernProxyServer)result.Servers[2];
        Assert.AreEqual("hysteria2", hysteria2.Node.Protocol);
        Assert.AreEqual(100, hysteria2.Node.Hysteria2.UpMbps);

        var tuic = (ModernProxyServer)result.Servers[3];
        Assert.AreEqual("tuic", tuic.Node.Protocol);
        Assert.AreEqual("bbr", tuic.Node.Tuic.CongestionControl);
    }
}
