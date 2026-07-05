using Netch.Forms;
using Netch.Interfaces;
using Netch.Models;
using Netch.Services;

namespace Netch.Servers;

public class ModernProxyUtil : IServerUtil
{
    public ushort Priority { get; } = 10;

    public string TypeName { get; } = "Modern";

    public string FullName { get; } = "Modern Proxy";

    public string ShortName { get; } = "NX";

    public string[] UriScheme { get; } = Array.Empty<string>();

    public Type ServerType { get; } = typeof(ModernProxyServer);

    public void Edit(Server s)
    {
        new ModernProxyForm((ModernProxyServer)s).ShowDialog();
    }

    public void Create()
    {
        new ModernProxyForm().ShowDialog();
    }

    public string GetShareLink(Server s)
    {
        throw new NotSupportedException("Modern proxy nodes do not have a Netch share link yet.");
    }

    public IServerController GetController()
    {
        return new SingBoxAdapter();
    }

    public IEnumerable<Server> ParseUri(string text)
    {
        return Array.Empty<Server>();
    }

    public bool CheckServer(Server s)
    {
        return s is ModernProxyServer modern && !string.IsNullOrWhiteSpace(modern.Node.Protocol);
    }
}
