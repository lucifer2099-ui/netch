using Netch.Interfaces;
using Netch.Models;

namespace Netch.Services;

public static class SubscriptionParserRegistry
{
    private static readonly ISubscriptionParser[] Parsers =
    {
        new SingBoxJsonSubscriptionParser(),
        new ClashYamlSubscriptionParser(),
        new LegacyShareLinkSubscriptionParser()
    };

    public static SubscriptionParseResult Parse(string text)
    {
        var errors = new List<string>();

        foreach (var parser in Parsers.Where(parser => parser.CanParse(text)))
        {
            var result = parser.Parse(text);
            if (result.HasServers || result.Warnings.Any())
                return result;

            errors.AddRange(result.Errors.Select(error => $"{parser.Name}: {error}"));
        }

        return errors.Count == 0
            ? SubscriptionParseResult.Empty
            : new SubscriptionParseResult(Array.Empty<Server>(), errors);
    }
}
