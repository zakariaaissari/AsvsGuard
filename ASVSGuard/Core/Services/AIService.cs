using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ASVSGuard.Core.Entities;
using ASVSGuard.Core.Interfaces;

namespace ASVSGuard.Core.Services;

public class AIService
{
    private readonly IAIHttpClient _client;
    private readonly string _modelChat;
    private readonly string _modelCode;

    public AIService(IAIHttpClient client, IConfiguration config)
    {
        _client = client;
        _modelChat = config["Groq:ModelChat"]
                  ?? config["HuggingFace:ModelChat"]
                  ?? "llama-3.3-70b-versatile";
        _modelCode = config["Groq:ModelCode"]
                  ?? config["HuggingFace:ModelCode"]
                  ?? "llama-3.3-70b-versatile";
    }

    public Task<string> ExplainExigenceAsync(Exigence e) =>
        _client.CompleteAsync(
            _modelChat,
            "You are an OWASP ASVS security expert. Be concise and practical.",
            $"Explain ASVS requirement {e.Code}: \"{e.Description}\" (CWE-{e.CWE}, Level {e.Level}). " +
            "What does it mean for a developer? What are the risks if not implemented? Answer in 3-4 sentences.",
            512);

    public Task<string> GenerateCodeAsync(Exigence e, string language) =>
        _client.CompleteAsync(
            _modelCode,
            $"You are a senior {language} developer. Return only code, no prose.",
            $"Implement ASVS requirement {e.Code}: \"{e.Description}\" Language: {language}. " +
            "Return a complete, runnable code example with inline comments.",
            800);

    public IAsyncEnumerable<string> StreamChatAsync(ChatSession session, List<ChatMessage> history, string userMessage)
    {
        var contextBlock = BuildContextBlock(session.Context);

        var historyText = string.Join("\n", history.TakeLast(10)
            .Select(m => $"[{m.Role}]: {m.Content}"));

        var system = "You are a security assistant helping developers implement OWASP ASVS 4.0 requirements. " +
                     contextBlock +
                     "Be concise and practical. When explaining a Missing or Partial finding always cover: " +
                     "1) what the requirement means, 2) the security risk if ignored, 3) a concrete code fix.";

        var user = string.IsNullOrEmpty(historyText)
            ? userMessage
            : $"Conversation so far:\n{historyText}\n\nUser: {userMessage}";

        return _client.StreamAsync(_modelChat, system, user);
    }

    private static string BuildContextBlock(string? context)
    {
        if (context is null) return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(context);
            var root = doc.RootElement;

            var code        = root.TryGetProperty("code",        out var c)  ? c.GetString()  : null;
            var description = root.TryGetProperty("description", out var d)  ? d.GetString()  : null;
            var status      = root.TryGetProperty("status",      out var st) ? st.GetString() : null;
            var filePath    = root.TryGetProperty("filePath",    out var fp) && fp.ValueKind != JsonValueKind.Null ? fp.GetString() : null;
            var line        = root.TryGetProperty("line",        out var l)  && l.ValueKind  != JsonValueKind.Null ? l.GetRawText() : null;
            var suggestion  = root.TryGetProperty("suggestion",  out var s)  && s.ValueKind  != JsonValueKind.Null ? s.GetString()  : null;

            var sb = new StringBuilder();
            sb.Append($"The developer is reviewing ASVS requirement {code}: \"{description}\" (status: {status}). ");
            if (filePath is not null)
                sb.Append($"Issue found in \"{filePath}\"" + (line is not null ? $" at line {line}" : "") + ". ");
            if (suggestion is not null)
                sb.Append($"Scanner note: {suggestion} ");
            return sb.ToString();
        }
        catch
        {
            return $"Current context: exigence {context}. ";
        }
    }
}
