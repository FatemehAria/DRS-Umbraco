namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotCandidateEvidence
{
    public required Guid KnowledgeItemId { get; init; }

    public required string Answer { get; init; }

    public int? SemanticRank { get; init; }
    public float? SemanticScore { get; init; }
    public string? SemanticMatchedText { get; init; }

    public int? CentroidRank { get; init; }
    public float? CentroidScore { get; init; }

    public int? WeightedLexicalRank { get; init; }
    public float? WeightedLexicalScore { get; init; }
    public string? WeightedLexicalMatchedText { get; init; }

    public int? Bm25Rank { get; init; }
    public float? Bm25Score { get; init; }
    public string? Bm25MatchedText { get; init; }

    public float? WeightedLexicalTopScore { get; init; }
    public float? WeightedLexicalMargin { get; init; }

    public float? Bm25TopScore { get; init; }
    public float? Bm25Margin { get; init; }
}