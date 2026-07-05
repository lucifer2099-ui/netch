using System.Net;
using System.Net.Sockets;
using Netch.Models;
using Netch.Services;

namespace Netch.Utils;

public static class SubscriptionUtil
{
    private static readonly string[] SubscriptionUserAgents =
    {
        "Netch",
        "Clash.Meta/1.18.0",
        "ClashforWindows/0.20.39",
        "mihomo/1.18.0",
        "sing-box/1.13.0"
    };

    private static readonly object ServerLock = new();

    public static async Task<SubscriptionUpdateSummary> UpdateServersAsync(string? proxyServer = default)
    {
        var reports = await Task.WhenAll(Global.Settings.Subscription.Select(item => UpdateServerCoreAsync(item, proxyServer)));
        return new SubscriptionUpdateSummary { Reports = reports.ToList() };
    }

    private static async Task<SubscriptionUpdateReport> UpdateServerCoreAsync(Subscription item, string? proxyServer)
    {
        var report = new SubscriptionUpdateReport { Remark = item.Remark };
        try
        {
            if (!item.Enable)
            {
                report.Success = true;
                report.Warnings.Add("Subscription disabled.");
                return report;
            }

            List<Server> servers;
            IReadOnlyList<string> warnings = Array.Empty<string>();

            var (code, result) = await DownloadSubscriptionStringAsync(item, proxyServer);
            if (code == HttpStatusCode.OK)
            {
                var parseResult = SubscriptionParserRegistry.Parse(result);
                if (parseResult.Errors.Any())
                    throw new Exception(string.Join("\n", parseResult.Errors));

                if (!parseResult.HasServers)
                    throw new Exception(parseResult.Warnings.Any()
                        ? $"No supported servers imported.\n{string.Join("\n", parseResult.Warnings)}"
                        : "No servers imported.");

                servers = parseResult.Servers.ToList();
                warnings = parseResult.Warnings;
            }
            else
                throw new Exception($"{item.Remark} Response Status Code: {code}");

            foreach (var server in servers)
                server.Group = item.Remark;

            lock (ServerLock)
            {
                Global.Settings.Server.RemoveAll(server => server.Group.Equals(item.Remark));
                Global.Settings.Server.AddRange(servers);
            }

            Global.MainForm.NotifyTip(i18N.TranslateFormat("Update {1} server(s) from {0}", item.Remark, servers.Count));
            if (warnings.Any())
                Global.MainForm.NotifyTip($"{warnings.Count} unsupported node(s) skipped from {item.Remark}", info: false);

            report.Success = true;
            report.ImportedCount = servers.Count;
            report.Warnings = warnings.ToList();
            return report;
        }
        catch (Exception e)
        {
            Global.MainForm.NotifyTip($"{i18N.TranslateFormat("Update servers failed from {0}", item.Remark)}\n{e.Message}", info: false);
            Log.Warning(e, "Update servers failed");
            report.Success = false;
            report.Error = e.Message;
            return report;
        }
    }

    private static async Task<(HttpStatusCode code, string result)> DownloadSubscriptionStringAsync(Subscription item, string? proxyServer)
    {
        var proxies = new List<string?> { proxyServer };
        if (string.IsNullOrWhiteSpace(proxyServer) && IsLocalProxyReachable("127.0.0.1", 7890))
            proxies.Add("http://127.0.0.1:7890");

        var userAgents = BuildUserAgentCandidates(item.UserAgent);

        Exception? lastException = null;
        (HttpStatusCode code, string result)? lastResponse = null;

        foreach (var proxy in proxies.Distinct())
        {
            foreach (var userAgent in userAgents)
            {
                try
                {
                    var response = await DownloadSubscriptionStringCoreAsync(item, proxy, userAgent);
                    lastResponse = response;

                    if (response.code == HttpStatusCode.OK && LooksLikeSubscriptionContent(response.result))
                    {
                        if (!string.IsNullOrWhiteSpace(proxy))
                            Log.Information("Downloaded subscription {Remark} with proxy {Proxy}", item.Remark, proxy);

                        if (!string.IsNullOrWhiteSpace(userAgent))
                            Log.Information("Downloaded subscription {Remark} with User-Agent {UserAgent}", item.Remark, userAgent);

                        return response;
                    }
                }
                catch (Exception e)
                {
                    lastException = e;
                    Log.Warning(e, "Download subscription {Remark} failed with proxy {Proxy} and User-Agent {UserAgent}", item.Remark, proxy, userAgent);
                }
            }
        }

        if (lastResponse is { } invalidResponse)
        {
            if (invalidResponse.code == HttpStatusCode.OK)
                throw new Exception("Downloaded subscription response does not look like a supported subscription.");

            return invalidResponse;
        }

        throw lastException ?? new Exception("Download subscription failed.");
    }

    private static Task<(HttpStatusCode code, string result)> DownloadSubscriptionStringCoreAsync(Subscription item, string? proxyServer, string? userAgent)
    {
        var request = WebUtil.CreateRequest(item.Link, userAgent: userAgent);

        if (!string.IsNullOrEmpty(proxyServer))
            request.Proxy = new WebProxy(proxyServer);

        return WebUtil.DownloadStringAsync(request);
    }

    private static IReadOnlyList<string?> BuildUserAgentCandidates(string? configuredUserAgent)
    {
        var candidates = new List<string?>();

        if (!string.IsNullOrWhiteSpace(configuredUserAgent))
            candidates.Add(configuredUserAgent);

        candidates.AddRange(SubscriptionUserAgents);
        candidates.Add(null);

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool LooksLikeSubscriptionContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.TrimStart();
        if (trimmed.StartsWith("proxies:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("\"outbounds\"", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("://", StringComparison.Ordinal))
            return true;

        if (trimmed.Length < 32)
            return false;

        if (!trimmed.All(c => char.IsLetterOrDigit(c) || c is '+' or '/' or '=' or '\r' or '\n'))
            return false;

        try
        {
            var decoded = ShareLink.URLSafeBase64Decode(trimmed);
            return decoded.Contains("://", StringComparison.Ordinal) ||
                   decoded.StartsWith("proxies:", StringComparison.OrdinalIgnoreCase) ||
                   decoded.Contains("\"outbounds\"", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLocalProxyReachable(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connect = client.BeginConnect(host, port, null, null);
            if (!connect.AsyncWaitHandle.WaitOne(300))
                return false;

            client.EndConnect(connect);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
