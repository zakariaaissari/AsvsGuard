using ASVSGuard.Core.Entities;

namespace ASVSGuard.Core.Interfaces;

public interface IChatRepository
{
    Task<ChatSession> GetOrCreateSessionAsync(string userId, string? context);
    Task AddMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetHistoryAsync(int sessionId);
}
