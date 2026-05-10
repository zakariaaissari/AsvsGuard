namespace ASVSGuard.Core.Entities;

public class Exigence
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public int Level { get; set; }
    public int? CWE { get; set; }
    public string Description { get; set; } = string.Empty;
    public ExigenceStatus Status { get; set; } = ExigenceStatus.Unknown;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
