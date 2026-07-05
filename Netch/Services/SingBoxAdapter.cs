using System.Net;
using System.Net.Sockets;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers;

namespace Netch.Services;

public class SingBoxAdapter : ICoreAdapter
{
    private SingBoxController? _controller;

    public string Name => "sing-box";

    public string CoreId => "sing-box";

    public ushort? Socks5LocalPort { get; set; }

    public string? LocalAddress { get; set; }

    public IReadOnlyCollection<string> SupportedServerTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Modern"
    };

    public bool CanStart(Server server)
    {
        return server is ModernProxyServer;
    }

    public async Task<Socks5Server> StartAsync(Server s)
    {
        if (s is not ModernProxyServer server)
            throw new NotSupportedException($"sing-box adapter does not support server type \"{s.Type}\".");

        _controller = new SingBoxController(Socks5LocalPort, LocalAddress);
        return await _controller.StartAsync(server);
    }

    public async Task StopAsync()
    {
        if (_controller != null)
            await _controller.StopAsync();
    }
}

public class SingBoxController : Guard
{
    private readonly ushort? _socks5LocalPort;
    private readonly string? _localAddress;

    public SingBoxController(ushort? socks5LocalPort, string? localAddress) : base(ResolveCoreFile())
    {
        _socks5LocalPort = socks5LocalPort;
        _localAddress = localAddress;
    }

    protected override IEnumerable<string> StartedKeywords => new[] { "started" };

    protected override IEnumerable<string> FailedKeywords => new[] { "error", "fatal", "panic" };

    public override string Name => "sing-box";

    private static string ResolveCoreFile()
    {
        const string fileName = "sing-box.exe";
        if (!File.Exists(Path.GetFullPath($"bin\\{fileName}")))
        {
            throw new MessageException("bin\\sing-box.exe file not found. Run tools\\install-sing-box.ps1 and rebuild, or copy sing-box.exe to the runtime bin directory.");
        }

        return fileName;
    }

    public async Task<Socks5Server> StartAsync(ModernProxyServer server)
    {
        var listenAddress = _localAddress ?? Global.Settings.LocalAddress;
        var listenPort = ResolveListenPort(listenAddress, _socks5LocalPort ?? Global.Settings.Socks5LocalPort);
        var config = SingBoxConfigBuilder.BuildClientConfig(server, listenAddress, listenPort);

        Directory.CreateDirectory(Path.Combine(Global.NetchDir, "data"));
        await File.WriteAllTextAsync(Path.Combine(Global.NetchDir, Constants.SingBoxTempConfig), config);

        await StartGuardAsync($"run -c ..\\{Constants.SingBoxTempConfig}");
        return new Socks5Server(IPAddress.Loopback.ToString(), listenPort, server.Hostname);
    }

    private static ushort ResolveListenPort(string listenAddress, ushort preferredPort)
    {
        if (CanBind(listenAddress, preferredPort))
            return preferredPort;

        for (var port = preferredPort + 1; port <= Math.Min(ushort.MaxValue, preferredPort + 200); port++)
        {
            if (CanBind(listenAddress, (ushort)port))
            {
                Log.Warning("sing-box local port {PreferredPort} is unavailable, fallback to {Port}", preferredPort, port);
                return (ushort)port;
            }
        }

        throw new MessageException($"sing-box local port {preferredPort} is unavailable and no fallback port was found.");
    }

    private static bool CanBind(string listenAddress, ushort port)
    {
        TcpListener? listener = null;
        try
        {
            var address = IPAddress.TryParse(listenAddress, out var parsedAddress) ? parsedAddress : IPAddress.Loopback;
            listener = new TcpListener(address, port);
            listener.Start();
            return true;
        }
        catch (SocketException e)
        {
            Log.Warning(e, "sing-box cannot bind local port {Port}", port);
            return false;
        }
        finally
        {
            listener?.Stop();
        }
    }
}
