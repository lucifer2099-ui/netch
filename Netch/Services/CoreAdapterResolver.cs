using Netch.Interfaces;
using Netch.Models;
using Netch.Servers;

namespace Netch.Services;

public static class CoreAdapterResolver
{
    private static readonly Func<ICoreAdapter>[] AdapterFactories =
    {
        () => new SingBoxAdapter(),
        () => new LegacyV2rayAdapter()
    };

    public static ICoreAdapter Resolve(Server server)
    {
        foreach (var createAdapter in AdapterFactories)
        {
            var adapter = createAdapter();
            if (adapter.CanStart(server))
                return adapter;
        }

        throw new NotSupportedException($"No proxy core adapter supports server type \"{server.Type}\".");
    }
}
