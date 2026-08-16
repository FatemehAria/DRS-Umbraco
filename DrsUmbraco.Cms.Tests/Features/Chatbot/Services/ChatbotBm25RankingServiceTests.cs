using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Search;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using DrsUmbraco.Cms.Features.Chatbot.Text;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class ChatbotBm25RankingServiceTests
{
    [Fact]
    public void FindTop_ShouldRankRareMatchingIntentFirst()
    {
        Guid lockedAccountId =
            Guid.NewGuid();

        Guid loginProblemId =
            Guid.NewGuid();

        FakeSemanticIndex index =
            new(
                [
                    CreateCandidate(
                        lockedAccountId,
                        "حساب کاربری من قفل شده است"),

                    CreateCandidate(
                        lockedAccountId,
                        "اکانتم قفل شده"),

                    CreateCandidate(
                        loginProblemId,
                        "نمی‌توانم وارد حساب شوم"),

                    CreateCandidate(
                        loginProblemId,
                        "ورود به حساب انجام نمی‌شود")
                ]);

        PersianWordTokenizer tokenizer =
            new(new PersianTextNormalizer());

        Bm25CorpusStatisticsBuilder statisticsBuilder =
            new(tokenizer);

        Bm25SimilarityCalculator calculator =
            new(tokenizer);

        ChatbotBm25RankingService service =
            new(
                index,
                statisticsBuilder,
                calculator);

        IReadOnlyList<ChatbotLexicalRankedResult> results =
            service.FindTop(
                "حسابم قفل شده",
                10);

        Assert.NotEmpty(results);

        Assert.Equal(
            lockedAccountId,
            results[0].KnowledgeItemId);
    }

    private static ChatbotSemanticCandidate CreateCandidate(
        Guid knowledgeItemId,
        string text)
    {
        return new ChatbotSemanticCandidate
        {
            KnowledgeItemId =
                knowledgeItemId,

            Text =
                text,

            Answer =
                $"Answer {knowledgeItemId}",

            Embedding =
                [1f]
        };
    }

    private sealed class FakeSemanticIndex
        : IChatbotSemanticIndex
    {
        private readonly IReadOnlyList<
            ChatbotSemanticCandidate> _candidates;

        public FakeSemanticIndex(
            IReadOnlyList<
                ChatbotSemanticCandidate> candidates)
        {
            _candidates = candidates;
        }

        public IReadOnlyList<
            ChatbotSemanticCandidate> GetAll()
        {
            return _candidates;
        }

        public void Replace(
            IReadOnlyList<
                ChatbotSemanticCandidate> candidates)
        {
            throw new NotSupportedException();
        }

        public void ReplaceForKnowledgeItem(
            Guid knowledgeItemId,
            IReadOnlyList<
                ChatbotSemanticCandidate> candidates)
        {
            throw new NotSupportedException();
        }

        public void RemoveForKnowledgeItem(
            Guid knowledgeItemId)
        {
            throw new NotSupportedException();
        }
    }
}