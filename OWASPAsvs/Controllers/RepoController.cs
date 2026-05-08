using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OWASPAsvs.Core.Services;
using OWASPAsvs.Models;

namespace OWASPAsvs.Controllers;

public class RepoController : Controller
{
    private readonly RepoScanService _scanner;

    public RepoController(RepoScanService scanner) => _scanner = scanner;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Scan(string repoUrl, string? githubToken)
    {
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            ModelState.AddModelError(string.Empty, "Please enter a GitHub repository URL.");
            return View("Index");
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var scan = await _scanner.ScanAsync(repoUrl, userId, githubToken);
            return RedirectToAction(nameof(Result), new { id = scan.Id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Scan failed: {ex.Message}");
            return View("Index");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var scan = await _scanner.GetScanByIdAsync(id);
        if (scan is null) return NotFound();

        var byCategory = scan.Findings
            .GroupBy(f => f.Exigence.Category)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => new ScanFindingItem(
                    f.Exigence.Code,
                    f.Exigence.Description,
                    f.FindingStatus,
                    f.FilePath,
                    f.Line,
                    f.VulnerableCode,
                    f.Issue,
                    f.Suggestion)).ToList());

        var vm = new ScanResultViewModel(
            scan.Id,
            scan.RepoUrl,
            scan.Branch,
            scan.ScannedAt,
            scan.Findings.Count,
            scan.Findings.Count(f => f.FindingStatus == OWASPAsvs.Core.Entities.FindingStatus.Present),
            scan.Findings.Count(f => f.FindingStatus == OWASPAsvs.Core.Entities.FindingStatus.Partial),
            scan.Findings.Count(f => f.FindingStatus == OWASPAsvs.Core.Entities.FindingStatus.Missing),
            byCategory,
            scan.Status,
            scan.ErrorMessage);

        return View(vm);
    }
}
