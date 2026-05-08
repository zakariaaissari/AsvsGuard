using OWASPAsvs.Core.Entities;

namespace OWASPAsvs.Core.Interfaces;

public interface IExigenceRepository
{
    Task<List<Exigence>> GetAllAsync();
    Task<Exigence?> GetByIdAsync(int id);
    Task<Exigence?> GetByCodeAsync(string code);
    Task AddRangeAsync(IEnumerable<Exigence> exigences);
    Task UpdateAsync(Exigence exigence);
    Task<List<Exigence>> FilterAsync(string? category, int? level, ExigenceStatus? status, string? search, int page, int pageSize);
    Task<int> CountAsync(string? category, int? level, ExigenceStatus? status, string? search);
    Task<List<string>> GetCategoriesAsync();
}
