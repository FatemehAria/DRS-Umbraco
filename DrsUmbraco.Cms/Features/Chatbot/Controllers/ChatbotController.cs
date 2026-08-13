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
    private readonly IChatbotMatchingService _matchingService;
    private readonly IChatbotSemanticSearchService _semanticSearchService;
    private readonly IChatbotSemanticDecisionService _semanticDecisionService;
    public ChatbotController(
        IChatbotMatchingService matchingService,
        IChatbotSemanticSearchService semanticSearchService,
        IChatbotSemanticDecisionService semanticDecisionService)
    {
        _matchingService = matchingService;
        _semanticSearchService = semanticSearchService;
        _semanticDecisionService = semanticDecisionService;
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

        // 1. Exact Match
        ChatbotMatchResult exactResult =
            _matchingService.FindMatch(request.Message);

        if (exactResult.IsMatch &&
            exactResult.Answer is string exactAnswer)
        {
            return Ok(new SendMessageResponse
            {
                Reply = exactAnswer
            });
        }

        // 2. Semantic Search
        ChatbotSemanticSearchResult? semanticResult =
            _semanticSearchService.FindBest(request.Message);

        // 3. Decide whether semantic result is trustworthy
        ChatbotSemanticDecision decision =
            _semanticDecisionService.Decide(semanticResult);

        // 4. Return appropriate response
        if (decision.Type ==
                ChatbotSemanticDecisionType.Confident &&
            decision.SearchResult?.Answer is string semanticAnswer)
        {
            return Ok(new SendMessageResponse
            {
                Reply = semanticAnswer
            });
        }

        if (decision.Type ==
            ChatbotSemanticDecisionType.Ambiguous)
        {
            return Ok(new SendMessageResponse
            {
                Reply =
                    "سؤال شما به چند موضوع نزدیک است. لطفاً کمی دقیق‌تر توضیح دهید."
            });
        }

        return Ok(new SendMessageResponse
        {
            Reply =
                "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید."
        });
    }

}