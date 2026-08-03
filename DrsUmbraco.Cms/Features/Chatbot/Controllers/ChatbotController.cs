using DrsUmbraco.Cms.Features.Chatbot.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Features.Chatbot.Controllers;

[ApiController]
[Route("api/chatbot/messages")]
public sealed class ChatbotController : ControllerBase
{
    [HttpPost]
    public ActionResult<SendMessageResponse> Send(
        SendMessageRequest request)
    {
        // اعتبارسنجی پیام
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required." });
        }
        // ساخت پاسخ ثابت
        SendMessageResponse response = new()
        {
            Reply = "پیام شما دریافت شد."
        };
        // برگرداندن پاسخ
        return Ok(response);
    }
}