using Netch.Models;

namespace Netch.Interfaces;

public interface ISubscriptionParser
{
    string Name { get; }

    bool CanParse(string text);

    SubscriptionParseResult Parse(string text);
}
