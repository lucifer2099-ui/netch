using Netch.Models;
using Netch.Servers;

namespace Netch.Interfaces;

public interface ICoreAdapter : IServerController
{
    string CoreId { get; }

    IReadOnlyCollection<string> SupportedServerTypes { get; }

    bool CanStart(Server server);
}
