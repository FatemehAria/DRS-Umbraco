using DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotCandidateIntentSetBuilder
    : IChatbotCandidateIntentSetBuilder
{
    public IReadOnlyList<ChatbotCandidateIntent> Build(
        IReadOnlyList<ChatbotCandidateEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        List<ChatbotCandidateIntent> candidates = [];

        foreach (ChatbotCandidateEvidence item in evidence)
        {
            int[] ranks =
            [
                item.SemanticRank ?? int.MaxValue,
                item.CentroidRank ?? int.MaxValue,
                item.WeightedLexicalRank ?? int.MaxValue,
                item.Bm25Rank ?? int.MaxValue
            ];

            int top1SupportCount =
                ranks.Count(rank => rank == 1);

            int top2SupportCount =
                ranks.Count(rank => rank <= 2);

            if (top2SupportCount == 0)
            {
                continue;
            }

            int bestRank =
                ranks.Min();

            candidates.Add(
                new ChatbotCandidateIntent
                {
                    KnowledgeItemId =
                        item.KnowledgeItemId,

                    Answer =
                        item.Answer,

                    Top1SupportCount =
                        top1SupportCount,

                    Top2SupportCount =
                        top2SupportCount,

                    BestRank =
                        bestRank
                });
        }

        return candidates
            .OrderByDescending(
                item => item.Top1SupportCount)
            .ThenByDescending(
                item => item.Top2SupportCount)
            .ThenBy(
                item => item.BestRank)
            .ToArray();
    }
}