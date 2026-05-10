using ASVSGuard.Core.Entities;

namespace ASVSGuard.Models;

public record DashboardViewModel(
    int TotalExigences,
    int Level1Count,
    int Level2Count,
    int Level3Count,
    List<CategoryStat> CategoryBreakdown,
    List<RecentScanItem> RecentScans
);

public record CategoryStat(string Category, int Count);

public record RecentScanItem(
    int Id,
    string RepoUrl,
    DateTime ScannedAt,
    RepoScanStatus Status
)
{
    public string RepoName => RepoUrl.TrimEnd('/').Split('/').LastOrDefault() ?? RepoUrl;
}
