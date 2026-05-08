using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OWASPAsvs.Core.Services;
using OWASPAsvs.Models;

namespace OWASPAsvs.Controllers;

public class HomeController : Controller
{
    private readonly ExigenceService _exigences;
    private readonly RepoScanService _scans;

    public HomeController(ExigenceService exigences, RepoScanService scans)
    {
        _exigences = exigences;
        _scans = scans;
    }

    public async Task<IActionResult> Index()
    {
        var all = (await _exigences.GetAllAsync()).ToList();

        var categories = all
            .GroupBy(e => e.Category)
            .Select(g => new CategoryStat(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var recentScans = new List<RecentScanItem>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var scans = await _scans.GetRecentByUserAsync(userId, 5);
            recentScans = scans.Select(s => new RecentScanItem(s.Id, s.RepoUrl, s.ScannedAt, s.Status)).ToList();
        }

        var vm = new DashboardViewModel(
            TotalExigences: all.Count,
            Level1Count: all.Count(e => e.Level == 1),
            Level2Count: all.Count(e => e.Level == 2),
            Level3Count: all.Count(e => e.Level == 3),
            CategoryBreakdown: categories,
            RecentScans: recentScans
        );

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
