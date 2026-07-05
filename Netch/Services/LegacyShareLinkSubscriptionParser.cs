using Netch.Interfaces;
using Netch.Models;
using Netch.Utils;

namespace Netch.Services;

public class LegacyShareLinkSubscriptionParser : ISubscriptionParser
{
    public string Name => "legacy-share-link";

    public bool CanParse(string text)
    {
        return !string.IsNullOrWhiteSpace(text);
    }

    public SubscriptionParseResult Parse(string text)
    {
        try
        {
            return new SubscriptionParseResult(ShareLink.ParseText(text));
        }
        catch (Exception e)
        {
            return new SubscriptionParseResult(Array.Empty<Server>(), new[] { e.Message });
        }
    }
}
