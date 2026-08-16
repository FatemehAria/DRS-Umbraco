using DrsUmbraco.Cms.Features.Chatbot.Services;
using Xunit;

public sealed class ChatbotCandidateIntentSetBuilderTests
{
    [Fact]
    public void Build_ShouldKeepOnlyCandidatesSupportedInTopTwo()
    {
        // Arrange
        Guid knowledgeItemA = Guid.NewGuid();
        Guid knowledgeItemB = Guid.NewGuid();
        Guid knowledgeItemC = Guid.NewGuid();

        ChatbotCandidateEvidence[] evidence =
        [
            CreateEvidence(
                knowledgeItemId: knowledgeItemA,
                answer: "Answer A",
                semanticRank: 1,
                centroidRank: 1,
                weightedLexicalRank: 2,
                bm25Rank: 1),

            CreateEvidence(
                knowledgeItemId: knowledgeItemB,
                answer: "Answer B",
                semanticRank: 2,
                centroidRank: 2,
                weightedLexicalRank: 1,
                bm25Rank: 3),

            CreateEvidence(
                knowledgeItemId: knowledgeItemC,
                answer: "Answer C",
                semanticRank: 3,
                centroidRank: 4,
                weightedLexicalRank: 3,
                bm25Rank: 4)
        ];

        ChatbotCandidateIntentSetBuilder builder = new();

        // Act
        IReadOnlyList<ChatbotCandidateIntent> result =
            builder.Build(evidence);

        // Assert
        Assert.Equal(2, result.Count);

        ChatbotCandidateIntent candidateA =
            result[0];

        Assert.Equal(
            knowledgeItemA,
            candidateA.KnowledgeItemId);

        Assert.Equal(
            "Answer A",
            candidateA.Answer);

        Assert.Equal(
            3,
            candidateA.Top1SupportCount);

        Assert.Equal(
            4,
            candidateA.Top2SupportCount);

        Assert.Equal(
            1,
            candidateA.BestRank);


        ChatbotCandidateIntent candidateB =
            result[1];

        Assert.Equal(
            knowledgeItemB,
            candidateB.KnowledgeItemId);

        Assert.Equal(
            "Answer B",
            candidateB.Answer);

        Assert.Equal(
            1,
            candidateB.Top1SupportCount);

        Assert.Equal(
            3,
            candidateB.Top2SupportCount);

        Assert.Equal(
            1,
            candidateB.BestRank);


        Assert.DoesNotContain(
            result,
            item =>
                item.KnowledgeItemId ==
                knowledgeItemC);
    }


    private static ChatbotCandidateEvidence CreateEvidence(
        Guid knowledgeItemId,
        string answer,
        int? semanticRank,
        int? centroidRank,
        int? weightedLexicalRank,
        int? bm25Rank)
    {
        return new ChatbotCandidateEvidence
        {
            KnowledgeItemId =
                knowledgeItemId,

            Answer =
                answer,

            SemanticRank =
                semanticRank,

            CentroidRank =
                centroidRank,

            WeightedLexicalRank =
                weightedLexicalRank,

            Bm25Rank =
                bm25Rank
        };
    }
}