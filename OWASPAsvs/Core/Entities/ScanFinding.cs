namespace OWASPAsvs.Core.Entities;

public class ScanFinding
{
    public int Id { get; set; }
    public int RepoScanId { get; set; }
    public RepoScan RepoScan { get; set; } = null!;
    public int ExigenceId { get; set; }
    public Exigence Exigence { get; set; } = null!;
    public FindingStatus FindingStatus { get; set; }
    public string? FilePath { get; set; }
    public int? Line { get; set; }
    public string? VulnerableCode { get; set; }
    public string? Issue { get; set; }
    public string? Suggestion { get; set; }
}
