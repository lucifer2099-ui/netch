namespace Netch.Services;

public static class StartupDiagnosticService
{
    public static string Explain(Exception exception)
    {
        var message = exception.Message;
        if (message.Contains("forbidden by its access permissions", StringComparison.OrdinalIgnoreCase))
        {
            return message + "\n\n诊断：本地监听端口被 Windows 或安全/VPN 软件禁止绑定。\n建议：已启用自动端口 fallback；如果仍失败，请在设置中换一个 SOCKS5 本地端口，例如 2802、2810 或 29000。";
        }

        if (message.Contains("address already in use", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Only one usage of each socket address", StringComparison.OrdinalIgnoreCase))
        {
            return message + "\n\n诊断：本地监听端口已被其他程序占用。\n建议：关闭占用程序，或在设置中换一个 SOCKS5 本地端口。";
        }

        if (message.Contains("no application protocol", StringComparison.OrdinalIgnoreCase))
        {
            return message + "\n\n诊断：TLS ALPN 协商失败。\n建议：AnyTLS 节点通常需要 ALPN=h2；请在 Modern 节点编辑中检查 ALPN。";
        }

        if (message.Contains("unrecognized name", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("certificate", StringComparison.OrdinalIgnoreCase))
        {
            return message + "\n\n诊断：TLS/SNI 或证书校验失败。\n建议：检查 SNI、Allow Insecure、Reality/指纹参数是否与订阅一致。";
        }

        if (message.Contains("Missing required field", StringComparison.OrdinalIgnoreCase))
        {
            return message + "\n\n诊断：节点缺少必要字段。\n建议：打开 Modern 节点编辑，检查 UUID、Password、Reality Public Key、SNI 等字段。";
        }

        return message;
    }
}
