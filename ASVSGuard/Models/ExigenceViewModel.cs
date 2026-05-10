using ASVSGuard.Core.Entities;

namespace ASVSGuard.Models;

public record ExigenceViewModel(
    int Id,
    string Code,
    string Category,
    string Group,
    int Level,
    int? CWE,
    string Description,
    ExigenceStatus Status,
    string? Notes,
    DateTime UpdatedAt
);

public record ExigenceFilterViewModel(
    List<ExigenceViewModel> Items,
    int TotalCount,
    int Page,
    int PageSize,
    string? Category,
    int? Level,
    ExigenceStatus? Status,
    string? Search,
    List<string> Categories
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
};
