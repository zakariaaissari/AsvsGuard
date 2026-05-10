namespace ASVSGuard.Core.Entities;

public class ChatSession
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Context { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ChatMessage> Messages { get; set; } = new();
}
