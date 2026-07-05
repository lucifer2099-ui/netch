namespace Netch.Models;

public class SubscriptionParseResult
{
    public SubscriptionParseResult(IEnumerable<Server> servers, IEnumerable<string>? errors = null, IEnumerable<string>? warnings = null)
    {
        Servers = servers.ToList();
        Errors = errors?.ToList() ?? new List<string>();
        Warnings = warnings?.ToList() ?? new List<string>();
    }

    public IReadOnlyList<Server> Servers { get; }

    public IReadOnlyList<string> Errors { get; }

    public IReadOnlyList<string> Warnings { get; }

    public bool HasServers => Servers.Count > 0;

    public static SubscriptionParseResult Empty { get; } = new(Array.Empty<Server>());
}
