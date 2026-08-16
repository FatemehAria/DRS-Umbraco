using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotCandidateIntentSetEvidenceService
    : IChatbotCandidateIntentSetEvidenceService
{
    private readonly IChatbotCandidateEvidenceService
        _candidateEvidenceService;

    private readonly IChatbotCandidateIntentSetBuilder
        _candidateIntentSetBuilder;

    public ChatbotCandidateIntentSetEvidenceService(
        IChatbotCandidateEvidenceService candidateEvidenceService,
        IChatbotCandidateIntentSetBuilder candidateIntentSetBuilder)
    {
        _candidateEvidenceService =
            candidateEvidenceService;

        _candidateIntentSetBuilder =
            candidateIntentSetBuilder;
    }

    public ChatbotCandidateIntentSetEvidence? Analyze(
        string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        IReadOnlyList<ChatbotCandidateEvidence> evidence =
            _candidateEvidenceService.Find(
                question,
                2);

        IReadOnlyList<ChatbotCandidateIntent> candidates =
            _candidateIntentSetBuilder.Build(
                evidence);

        if (candidates.Count == 0)
        {
            return new ChatbotCandidateIntentSetEvidence
            {
                Candidates = [],
                CandidateCount = 0,
                MaxTop1SupportCount = 0,
                SecondTop1SupportCount = 0,
                Top1SupportGap = 0
            };
        }

        int[] top1SupportCounts =
            candidates
                .Select(
                    candidate =>
                        candidate.Top1SupportCount)
                .OrderByDescending(
                    count => count)
                .ToArray();

        int maxTop1SupportCount =
            top1SupportCounts[0];

        int secondTop1SupportCount =
            top1SupportCounts.Length >= 2
                ? top1SupportCounts[1]
                : 0;

        int top1SupportGap =
            maxTop1SupportCount -
            secondTop1SupportCount;

        return new ChatbotCandidateIntentSetEvidence
        {
            Candidates =
                candidates,

            CandidateCount =
                candidates.Count,

            MaxTop1SupportCount =
                maxTop1SupportCount,

            SecondTop1SupportCount =
                secondTop1SupportCount,

            Top1SupportGap =
                top1SupportGap
        };
    }
}