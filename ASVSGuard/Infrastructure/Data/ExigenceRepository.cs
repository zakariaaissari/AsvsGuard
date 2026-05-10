using Microsoft.EntityFrameworkCore;
using ASVSGuard.Core.Entities;
using ASVSGuard.Core.Interfaces;

namespace ASVSGuard.Infrastructure.Data;

public class ExigenceRepository : IExigenceRepository
{
    private readonly AppDbContext _db;

    public ExigenceRepository(AppDbContext db) => _db = db;

    public Task<List<Exigence>> GetAllAsync() =>
        _db.Exigences.OrderBy(e => e.Code).ToListAsync();

    public Task<Exigence?> GetByIdAsync(int id) =>
        _db.Exigences.FindAsync(id).AsTask();

    public async Task AddRangeAsync(IEnumerable<Exigence> exigences)
    {
        await _db.Exigences.AddRangeAsync(exigences);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Exigence exigence)
    {
        _db.Exigences.Update(exigence);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Exigence>> FilterAsync(string? category, int? level, ExigenceStatus? status, string? search, int page, int pageSize)
    {
        var query = BuildQuery(category, level, status, search);
        return await query
            .OrderBy(e => e.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(string? category, int? level, ExigenceStatus? status, string? search) =>
        await BuildQuery(category, level, status, search).CountAsync();

    public async Task<List<string>> GetCategoriesAsync() =>
        await _db.Exigences.Select(e => e.Category).Distinct().OrderBy(c => c).ToListAsync();

    private IQueryable<Exigence> BuildQuery(string? category, int? level, ExigenceStatus? status, string? search)
    {
        var q = _db.Exigences.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(e => e.Category == category);
        if (level.HasValue) q = q.Where(e => e.Level == level.Value);
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.Description.Contains(search) || e.Code.Contains(search));
        return q;
    }
}
