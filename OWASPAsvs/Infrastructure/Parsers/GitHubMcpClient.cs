using System.Net.Http.Headers;
using System.Text.Json;

namespace OWASPAsvs.Infrastructure.Parsers;

public class GitHubMcpClient
{
    private readonly HttpClient _http;

    private static readonly string[] PriorityKeywords =
    [
        "auth", "login", "password", "token", "secret", "crypto", "encrypt",
        "jwt", "session", "sql", "query", "inject", "sanitize", "validate",
        "config", "startup", "program", "middleware", "security", "permission",
        "role", "claim", "hash", "salt", "cert", "tls", "ssl", "cors", "csrf",
        "xss", "header", "cookie", "log", "audit", "rate", "limit"
    ];

    private static readonly string[] AllowedExtensions =
    [
        ".cs", ".java", ".py", ".js", ".ts", ".go", ".php",
        ".env", ".json", ".xml", ".yaml", ".yml", ".config", ".ini"
    ];

    public GitHubMcpClient(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("OWASPAsvs/1.0");
    }

    public async Task<Dictionary<string, string>> FetchRelevantFilesAsync(
        string owner, string repo, string? token = null)
    {
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        string? treeJson = null;
        foreach (var branch in new[] { "main", "master" })
        {
            var treeUrl = $"https://api.github.com/repos/{owner}/{repo}/git/trees/{branch}?recursive=1";
            var resp = await _http.GetAsync(treeUrl);
            if (resp.IsSuccessStatusCode)
            {
                treeJson = await resp.Content.ReadAsStringAsync();
                break;
            }
        }

        if (treeJson is null)
            throw new InvalidOperationException("Could not fetch repository tree. Check the URL and token.");

        using var doc = JsonDocument.Parse(treeJson);
        var tree = doc.RootElement.GetProperty("tree");

        var candidates = tree.EnumerateArray()
            .Where(n => n.GetProperty("type").GetString() == "blob")
            .Select(n => n.GetProperty("path").GetString()!)
            .Where(p => AllowedExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .Where(p => !p.Contains("node_modules/") && !p.Contains("vendor/")
                     && !p.Contains(".min.") && !p.Contains("dist/")
                     && !p.Contains("bin/") && !p.Contains("obj/")
                     && !p.Contains("test") && !p.Contains("spec")
                     && !p.Contains("migration") && !p.Contains("Migration"))
            .ToList();

        var scored = candidates
            .Select(p => (path: p, score: Score(p)))
            .OrderByDescending(x => x.score)
            .Take(15)
            .Select(x => x.path)
            .ToList();

        var result = new Dictionary<string, string>();
        foreach (var path in scored)
        {
            var contentUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
            var resp = await _http.GetAsync(contentUrl);
            if (!resp.IsSuccessStatusCode) continue;

            var json = await resp.Content.ReadAsStringAsync();
            using var d = JsonDocument.Parse(json);
            if (!d.RootElement.TryGetProperty("content", out var contentProp)) continue;

            var base64 = contentProp.GetString()?.Replace("\n", "") ?? string.Empty;
            if (string.IsNullOrEmpty(base64)) continue;

            try
            {
                var bytes = Convert.FromBase64String(base64);
                result[path] = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch { /* skip binary or malformed */ }
        }

        return result;
    }

    private static int Score(string path)
    {
        var lower = path.ToLowerInvariant();
        return PriorityKeywords.Count(k => lower.Contains(k));
    }
}
