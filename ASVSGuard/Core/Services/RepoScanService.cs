using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ASVSGuard.Core.Entities;
using ASVSGuard.Core.Interfaces;
using ASVSGuard.Infrastructure.Parsers;

namespace ASVSGuard.Core.Services;

public class RepoScanService
{
    private readonly GitHubMcpClient _github;
    private readonly IAIHttpClient _ai;
    private readonly IRepoScanRepository _repo;
    private readonly IExigenceRepository _exigenceRepo;
    private readonly string _modelScan;

    public RepoScanService(
        GitHubMcpClient github,
        IAIHttpClient ai,
        IRepoScanRepository repo,
        IExigenceRepository exigenceRepo,
        IConfiguration config)
    {
        _github = github;
        _ai = ai;
        _repo = repo;
        _exigenceRepo = exigenceRepo;
        _modelScan = config["Groq:ModelScan"]
                  ?? config["HuggingFace:ModelScan"]
                  ?? config["HuggingFace:ModelChat"]
                  ?? "llama-3.3-70b-versatile";
    }

    public Task<RepoScan?> GetScanByIdAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<List<RepoScan>> GetRecentByUserAsync(string userId, int count = 5)
    {
        var scans = await _repo.GetByUserAsync(userId);
        return scans.OrderByDescending(s => s.ScannedAt).Take(count).ToList();
    }

    public async Task<RepoScan> ScanAsync(string repoUrl, string userId, string? githubToken = null)
    {
        var (owner, repoName) = ParseRepoUrl(repoUrl);

        var scan = await _repo.CreateAsync(new RepoScan
        {
            UserId = userId,
            RepoUrl = repoUrl,
            Branch = "main",
            ScannedAt = DateTime.UtcNow,
            Status = RepoScanStatus.Running
        });

        try
        {
            var files = await _github.FetchRelevantFilesAsync(owner, repoName, githubToken);
            if (files.Count == 0)
                throw new InvalidOperationException("No source files found in the repository.");

            var allExigences = (await _exigenceRepo.GetAllAsync()).ToList();
            var findings = new List<ScanFinding>();
            var analysedIds = new HashSet<int>();

            const int batchSize = 30;
            for (int i = 0; i < allExigences.Count; i += batchSize)
            {
                var batch = allExigences.Skip(i).Take(batchSize).ToList();
                var batchFindings = await AnalyseBatchAsync(scan.Id, files, batch);
                findings.AddRange(batchFindings);
                foreach (var f in batchFindings) analysedIds.Add(f.ExigenceId);
            }

            // Anything not returned by the AI is considered Present
            foreach (var ex in allExigences.Where(e => !analysedIds.Contains(e.Id)))
            {
                findings.Add(new ScanFinding
                {
                    RepoScanId = scan.Id,
                    ExigenceId = ex.Id,
                    FindingStatus = FindingStatus.Present
                });
            }

            await _repo.AddFindingsAsync(findings);
            scan.Status = RepoScanStatus.Done;
            scan.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            scan.Status = RepoScanStatus.Failed;
            scan.ErrorMessage = ex.Message;
        }
        finally
        {
            await _repo.UpdateAsync(scan);
        }

        return (await _repo.GetByIdAsync(scan.Id)) ?? scan;
    }

    private async Task<List<ScanFinding>> AnalyseBatchAsync(
        int scanId,
        Dictionary<string, string> files,
        List<Exigence> batch)
    {
        // Build line-numbered file context so the AI can cite exact lines
        var fileContext = new StringBuilder();
        foreach (var (path, content) in files)
        {
            fileContext.AppendLine($"### FILE: {path}");
            var lines = content.Split('\n');
            var limit = Math.Min(lines.Length, 120);
            for (int n = 1; n <= limit; n++)
                fileContext.AppendLine($"{n,4}: {lines[n - 1]}");
            fileContext.AppendLine();
        }

        var exigenceList = string.Join("\n", batch.Select(e => $"- {e.Code}: {e.Description}"));

        const string system =
            "You are a security code reviewer. Output ONLY valid JSON, no prose, no markdown fences.";

        // $$"""...""" → double-$ raw string: {{expr}} = interpolation, { } = literal JSON braces
        var user = $$"""
            You are a security code reviewer. Analyse these source code files
            for security vulnerabilities and bad practices.

            Source code files:
            {{fileContext}}

            For each ASVS requirement below, do TWO things:

            1. Find CONCRETE evidence in the code above:
               - Exact file name where the issue is
               - Exact line number
               - The actual vulnerable/missing code snippet (copy it exactly)
               - Why it violates the requirement

            2. Give a specific fix suggestion for this codebase
               (not generic advice — reference the actual code)

            Requirements to check:
            {{exigenceList}}

            Return ONLY this JSON array, one entry per requirement:
            [
              {
                "code": "2.1.1",
                "status": "Missing",
                "filePath": "Controllers/AccountController.cs",
                "line": 87,
                "vulnerableCode": "if (password.Length < 6)",
                "issue": "Password minimum length is only 6 characters, ASVS requires 12",
                "suggestion": "Change to: if (password.Length < 12) or use FluentValidation with MinimumLength(12)"
              },
              {
                "code": "3.4.1",
                "status": "Present",
                "filePath": null,
                "line": null,
                "vulnerableCode": null,
                "issue": null,
                "suggestion": null
              }
            ]

            Rules:
            - If you find a real issue in the code: status = "Missing", fill ALL fields
            - If properly implemented: status = "Present", set filePath/line/vulnerableCode/issue/suggestion to null
            - If partially done: status = "Partial", explain what is missing
            - Copy the EXACT line of code from the files above into vulnerableCode
            - Never invent code that is not in the files above
            - Return an entry for EVERY requirement in the list
            """;

        var raw = await _ai.CompleteAsync(_modelScan, system, user, 7000);
        return ParseFindings(scanId, batch, raw);
    }

    private static List<ScanFinding> ParseFindings(int scanId, List<Exigence> batch, string raw)
    {
        var results = new List<ScanFinding>();

        var json = raw.Trim();
        // Strip markdown fences if model adds them
        if (json.StartsWith("```"))
        {
            var newline = json.IndexOf('\n');
            if (newline >= 0) json = json[(newline + 1)..];
        }
        if (json.EndsWith("```"))
            json = json[..json.LastIndexOf("```")].TrimEnd();

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        List<RepoFindingDto>? dtos = null;
        try
        {
            dtos = JsonSerializer.Deserialize<List<RepoFindingDto>>(json, opts);
        }
        catch (JsonException)
        {
            // Response was truncated — recover as many complete objects as possible
            var lastBrace = json.LastIndexOf('}');
            if (lastBrace >= 0)
            {
                var recovered = json[..(lastBrace + 1)] + "]";
                try { dtos = JsonSerializer.Deserialize<List<RepoFindingDto>>(recovered, opts); }
                catch { /* nothing recoverable */ }
            }
        }
        if (dtos is null) return results;

        foreach (var dto in dtos)
        {
            if (dto.Code is null) continue;
            var exigence = batch.FirstOrDefault(e => e.Code == dto.Code);
            if (exigence is null) continue;

            var status = dto.Status switch
            {
                "Present" => FindingStatus.Present,
                "Partial" => FindingStatus.Partial,
                _ => FindingStatus.Missing
            };

            results.Add(new ScanFinding
            {
                RepoScanId     = scanId,
                ExigenceId     = exigence.Id,
                FindingStatus  = status,
                FilePath       = dto.FilePath,
                Line           = dto.Line,
                VulnerableCode = dto.VulnerableCode,
                Issue          = dto.Issue,
                Suggestion     = dto.Suggestion
            });
        }

        return results;
    }

    private static (string owner, string repo) ParseRepoUrl(string repoUrl)
    {
        repoUrl = repoUrl.Trim().TrimEnd('/');
        if (repoUrl.StartsWith("https://github.com/"))
            repoUrl = repoUrl["https://github.com/".Length..];
        var parts = repoUrl.Split('/');
        if (parts.Length < 2)
            throw new ArgumentException("Invalid GitHub URL. Use: https://github.com/owner/repo");
        return (parts[0], parts[1]);
    }
}

// DTO for deserialising the AI response
file record RepoFindingDto(
    [property: JsonPropertyName("code")]           string? Code,
    [property: JsonPropertyName("status")]         string? Status,
    [property: JsonPropertyName("filePath")]       string? FilePath,
    [property: JsonPropertyName("line")]           int?    Line,
    [property: JsonPropertyName("vulnerableCode")] string? VulnerableCode,
    [property: JsonPropertyName("issue")]          string? Issue,
    [property: JsonPropertyName("suggestion")]     string? Suggestion
);
