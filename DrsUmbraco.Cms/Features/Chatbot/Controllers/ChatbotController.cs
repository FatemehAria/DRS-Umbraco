using DrsUmbraco.Cms.Features.Chatbot.Contracts;
using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Features.Chatbot.Controllers;

[ApiController]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly IChatbotMessageService _chatbotMessageService;
    public ChatbotController(
        IChatbotMessageService chatbotMsgService)
    {
        _chatbotMessageService = chatbotMsgService;
    }

    [HttpPost("messages")]
    public ActionResult<SendMessageResponse> Send(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                error = "Message is required."
            });
        }

        ChatbotMessageResult chatbotMessageResult = _chatbotMessageService.Process(request.Message);

        return Ok(new SendMessageResponse
        {
            ResponseType = chatbotMessageResult.ResponseType,
            Reply = chatbotMessageResult.Reply,
            Suggestions = chatbotMessageResult.Suggestions
        });

    }

    [HttpPost("suggestions/select")]
    public ActionResult<SendMessageResponse>
    SelectSuggestion(
        [FromBody] SelectSuggestionRequest request)
    {
        ChatbotMessageResult? result =
            _chatbotMessageService.SelectSuggestion(
                request.KnowledgeItemId);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(
            new SendMessageResponse
            {
                ResponseType =
                    result.ResponseType,

                Reply =
                    result.Reply,

                Suggestions =
                    result.Suggestions
            });
    }

}