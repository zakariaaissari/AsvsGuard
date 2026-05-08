using Microsoft.AspNetCore.Mvc;
using OWASPAsvs.Core.Entities;
using OWASPAsvs.Core.Services;
using OWASPAsvs.Models;

namespace OWASPAsvs.Controllers;

public class ExigenceController : Controller
{
    private readonly ExigenceService _service;

    public ExigenceController(ExigenceService service) => _service = service;

    public async Task<IActionResult> Index(
        string? category, int? level, ExigenceStatus? status, string? search, int page = 1)
    {
        const int pageSize = 300;

        var items = await _service.FilterAsync(category, level, status, search, page, pageSize);
        var total = await _service.CountAsync(category, level, status, search);
        var categories = await _service.GetCategoriesAsync();

        var vms = items.Select(e => new ExigenceViewModel(
            e.Id, e.Code, e.Category, e.Group, e.Level, e.CWE,
            e.Description, e.Status, e.Notes, e.UpdatedAt)).ToList();

        var model = new ExigenceFilterViewModel(
            vms, total, page, pageSize, category, level, status, search, categories);

        return View(model);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var e = await _service.GetByIdAsync(id);
        if (e is null) return NotFound();

        var vm = new ExigenceViewModel(
            e.Id, e.Code, e.Category, e.Group, e.Level, e.CWE,
            e.Description, e.Status, e.Notes, e.UpdatedAt);

        return View(vm);
    }

    [HttpGet]
    public IActionResult Import() => View();

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select an Excel file.");
            return View();
        }

        await using var stream = file.OpenReadStream();
        var count = await _service.ImportFromExcelAsync(stream);
        TempData["Success"] = $"Imported {count} new exigences.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, ExigenceStatus status, string? notes)
    {
        await _service.UpdateStatusAsync(id, status, notes);
        return RedirectToAction(nameof(Detail), new { id });
    }
}
