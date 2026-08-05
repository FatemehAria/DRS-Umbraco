using DrsUmbraco.Cms.Features.Chatbot.Contracts;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Features.Chatbot.Controllers;

[ApiController]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IPersianTextNormalizer _textNormalizer;
    public ChatbotController(
        IChatbotKnowledgeService knowledgeService,
        IPersianTextNormalizer textNormalizer)
    {
        _knowledgeService = knowledgeService;
        _textNormalizer = textNormalizer;
    }

    [HttpPost("messages")]
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

    [HttpGet("knowledge")]
    public ActionResult<IReadOnlyList<ChatbotKnowledgeItem>> GetKnowledge()
    {
        IReadOnlyList<ChatbotKnowledgeItem> items = _knowledgeService.GetAll();

        return Ok(items);
    }

    [HttpPost("normalize")]
    public ActionResult Normalize(
    [FromBody] SendMessageRequest request)
    {
        // ورودی خالی را بررسی کن
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required." });
        }
        // _textNormalizer.Normalize را صدا بزن
        string normalized = _textNormalizer.Normalize(request.Message);
        // original و normalized را برگردان
        return Ok(new { original = request.Message, normalized = normalized });
    }
}