using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OWASPAsvs.Core.Services;

namespace OWASPAsvs.Controllers;

[Authorize]
public class AIController : Controller
{
    private readonly AIService _ai;
    private readonly ExigenceService _exigences;

    public AIController(AIService ai, ExigenceService exigences)
    {
        _ai = ai;
        _exigences = exigences;
    }

    [HttpPost]
    public async Task<IActionResult> Explain([FromBody] ExigenceRequest req)
    {
        var e = await _exigences.GetByIdAsync(req.ExigenceId);
        if (e is null) return NotFound(new { error = "Exigence not found" });
        try
        {
            var explanation = await _ai.ExplainExigenceAsync(e);
            return Json(new { explanation });
        }
        catch (Exception ex)
        {
            return Json(new { explanation = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateCode([FromBody] CodeRequest req)
    {
        var e = await _exigences.GetByIdAsync(req.ExigenceId);
        if (e is null) return NotFound(new { error = "Exigence not found" });
        try
        {
            var code = await _ai.GenerateCodeAsync(e, req.Language);
            return Json(new { code });
        }
        catch (Exception ex)
        {
            return Json(new { code = $"Error: {ex.Message}" });
        }
    }
}

public record ExigenceRequest(int ExigenceId);
public record CodeRequest(int ExigenceId, string Language);
