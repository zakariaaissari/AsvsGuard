using Microsoft.EntityFrameworkCore;
using ASVSGuard.Core.Entities;
using ASVSGuard.Core.Interfaces;

namespace ASVSGuard.Infrastructure.Data;

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _db;

    public ChatRepository(AppDbContext db) => _db = db;

    public async Task<ChatSession> GetOrCreateSessionAsync(string userId, string? context)
    {
        var session = await _db.ChatSessions
            .Where(s => s.UserId == userId && s.Context == context)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (session is null)
        {
            session = new ChatSession { UserId = userId, Context = context };
            _db.ChatSessions.Add(session);
            await _db.SaveChangesAsync();
        }

        return session;
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
    }

    public Task<List<ChatMessage>> GetHistoryAsync(int sessionId) =>
        _db.ChatMessages.Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
}
