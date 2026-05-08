using OWASPAsvs.Core.Entities;
using OWASPAsvs.Core.Interfaces;

namespace OWASPAsvs.Core.Services;

public class ChatService
{
    private readonly IChatRepository _repo;
    private readonly AIService _ai;

    public ChatService(IChatRepository repo, AIService ai)
    {
        _repo = repo;
        _ai = ai;
    }

    public async Task<(ChatSession session, IAsyncEnumerable<string> stream, string fullResponse)>
        SendAsync(string userId, string message, string? context)
    {
        var session = await _repo.GetOrCreateSessionAsync(userId, context);
        var history = await _repo.GetHistoryAsync(session.Id);

        await _repo.AddMessageAsync(new ChatMessage
        {
            SessionId = session.Id,
            Role = "user",
            Content = message
        });

        var stream = _ai.StreamChatAsync(session, history, message);

        return (session, stream, string.Empty);
    }

    public Task<List<ChatMessage>> GetHistoryAsync(int sessionId) =>
        _repo.GetHistoryAsync(sessionId);

    public Task SaveAssistantMessageAsync(int sessionId, string content) =>
        _repo.AddMessageAsync(new ChatMessage
        {
            SessionId = sessionId,
            Role = "assistant",
            Content = content
        });
}
