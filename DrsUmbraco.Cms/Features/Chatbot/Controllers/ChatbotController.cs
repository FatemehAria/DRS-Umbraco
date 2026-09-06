using DrsUmbraco.Cms.Features.Chatbot.Contracts;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Readiness;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Features.Chatbot.Controllers;

[ApiController]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly IChatbotMessageService _chatbotMessageService;
    private readonly IChatbotSemanticIndexReadiness _readiness;
    public ChatbotController(
        IChatbotMessageService chatbotMsgService,
        IChatbotSemanticIndexReadiness readiness)
    {
        _chatbotMessageService = chatbotMsgService;
        _readiness = readiness;
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

        if (!_readiness.IsReady)
        {
            Response.Headers[
                "Retry-After"] = "5";

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    error =
                        "Chatbot semantic index is not ready.",

                    retryAfterSeconds = 5
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