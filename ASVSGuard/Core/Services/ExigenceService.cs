using ASVSGuard.Core.Entities;
using ASVSGuard.Core.Interfaces;

namespace ASVSGuard.Core.Services;

public class ExigenceService
{
    private readonly IExigenceRepository _repo;
    private readonly IExcelParser _parser;

    public ExigenceService(IExigenceRepository repo, IExcelParser parser)
    {
        _repo = repo;
        _parser = parser;
    }

    public async Task<int> ImportFromExcelAsync(Stream stream)
    {
        var parsed = _parser.Parse(stream).ToList();
        var existing = (await _repo.GetAllAsync()).Select(e => e.Code).ToHashSet();
        var newOnes = parsed.Where(e => !existing.Contains(e.Code)).ToList();
        if (newOnes.Count > 0)
            await _repo.AddRangeAsync(newOnes);
        return newOnes.Count;
    }

    public Task<List<Exigence>> GetAllAsync() => _repo.GetAllAsync();

    public Task<Exigence?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public Task<List<Exigence>> FilterAsync(string? category, int? level, ExigenceStatus? status, string? search, int page = 1, int pageSize = 20) =>
        _repo.FilterAsync(category, level, status, search, page, pageSize);

    public Task<int> CountAsync(string? category, int? level, ExigenceStatus? status, string? search) =>
        _repo.CountAsync(category, level, status, search);

    public Task<List<string>> GetCategoriesAsync() => _repo.GetCategoriesAsync();

    public async Task UpdateStatusAsync(int id, ExigenceStatus status, string? notes)
    {
        var exigence = await _repo.GetByIdAsync(id)
                       ?? throw new InvalidOperationException($"Exigence {id} not found.");
        exigence.Status = status;
        exigence.Notes = notes;
        exigence.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(exigence);
    }
}
