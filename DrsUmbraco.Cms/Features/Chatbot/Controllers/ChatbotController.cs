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
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IPersianTextNormalizer _textNormalizer;
    private readonly IChatbotMatchingService _matchingService;
    private readonly LocalEmbeddingModel _embeddingModel;
    private readonly ILocalEmbeddingTokenizer _embeddingTokenizer;
    private readonly IEmbeddingService _embeddingService;
    public ChatbotController(
        IChatbotKnowledgeService knowledgeService,
        IPersianTextNormalizer textNormalizer,
        IChatbotMatchingService matchingService,
        LocalEmbeddingModel embeddingModel,
        ILocalEmbeddingTokenizer embeddingTokenizer,
        IEmbeddingService embeddingService)
    {
        _knowledgeService = knowledgeService;
        _textNormalizer = textNormalizer;
        _matchingService = matchingService;
        _embeddingModel = embeddingModel;
        _embeddingTokenizer = embeddingTokenizer;
        _embeddingService = embeddingService;
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

        var result = _matchingService.FindMatch(request.Message);

        if (result.IsMatch && result.Answer is string answer)
        {
            return Ok(new SendMessageResponse { Reply = answer });
        }

        return Ok(new SendMessageResponse { Reply = "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید." });
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

    [HttpGet("model-info")]
    public ActionResult GetModelInfo()
    {
        return Ok(new
        {
            inputs = _embeddingModel.GetInputNames(),
            outputs = _embeddingModel.GetOutputNames()
        });
    }

    [HttpPost("tokenize")]
    public ActionResult Tokenize(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                error = "Message is required."
            });
        }

        EmbeddingModelInput result =
            _embeddingTokenizer.Encode(request.Message);

        return Ok(new
        {
            inputIds = result.InputIds,
            attentionMask = result.AttentionMask,
            tokenTypeIds = result.TokenTypeIds,

            inputIdsLength = result.InputIds.Length,
            attentionMaskLength = result.AttentionMask.Length,
            tokenTypeIdsLength = result.TokenTypeIds.Length
        });
    }

    [HttpPost("embedding-raw")]
    public ActionResult GetRawEmbedding(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        EmbeddingModelInput input =
            _embeddingTokenizer.Encode(request.Message);

        EmbeddingModelRawOutput output =
            _embeddingModel.Run(input);

        return Ok(new
        {
            shape = output.Shape,
            valueCount = output.Values.Length,

            // فقط چند عدد اول برای debug
            firstValues = output.Values.Take(5)
        });
    }

    [HttpPost("embedding")]
    public ActionResult GetEmbedding(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                error = "Message is required."
            });
        }

        float[] embedding =
            _embeddingService.Generate(request.Message);

        double l2Norm = Math.Sqrt(
            embedding.Sum(value =>
                (double)value * value));

        return Ok(new
        {
            dimension = embedding.Length,
            l2Norm,
            firstValues = embedding.Take(5)
        });
    }

    [HttpPost("similarity")]
public ActionResult GetSimilarity(
    [FromBody] SimilarityRequest request)
{
    if (string.IsNullOrWhiteSpace(request.FirstText) ||
        string.IsNullOrWhiteSpace(request.SecondText))
    {
        return BadRequest(new
        {
            error = "Both texts are required."
        });
    }

    float[] firstEmbedding =
        _embeddingService.Generate(request.FirstText);

    float[] secondEmbedding =
        _embeddingService.Generate(request.SecondText);

    float similarity =
        CosineSimilarityCalculator.Calculate(
            firstEmbedding,
            secondEmbedding);

    return Ok(new
    {
        firstText = request.FirstText,
        secondText = request.SecondText,
        similarity
    });
}
}