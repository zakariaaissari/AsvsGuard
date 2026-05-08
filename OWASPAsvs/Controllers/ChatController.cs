using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OWASPAsvs.Core.Services;

namespace OWASPAsvs.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ChatService _chat;

    public ChatController(ChatService chat) => _chat = chat;

    [HttpPost]
    public async Task SendAsync([FromBody] ChatSendRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (session, stream, _) = await _chat.SendAsync(userId, req.Message, req.Context);

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var sb = new StringBuilder();
        await foreach (var token in stream)
        {
            sb.Append(token);
            var data = $"data: {token.Replace("\n", "\\n")}\n\n";
            await Response.WriteAsync(data);
            await Response.Body.FlushAsync();
        }

        await _chat.SaveAssistantMessageAsync(session.Id, sb.ToString());
        await Response.WriteAsync("data: [DONE]\n\n");
    }

    [HttpGet]
    public async Task<IActionResult> History(int sessionId)
    {
        var messages = await _chat.GetHistoryAsync(sessionId);
        return Json(messages.Select(m => new { m.Role, m.Content, m.CreatedAt }));
    }
}

public record ChatSendRequest(string Message, string? Context);
