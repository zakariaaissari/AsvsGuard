using Microsoft.EntityFrameworkCore;
using OWASPAsvs.Core.Entities;
using OWASPAsvs.Core.Interfaces;

namespace OWASPAsvs.Infrastructure.Data;

public class RepoScanRepository : IRepoScanRepository
{
    private readonly AppDbContext _db;

    public RepoScanRepository(AppDbContext db) => _db = db;

    public async Task<RepoScan> CreateAsync(RepoScan scan)
    {
        _db.RepoScans.Add(scan);
        await _db.SaveChangesAsync();
        return scan;
    }

    public Task<RepoScan?> GetByIdAsync(int id) =>
        _db.RepoScans.Include(s => s.Findings).ThenInclude(f => f.Exigence)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<RepoScan>> GetByUserAsync(string userId) =>
        _db.RepoScans.Where(s => s.UserId == userId).OrderByDescending(s => s.ScannedAt).ToListAsync();

    public async Task UpdateAsync(RepoScan scan)
    {
        _db.RepoScans.Update(scan);
        await _db.SaveChangesAsync();
    }

    public async Task AddFindingsAsync(IEnumerable<ScanFinding> findings)
    {
        await _db.ScanFindings.AddRangeAsync(findings);
        await _db.SaveChangesAsync();
    }
}
