using DrsUmbraco.Cms.Features.Chatbot.Contracts;
using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;
using Microsoft.AspNetCore.Mvc;
using DrsUmbraco.Cms.Features.Chatbot.Filters;
using DrsUmbraco.Cms.Features.Chatbot.Search;

namespace DrsUmbraco.Cms.Features.Chatbot.Controllers;

[ApiController]
[Route("api/chatbot/debug")]
[TypeFilter(typeof(DevelopmentOnlyFilter))]
public sealed class ChatbotDebugController : ControllerBase
{
    private readonly IChatbotKnowledgeService _knowledgeService;
    private readonly IPersianTextNormalizer _textNormalizer;
    private readonly IChatbotMatchingService _matchingService;
    private readonly LocalEmbeddingModel _embeddingModel;
    private readonly ILocalEmbeddingTokenizer _embeddingTokenizer;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatbotSemanticIndexBuilder _semanticIndexBuilder;
    private readonly IChatbotSemanticIndex _semanticIndex;
    private readonly IChatbotSemanticSearchService _semanticSearchService;
    private readonly IChatbotSemanticDecisionService _semanticDecisionService;
    private readonly IChatbotSemanticIndexUpdater _semanticIndexUpdater;
    private readonly IChatbotHybridSearchService _hybridSearchService;
    private readonly IChatbotWeightedHybridSearchService _weightedHybridSearchService;
    private readonly IChatbotSemanticRankingService _semanticRankingService;
    private readonly IChatbotWeightedLexicalRankingService _weightedLexicalRankingService;
    private readonly IChatbotSemanticCentroidRankingService _semanticCentroidRankingService;
    private readonly IChatbotRerankingService _rerankingService;
    private readonly IChatbotBm25RankingService _bm25RankingService;
    private readonly IChatbotCandidateEvidenceService _candidateEvidenceService;
    private readonly IChatbotAmbiguityEvidenceService _ambiguityEvidenceService;
    private readonly IChatbotCandidateIntentSetBuilder _candidateIntentSetBuilder;
    private readonly IChatbotCandidateIntentSetEvidenceService _candidateIntentSetEvidenceService;
    private readonly IChatbotClarificationExactMatchingService _clarificationExactMatchingService;
    private readonly IChatbotDiscriminativeEvidenceService _discriminativeEvidenceService;
    public ChatbotDebugController(
        IChatbotKnowledgeService knowledgeService,
        IPersianTextNormalizer textNormalizer,
        IChatbotMatchingService matchingService,
        LocalEmbeddingModel embeddingModel,
        ILocalEmbeddingTokenizer embeddingTokenizer,
        IEmbeddingService embeddingService,
        IChatbotSemanticIndexBuilder semanticIndexBuilder,
        IChatbotSemanticIndex semanticIndex,
        IChatbotSemanticSearchService semanticSearchService,
        IChatbotSemanticDecisionService semanticDecisionService,
        IChatbotSemanticIndexUpdater semanticIndexUpdater,
        IChatbotHybridSearchService chatbotHybridSearchService,
        IChatbotWeightedHybridSearchService chatbotWeightedHybridSearchService,
        IChatbotSemanticRankingService chatbotSemanticRankingService,
        IChatbotWeightedLexicalRankingService chatbotWeightedLexicalRankingService,
        IChatbotSemanticCentroidRankingService chatbotSemanticCentroidRankingService,
        IChatbotRerankingService chatbotRerankingService,
        IChatbotBm25RankingService bm25RankingService,
        IChatbotCandidateEvidenceService candidateEvidenceService,
        IChatbotAmbiguityEvidenceService ambiguityEvidenceService,
        IChatbotCandidateIntentSetBuilder candidateIntentSetBuilder,
        IChatbotCandidateIntentSetEvidenceService candidateIntentSetEvidenceService,
        IChatbotClarificationExactMatchingService clarificationExactMatchingService,
        IChatbotDiscriminativeEvidenceService discriminativeEvidenceService)
    {
        _knowledgeService = knowledgeService;
        _textNormalizer = textNormalizer;
        _matchingService = matchingService;
        _embeddingModel = embeddingModel;
        _embeddingTokenizer = embeddingTokenizer;
        _embeddingService = embeddingService;
        _semanticIndexBuilder = semanticIndexBuilder;
        _semanticIndex = semanticIndex;
        _semanticSearchService = semanticSearchService;
        _semanticDecisionService = semanticDecisionService;
        _semanticIndexUpdater = semanticIndexUpdater;
        _hybridSearchService = chatbotHybridSearchService;
        _weightedHybridSearchService = chatbotWeightedHybridSearchService;
        _semanticRankingService = chatbotSemanticRankingService;
        _weightedLexicalRankingService = chatbotWeightedLexicalRankingService;
        _semanticCentroidRankingService = chatbotSemanticCentroidRankingService;
        _rerankingService = chatbotRerankingService;
        _bm25RankingService = bm25RankingService;
        _candidateEvidenceService = candidateEvidenceService;
        _ambiguityEvidenceService = ambiguityEvidenceService;
        _candidateIntentSetBuilder = candidateIntentSetBuilder;
        _candidateIntentSetEvidenceService = candidateIntentSetEvidenceService;
        _clarificationExactMatchingService = clarificationExactMatchingService;
        _discriminativeEvidenceService = discriminativeEvidenceService;
    }


    // [HttpPost("messages")]
    // public ActionResult<SendMessageResponse> Send(
    //     SendMessageRequest request)
    // {
    //     // اعتبارسنجی پیام
    //     if (string.IsNullOrWhiteSpace(request.Message))
    //     {
    //         return BadRequest(new { error = "Message is required." });
    //     }

    //     var result = _matchingService.FindMatch(request.Message);

    //     if (result.IsMatch && result.Answer is string answer)
    //     {
    //         return Ok(new SendMessageResponse { Reply = answer });
    //     }

    //     return Ok(new SendMessageResponse { Reply = "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید." });
    // }

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

    //For Test
    [HttpPost("semantic-index/rebuild")]
    public ActionResult RebuildSemanticIndex()
    {
        int candidateCount =
            _semanticIndexBuilder.Rebuild();

        return Ok(new
        {
            candidateCount
        });
    }

    [HttpGet("semantic-index")]
    public ActionResult GetSemanticIndex()
    {
        IReadOnlyList<ChatbotSemanticCandidate> candidates =
            _semanticIndex.GetAll();

        return Ok(new
        {
            candidateCount = candidates.Count,

            candidates = candidates.Select(candidate => new
            {
                candidate.KnowledgeItemId,
                candidate.Text,
                embeddingDimension =
                    candidate.Embedding.Length
            })
        });
    }

    [HttpPost("semantic-search")]
    public ActionResult SemanticSearch(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                error = "Message is required."
            });
        }

        ChatbotSemanticSearchResult? result =
            _semanticSearchService.FindBest(request.Message);

        if (result is null)
        {
            return Ok(new
            {
                found = false,
                reason = "Semantic index is empty."
            });
        }

        return Ok(new
        {
            found = true,

            result.KnowledgeItemId,
            result.MatchedText,
            result.Score,

            result.SecondBestKnowledgeItemId,
            result.SecondBestScore,
            result.Margin,

            result.Answer
        });
    }

    // For Test
    [HttpPost("semantic-index/refresh/{knowledgeItemId:guid}")]
    public ActionResult RefreshSemanticIndexItem(
    Guid knowledgeItemId)
    {
        bool indexed =
            _semanticIndexUpdater.Refresh(knowledgeItemId);

        return Ok(new
        {
            knowledgeItemId,
            indexed,
            candidateCount =
                _semanticIndex.GetAll().Count
        });
    }

    [HttpPost("hybrid-search")]
    public ActionResult HybridSearch(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotHybridSearchResult? result =
            _hybridSearchService.FindBest(
                request.Message);

        if (result is null)
        {
            return Ok(new
            {
                found = false
            });
        }

        return Ok(new
        {
            found = true,
            result.KnowledgeItemId,
            result.MatchedText,
            result.SemanticScore,
            result.LexicalScore,
            result.Score,
            result.SecondBestKnowledgeItemId,
            result.SecondBestScore,
            result.Margin,
            result.Answer
        });
    }

    [HttpPost("weighted-hybrid-search")]
    public ActionResult WeightedHybridSearch(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotHybridSearchResult? result =
            _weightedHybridSearchService.FindBest(
                request.Message);

        if (result is null)
        {
            return Ok(new
            {
                found = false
            });
        }

        return Ok(new
        {
            found = true,
            result.KnowledgeItemId,
            result.MatchedText,
            result.SemanticScore,
            result.LexicalScore,
            result.Score,
            result.SecondBestKnowledgeItemId,
            result.SecondBestScore,
            result.Margin,
            result.Answer
        });
    }

    [HttpPost("semantic-top")]
    public ActionResult SemanticTop(
    [FromBody] SendMessageRequest request,
    [FromQuery] int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        if (limit < 1 || limit > 10)
        {
            return BadRequest(
                new
                {
                    error =
                        "Limit must be between 1 and 10."
                });
        }

        IReadOnlyList<ChatbotSemanticRankedResult> results =
            _semanticRankingService.FindTop(
                request.Message,
                limit);

        return Ok(new
        {
            count = results.Count,
            results
        });
    }

    [HttpPost("weighted-lexical-top")]
    public ActionResult WeightedLexicalTop(
    [FromBody] SendMessageRequest request,
    [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        if (limit < 1 || limit > 10)
        {
            return BadRequest();
        }

        IReadOnlyList<ChatbotLexicalRankedResult> results =
            _weightedLexicalRankingService.FindTop(
                request.Message,
                limit);

        return Ok(new
        {
            count = results.Count,
            results
        });
    }

    [HttpPost("semantic-centroid-top")]
    public ActionResult SemanticCentroidTop(
    [FromBody] SendMessageRequest request,
    [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        if (limit < 1 || limit > 10)
        {
            return BadRequest(
                new
                {
                    error =
                        "Limit must be between 1 and 10."
                });
        }

        IReadOnlyList<ChatbotSemanticRankedResult> results =
            _semanticCentroidRankingService.FindTop(
                request.Message,
                limit);

        return Ok(new
        {
            count = results.Count,
            results
        });
    }

    [HttpPost("rerank")]
    public ActionResult Rerank(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotRerankResult? result =
            _rerankingService.FindBest(
                request.Message);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost("bm25-top")]
    public ActionResult Bm25Top(
    [FromBody] SendMessageRequest request,
    [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        if (limit < 1 || limit > 10)
        {
            return BadRequest(
                new
                {
                    error =
                        "Limit must be between 1 and 10."
                });
        }

        IReadOnlyList<ChatbotLexicalRankedResult> results =
            _bm25RankingService.FindTop(
                request.Message,
                limit);

        return Ok(
            new
            {
                count = results.Count,
                results
            });
    }

    [HttpPost("candidate-evidence")]
    public ActionResult CandidateEvidence(
    [FromBody] SendMessageRequest request,
    [FromQuery] int limit = 4)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        if (limit < 1 || limit > 10)
        {
            return BadRequest(
                new
                {
                    error =
                        "Limit must be between 1 and 10."
                });
        }

        IReadOnlyList<ChatbotCandidateEvidence> results =
            _candidateEvidenceService.Find(
                request.Message,
                limit);

        return Ok(
            new
            {
                count = results.Count,
                results
            });
    }

    [HttpPost("ambiguity-evidence")]
    public ActionResult<ChatbotAmbiguityEvidence>
    GetAmbiguityEvidence(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotAmbiguityEvidence? evidence =
            _ambiguityEvidenceService.Analyze(
                request.Message);

        if (evidence is null)
        {
            return NotFound();
        }

        return Ok(evidence);
    }

    [HttpPost("candidate-intents")]
    public ActionResult<IReadOnlyList<ChatbotCandidateIntent>>
    GetCandidateIntents(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        IReadOnlyList<ChatbotCandidateEvidence> evidence =
            _candidateEvidenceService.Find(
                request.Message,
                2);

        IReadOnlyList<ChatbotCandidateIntent> candidates =
            _candidateIntentSetBuilder.Build(
                evidence);

        return Ok(candidates);
    }

    [HttpPost("candidate-intent-evidence")]
    public ActionResult<ChatbotCandidateIntentSetEvidence>
    GetCandidateIntentEvidence(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotCandidateIntentSetEvidence? evidence =
            _candidateIntentSetEvidenceService.Analyze(
                request.Message);

        if (evidence is null)
        {
            return NotFound();
        }

        return Ok(evidence);
    }

    [HttpPost("retrieval-pools")]
    public ActionResult<ChatbotRetrievalPoolsEvidence>
    GetRetrievalPools(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        IReadOnlyList<ChatbotCandidateEvidence> answers =
            _candidateEvidenceService.Find(
                request.Message,
                4,
                ChatbotKnowledgeItemKind.Answer);

        IReadOnlyList<ChatbotCandidateEvidence> clarifications =
            _candidateEvidenceService.Find(
                request.Message,
                4,
                ChatbotKnowledgeItemKind.Clarification);

        return Ok(
            new ChatbotRetrievalPoolsEvidence
            {
                Answers = answers,
                Clarifications = clarifications
            });
    }

    [HttpPost("clarification-exact")]
    public ActionResult<ChatbotMatchResult> ClarificationExact(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotMatchResult result =
            _clarificationExactMatchingService.Find(
                request.Message);

        return Ok(result);
    }

    [HttpPost("answer-rerank")]
    public ActionResult<ChatbotRerankResult?> AnswerRerank(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotRerankResult? result =
            _rerankingService.FindBest(
                request.Message,
                ChatbotKnowledgeItemKind.Answer);

        return Ok(result);
    }

    [HttpPost("answer-vs-clarification")]
    public ActionResult<ChatbotAnswerClarificationEvidence>
    AnswerVsClarification(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotRerankResult? answer =
            _rerankingService.FindBest(
                request.Message,
                ChatbotKnowledgeItemKind.Answer);

        ChatbotRerankResult? clarification =
            _rerankingService.FindBest(
                request.Message,
                ChatbotKnowledgeItemKind.Clarification);

        return Ok(
            new ChatbotAnswerClarificationEvidence
            {
                Answer = answer,
                Clarification = clarification
            });
    }

    [HttpPost("discriminative-evidence")]
    public ActionResult<ChatbotDiscriminativeEvidence>
    DiscriminativeEvidence(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotDiscriminativeEvidence result =
            _discriminativeEvidenceService.Analyze(
                request.Message,
                4);

        return Ok(result);
    }

    [HttpPost("winner-discriminative-evidence")]
    public ActionResult<ChatbotWinnerDiscriminativeEvidence>
    WinnerDiscriminativeEvidence(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        ChatbotRerankResult? winner =
            _rerankingService.FindBest(
                request.Message,
                ChatbotKnowledgeItemKind.Answer);

        ChatbotDiscriminativeEvidence evidence =
            _discriminativeEvidenceService.Analyze(
                request.Message,
                4);

        ChatbotDiscriminativeCandidateEvidence?
            winnerEvidence =
                winner is null
                    ? null
                    : evidence.Candidates.FirstOrDefault(
                        candidate =>
                            candidate.KnowledgeItemId ==
                            winner.KnowledgeItemId);

        return Ok(
            new ChatbotWinnerDiscriminativeEvidence
            {
                Winner = winner,
                WinnerEvidence = winnerEvidence,
                UserTerms = evidence.UserTerms
            });
    }

    [HttpPost("answer-top3")]
    public ActionResult<IReadOnlyList<ChatbotCandidateEvidence>>
    AnswerTop3(
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        IReadOnlyList<ChatbotCandidateEvidence> candidates =
            _candidateEvidenceService.Find(
                request.Message,
                3,
                ChatbotKnowledgeItemKind.Answer);

        return Ok(
            candidates
                .Take(3)
                .ToArray());
    }

    [HttpPost("answer-no-match-evidence")]
    public IActionResult GetAnswerNoMatchEvidence(
    [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest();
        }

        IReadOnlyList<ChatbotCandidateEvidence> candidates =
            _candidateEvidenceService.Find(
                request.Message,
                3,
                ChatbotKnowledgeItemKind.Answer);

        float semanticTopScore =
            candidates
                .Where(candidate => candidate.SemanticScore.HasValue)
                .Select(candidate => candidate.SemanticScore!.Value)
                .DefaultIfEmpty(0f)
                .Max();

        return Ok(new
        {
            SemanticTopScore = semanticTopScore,

            Candidates = candidates.Select(candidate => new
            {
                candidate.KnowledgeItemId,
                candidate.SemanticRank,
                candidate.SemanticScore,
                candidate.SemanticMatchedText
            })
        });
    }
}