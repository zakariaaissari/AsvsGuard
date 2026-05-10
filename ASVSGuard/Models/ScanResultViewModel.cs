using ASVSGuard.Core.Entities;

namespace ASVSGuard.Models;

public record ScanFindingItem(
    string Code,
    string Description,
    FindingStatus Status,
    string? FilePath,
    int? Line,
    string? VulnerableCode,
    string? Issue,
    string? Suggestion
);

public record ScanResultViewModel(
    int Id,
    string RepoUrl,
    string Branch,
    DateTime ScannedAt,
    int TotalExigences,
    int PresentCount,
    int PartialCount,
    int MissingCount,
    Dictionary<string, List<ScanFindingItem>> FindingsByCategory,
    RepoScanStatus Status = RepoScanStatus.Done,
    string? ErrorMessage = null
)
{
    public double CompliancePercent =>
        TotalExigences == 0 ? 0 : Math.Round((PresentCount + PartialCount * 0.5) / TotalExigences * 100, 1);
}
