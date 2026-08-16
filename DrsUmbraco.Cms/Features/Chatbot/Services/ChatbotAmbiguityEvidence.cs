namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotAmbiguityEvidence
{
    public Guid? SemanticTopKnowledgeItemId { get; init; }
    public float? SemanticTopScore { get; init; }
    public float? SemanticMargin { get; init; }

    public Guid? CentroidTopKnowledgeItemId { get; init; }
    public float? CentroidTopScore { get; init; }
    public float? CentroidMargin { get; init; }

    public Guid? WeightedLexicalTopKnowledgeItemId { get; init; }
    public Guid? Bm25TopKnowledgeItemId { get; init; }

    public required bool SemanticCentroidAgree { get; init; }

    public required int Top1AgreementCount { get; init; }

    public required int ActiveStrategyCount { get; init; }

    public float? WeightedLexicalTopScore { get; init; }
    public float? WeightedLexicalMargin { get; init; }

    public float? Bm25TopScore { get; init; }
    public float? Bm25Margin { get; init; }
}