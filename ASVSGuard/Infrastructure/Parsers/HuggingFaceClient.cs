using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ASVSGuard.Core.Interfaces;

namespace ASVSGuard.Infrastructure.Parsers;

public class HuggingFaceClient : IAIHttpClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _chatPath;

    public HuggingFaceClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        var groqKey = config["Groq:ApiKey"];
        if (!string.IsNullOrWhiteSpace(groqKey))
        {
            _apiKey   = groqKey;
            _chatPath = "/openai/v1/chat/completions";
        }
        else
        {
            _apiKey   = config["HuggingFace:ApiKey"] ?? string.Empty;
            var provider = config["HuggingFace:Provider"] ?? "together";
            _chatPath = $"/{provider}/v1/chat/completions";
        }
    }

    public async Task<string> CompleteAsync(string model, string systemPrompt, string userMessage, int maxTokens = 512)
    {
        var body = JsonSerializer.Serialize(new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  }
            },
            max_tokens = maxTokens
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _chatPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (TaskCanceledException)
        {
            return "Error: Request timed out — try again.";
        }

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                using var errDoc = JsonDocument.Parse(json);
                if (errDoc.RootElement.TryGetProperty("error", out var errMsg))
                    return $"Error: {errMsg.GetString()}";
            }
            catch { /* fall through */ }
            return $"Error: HTTP {(int)response.StatusCode} — {json[..Math.Min(300, json.Length)]}";
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            return $"Error parsing response: {ex.Message} — {json[..Math.Min(200, json.Length)]}";
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(string model, string systemPrompt, string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var result = await CompleteAsync(model, systemPrompt, userMessage);
        if (result.StartsWith("Error:"))
            throw new InvalidOperationException(result);
        foreach (var word in result.Split(' '))
        {
            yield return word + " ";
            await Task.Delay(20, ct);
        }
    }
}
