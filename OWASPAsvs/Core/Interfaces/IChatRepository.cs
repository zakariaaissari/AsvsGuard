using OWASPAsvs.Core.Entities;

namespace OWASPAsvs.Core.Interfaces;

public interface IChatRepository
{
    Task<ChatSession> GetOrCreateSessionAsync(string userId, string? context);
    Task AddMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetHistoryAsync(int sessionId);
}
