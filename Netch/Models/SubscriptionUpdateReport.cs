namespace Netch.Models;

public class SubscriptionUpdateReport
{
    public string Remark { get; set; } = string.Empty;

    public bool Success { get; set; }

    public int ImportedCount { get; set; }

    public int WarningCount => Warnings.Count;

    public List<string> Warnings { get; set; } = new();

    public string? Error { get; set; }
}

public class SubscriptionUpdateSummary
{
    public List<SubscriptionUpdateReport> Reports { get; set; } = new();

    public int ImportedCount => Reports.Sum(report => report.ImportedCount);

    public int WarningCount => Reports.Sum(report => report.WarningCount);

    public int FailedCount => Reports.Count(report => !report.Success);

    public string ToDisplayText()
    {
        var lines = new List<string>
        {
            $"Subscription update report",
            $"Imported: {ImportedCount}",
            $"Skipped/Warnings: {WarningCount}",
            $"Failed subscriptions: {FailedCount}"
        };

        foreach (var report in Reports)
        {
            lines.Add("");
            lines.Add(report.Success
                ? $"{report.Remark}: imported {report.ImportedCount}, warnings {report.WarningCount}"
                : $"{report.Remark}: failed");

            if (!string.IsNullOrWhiteSpace(report.Error))
                lines.Add(report.Error);

            lines.AddRange(report.Warnings.Take(8).Select(warning => "- " + warning));
            if (report.Warnings.Count > 8)
                lines.Add($"- ... {report.Warnings.Count - 8} more warning(s)");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
