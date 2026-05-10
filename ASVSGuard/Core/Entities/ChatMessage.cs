namespace ASVSGuard.Core.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public ChatSession Session { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
