using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Netch.Services;

namespace Tests;

[TestClass]
public class ModernUsabilityTests
{
    [TestMethod]
    public void StartupDiagnosticService_ExplainsBlockedLocalPort()
    {
        var text = StartupDiagnosticService.Explain(new Exception("listen tcp 127.0.0.1:2801: bind: An attempt was made to access a socket in a way forbidden by its access permissions."));

        StringAssert.Contains(text, "本地监听端口");
        StringAssert.Contains(text, "SOCKS5");
    }

    [TestMethod]
    public void StartupDiagnosticService_ExplainsAnyTlsAlpnFailure()
    {
        var text = StartupDiagnosticService.Explain(new Exception("remote error: tls: no application protocol"));

        StringAssert.Contains(text, "ALPN");
        StringAssert.Contains(text, "h2");
    }

    [TestMethod]
    public void SubscriptionUpdateSummary_BuildsReadableReport()
    {
        var summary = new SubscriptionUpdateSummary
        {
            Reports =
            {
                new SubscriptionUpdateReport
                {
                    Remark = "sub-a",
                    Success = true,
                    ImportedCount = 3,
                    Warnings = { "unsupported node" }
                },
                new SubscriptionUpdateReport
                {
                    Remark = "sub-b",
                    Success = false,
                    Error = "download failed"
                }
            }
        };

        Assert.AreEqual(3, summary.ImportedCount);
        Assert.AreEqual(1, summary.WarningCount);
        Assert.AreEqual(1, summary.FailedCount);

        var text = summary.ToDisplayText();
        StringAssert.Contains(text, "Imported: 3");
        StringAssert.Contains(text, "sub-b: failed");
    }
}
