using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Search;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class ChatbotCandidateEvidenceServiceTests
{
    [Fact]
    public void Find_WhenSeveralStrategiesSupportSameCandidate_ShouldRankItAboveSingleStrategyCandidate()
    {
        // Arrange
        Guid singleStrategyCandidateId = Guid.NewGuid();
        Guid multiStrategyCandidateId = Guid.NewGuid();

        var semanticRankingService =
            new FakeSemanticRankingService(
                [
                    CreateSemanticResult(
                        rank: 1,
                        knowledgeItemId: singleStrategyCandidateId,
                        score: 0.95f)
                ]);

        var centroidRankingService =
            new FakeSemanticCentroidRankingService(
                [
                    CreateSemanticResult(
                        rank: 2,
                        knowledgeItemId: multiStrategyCandidateId,
                        score: 0.90f)
                ]);

        var weightedLexicalRankingService =
            new FakeWeightedLexicalRankingService(
                [
                    CreateLexicalResult(
                        rank: 3,
                        knowledgeItemId: multiStrategyCandidateId,
                        score: 0.30f)
                ]);

        var bm25RankingService =
            new FakeBm25RankingService(
                [
                    CreateLexicalResult(
                        rank: 2,
                        knowledgeItemId: multiStrategyCandidateId,
                        score: 2.50f)
                ]);

        var service =
            new ChatbotCandidateEvidenceService(
                semanticRankingService,
                centroidRankingService,
                weightedLexicalRankingService,
                bm25RankingService,
                NullLogger<ChatbotCandidateEvidenceService>.Instance);

        // Act
        IReadOnlyList<ChatbotCandidateEvidence> results =
            service.Find(
                "test question",
                3,
                ChatbotKnowledgeItemKind.Answer);

        // Assert
        Assert.Equal(2, results.Count);

        Assert.Equal(
            multiStrategyCandidateId,
            results[0].KnowledgeItemId);

        Assert.Equal(
            singleStrategyCandidateId,
            results[1].KnowledgeItemId);
    }

    private static ChatbotSemanticRankedResult
        CreateSemanticResult(
            int rank,
            Guid knowledgeItemId,
            float score)
    {
        return new ChatbotSemanticRankedResult
        {
            Rank = rank,
            KnowledgeItemId = knowledgeItemId,
            MatchedText = "matched text",
            Answer = "answer",
            Score = score
        };
    }

    private static ChatbotLexicalRankedResult
        CreateLexicalResult(
            int rank,
            Guid knowledgeItemId,
            float score)
    {
        return new ChatbotLexicalRankedResult
        {
            Rank = rank,
            KnowledgeItemId = knowledgeItemId,
            MatchedText = "matched text",
            Answer = "answer",
            Score = score
        };
    }

    private sealed class FakeSemanticRankingService
        : IChatbotSemanticRankingService
    {
        private readonly IReadOnlyList<ChatbotSemanticRankedResult>
            _results;

        public FakeSemanticRankingService(
            IReadOnlyList<ChatbotSemanticRankedResult> results)
        {
            _results = results;
        }

        public IReadOnlyList<ChatbotSemanticRankedResult> FindTop(
            string question,
            int limit,
            ChatbotKnowledgeItemKind? kind = null)
        {
            return _results;
        }
    }

    private sealed class FakeSemanticCentroidRankingService
        : IChatbotSemanticCentroidRankingService
    {
        private readonly IReadOnlyList<ChatbotSemanticRankedResult>
            _results;

        public FakeSemanticCentroidRankingService(
            IReadOnlyList<ChatbotSemanticRankedResult> results)
        {
            _results = results;
        }

        public IReadOnlyList<ChatbotSemanticRankedResult> FindTop(
            string question,
            int limit,
            ChatbotKnowledgeItemKind? kind = null)
        {
            return _results;
        }
    }

    private sealed class FakeWeightedLexicalRankingService
        : IChatbotWeightedLexicalRankingService
    {
        private readonly IReadOnlyList<ChatbotLexicalRankedResult>
            _results;

        public FakeWeightedLexicalRankingService(
            IReadOnlyList<ChatbotLexicalRankedResult> results)
        {
            _results = results;
        }

        public IReadOnlyList<ChatbotLexicalRankedResult> FindTop(
            string question,
            int limit,
            ChatbotKnowledgeItemKind? kind = null)
        {
            return _results;
        }
    }

    private sealed class FakeBm25RankingService
        : IChatbotBm25RankingService
    {
        private readonly IReadOnlyList<ChatbotLexicalRankedResult>
            _results;

        public FakeBm25RankingService(
            IReadOnlyList<ChatbotLexicalRankedResult> results)
        {
            _results = results;
        }

        public IReadOnlyList<ChatbotLexicalRankedResult> FindTop(
            string question,
            int limit,
            ChatbotKnowledgeItemKind? kind = null)
        {
            return _results;
        }
    }
}