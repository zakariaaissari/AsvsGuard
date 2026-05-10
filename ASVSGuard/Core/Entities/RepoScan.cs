namespace ASVSGuard.Core.Entities;

public class RepoScan
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string RepoUrl { get; set; } = string.Empty;
    public string Branch { get; set; } = "main";
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public RepoScanStatus Status { get; set; } = RepoScanStatus.Pending;

    public string? ErrorMessage { get; set; }

    public List<ScanFinding> Findings { get; set; } = new();
}
