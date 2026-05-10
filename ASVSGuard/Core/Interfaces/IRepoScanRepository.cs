using ASVSGuard.Core.Entities;

namespace ASVSGuard.Core.Interfaces;

public interface IRepoScanRepository
{
    Task<RepoScan> CreateAsync(RepoScan scan);
    Task<RepoScan?> GetByIdAsync(int id);
    Task<List<RepoScan>> GetByUserAsync(string userId);
    Task UpdateAsync(RepoScan scan);
    Task AddFindingsAsync(IEnumerable<ScanFinding> findings);
}
