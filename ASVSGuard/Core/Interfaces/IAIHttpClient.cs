namespace ASVSGuard.Core.Interfaces;

public interface IAIHttpClient
{
    Task<string> CompleteAsync(string model, string systemPrompt, string userMessage, int maxTokens = 512);
    IAsyncEnumerable<string> StreamAsync(string model, string systemPrompt, string userMessage, CancellationToken ct = default);
}
