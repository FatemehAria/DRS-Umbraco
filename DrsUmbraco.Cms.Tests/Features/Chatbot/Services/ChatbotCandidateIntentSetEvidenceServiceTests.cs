using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using Xunit;

public sealed class
    ChatbotCandidateIntentSetEvidenceServiceTests
{
    [Fact]
    public void Analyze_WhenOneIntentDominates_ShouldReturnLargeGap()
    {
        // Arrange

        Guid emailId = Guid.NewGuid();
        Guid passwordId = Guid.NewGuid();

        ChatbotCandidateEvidence[] rawEvidence =
        [
            new ChatbotCandidateEvidence
            {
                KnowledgeItemId = emailId,
                Answer = "Email answer"
            }
        ];

        ChatbotCandidateIntent[] candidateIntents =
        [
            new ChatbotCandidateIntent
            {
                KnowledgeItemId = emailId,
                Answer = "Email answer",
                Top1SupportCount = 4,
                Top2SupportCount = 4,
                BestRank = 1
            },

            new ChatbotCandidateIntent
            {
                KnowledgeItemId = passwordId,
                Answer = "Password answer",
                Top1SupportCount = 0,
                Top2SupportCount = 3,
                BestRank = 2
            }
        ];

        FakeCandidateEvidenceService evidenceService =
            new(rawEvidence);

        FakeCandidateIntentSetBuilder intentSetBuilder =
            new(candidateIntents);

        ChatbotCandidateIntentSetEvidenceService service =
            new(
                evidenceService,
                intentSetBuilder);

        // Act

        ChatbotCandidateIntentSetEvidence? result =
            service.Analyze(
                "ایمیل حسابم رو میخوام عوض کنم");

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            2,
            result.CandidateCount);

        Assert.Equal(
            4,
            result.MaxTop1SupportCount);

        Assert.Equal(
            0,
            result.SecondTop1SupportCount);

        Assert.Equal(
            4,
            result.Top1SupportGap);
    }


    [Fact]
    public void Analyze_WhenTop1SupportIsTied_ShouldReturnZeroGap()
    {
        // Arrange

        Guid intentA = Guid.NewGuid();
        Guid intentB = Guid.NewGuid();
        Guid intentC = Guid.NewGuid();

        ChatbotCandidateEvidence[] rawEvidence =
        [
            new ChatbotCandidateEvidence
            {
                KnowledgeItemId = intentA,
                Answer = "Answer A"
            }
        ];

        ChatbotCandidateIntent[] candidateIntents =
        [
            new ChatbotCandidateIntent
            {
                KnowledgeItemId = intentA,
                Answer = "Answer A",
                Top1SupportCount = 1,
                Top2SupportCount = 3,
                BestRank = 1
            },

            new ChatbotCandidateIntent
            {
                KnowledgeItemId = intentB,
                Answer = "Answer B",
                Top1SupportCount = 1,
                Top2SupportCount = 3,
                BestRank = 1
            },

            new ChatbotCandidateIntent
            {
                KnowledgeItemId = intentC,
                Answer = "Answer C",
                Top1SupportCount = 1,
                Top2SupportCount = 1,
                BestRank = 1
            }
        ];

        FakeCandidateEvidenceService evidenceService =
            new(rawEvidence);

        FakeCandidateIntentSetBuilder intentSetBuilder =
            new(candidateIntents);

        ChatbotCandidateIntentSetEvidenceService service =
            new(
                evidenceService,
                intentSetBuilder);

        // Act

        ChatbotCandidateIntentSetEvidence? result =
            service.Analyze(
                "یکی از اطلاعات حسابم رو میخوام عوض کنم");

        // Assert

        Assert.NotNull(result);

        Assert.Equal(
            3,
            result.CandidateCount);

        Assert.Equal(
            1,
            result.MaxTop1SupportCount);

        Assert.Equal(
            1,
            result.SecondTop1SupportCount);

        Assert.Equal(
            0,
            result.Top1SupportGap);
    }


    private sealed class FakeCandidateEvidenceService
        : IChatbotCandidateEvidenceService
    {
        private readonly IReadOnlyList<ChatbotCandidateEvidence>
            _results;

        public FakeCandidateEvidenceService(
            IReadOnlyList<ChatbotCandidateEvidence> results)
        {
            _results = results;
        }

        public IReadOnlyList<ChatbotCandidateEvidence> Find(
            string question,
            int limit,
            ChatbotKnowledgeItemKind? kind = null)
        {
            return _results;
        }
    }


    private sealed class FakeCandidateIntentSetBuilder
        : IChatbotCandidateIntentSetBuilder
    {
        private readonly IReadOnlyList<ChatbotCandidateIntent>
            _results;

        public FakeCandidateIntentSetBuilder(
            IReadOnlyList<ChatbotCandidateIntent> results)
        {
            _results = results;
        }

        public IReadOnlyList<ChatbotCandidateIntent> Build(
            IReadOnlyList<ChatbotCandidateEvidence> evidence)
        {
            return _results;
        }
    }
}