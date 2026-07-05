using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Netch.Servers;
using Netch.Services;

namespace Tests;

[TestClass]
public class CoreAdapterResolverTests
{
    public static IEnumerable<object[]> LegacyServerCases
    {
        get
        {
            yield return new object[] { new Socks5Server() };
            yield return new object[] { new ShadowsocksServer() };
            yield return new object[] { new ShadowsocksRServer() };
            yield return new object[] { new TrojanServer() };
            yield return new object[] { new VMessServer() };
            yield return new object[] { new VLESSServer() };
            yield return new object[] { new WireGuardServer() };
            yield return new object[] { new SSHServer() };
        }
    }

    [DataTestMethod]
    [DynamicData(nameof(LegacyServerCases))]
    public void Resolve_ReturnsLegacyV2rayAdapter_ForExistingServerTypes(Server server)
    {
        var adapter = CoreAdapterResolver.Resolve(server);

        Assert.IsInstanceOfType(adapter, typeof(LegacyV2rayAdapter));
        Assert.IsTrue(adapter.CanStart(server));
    }

    [TestMethod]
    public void Resolve_ReturnsSingBoxAdapter_ForModernProxyServer()
    {
        var server = new ModernProxyServer
        {
            Node = new ProxyNode
            {
                Protocol = "vless"
            }
        };

        var adapter = CoreAdapterResolver.Resolve(server);

        Assert.IsInstanceOfType(adapter, typeof(SingBoxAdapter));
        Assert.IsTrue(adapter.CanStart(server));
    }
}
