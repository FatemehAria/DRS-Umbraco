using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public sealed class ChatbotDiscriminativeEvidenceService
    : IChatbotDiscriminativeEvidenceService
{
    private readonly IChatbotCandidateEvidenceService
        _candidateEvidenceService;

    private readonly IChatbotKnowledgeService
        _knowledgeService;

    private readonly IPersianWordTokenizer
        _wordTokenizer;

    private readonly IBm25TermFilter
        _termFilter;

    public ChatbotDiscriminativeEvidenceService(
        IChatbotCandidateEvidenceService candidateEvidenceService,
        IChatbotKnowledgeService knowledgeService,
        IPersianWordTokenizer wordTokenizer,
        IBm25TermFilter termFilter)
    {
        _candidateEvidenceService =
            candidateEvidenceService;

        _knowledgeService =
            knowledgeService;

        _wordTokenizer =
            wordTokenizer;

        _termFilter =
            termFilter;
    }

    public ChatbotDiscriminativeEvidence Analyze(
        string question,
        int candidateLimit = 4)
    {
        IReadOnlyList<string> userTerms =
            GetTerms(question);

        IReadOnlyList<ChatbotCandidateEvidence> evidence =
            _candidateEvidenceService.Find(
                question,
                candidateLimit,
                ChatbotKnowledgeItemKind.Answer);

        List<CandidateTerms> candidates = [];

        foreach (ChatbotCandidateEvidence candidate in evidence)
        {
            ChatbotKnowledgeItem? item =
                _knowledgeService.GetById(
                    candidate.KnowledgeItemId);

            if (item is null)
            {
                continue;
            }

            HashSet<string> terms =
                GetKnowledgeItemTerms(item);

            candidates.Add(
                new CandidateTerms(
                    item,
                    terms));
        }

        List<ChatbotDiscriminativeCandidateEvidence>
            results = [];

        foreach (CandidateTerms candidate in candidates)
        {
            HashSet<string> competitorTerms =
                candidates
                    .Where(other =>
                        other.Item.Id != candidate.Item.Id)
                    .SelectMany(other =>
                        other.Terms)
                    .ToHashSet(
                        StringComparer.Ordinal);

            string[] discriminativeTerms =
                candidate.Terms
                    .Where(term =>
                        !competitorTerms.Contains(term))
                    .OrderBy(term => term)
                    .ToArray();

            string[] matchedTerms =
                discriminativeTerms
                    .Where(term =>
                        userTerms.Contains(
                            term,
                            StringComparer.Ordinal))
                    .ToArray();

            results.Add(
                new ChatbotDiscriminativeCandidateEvidence
                {
                    KnowledgeItemId =
                        candidate.Item.Id,

                    Question =
                        candidate.Item.Question,

                    DiscriminativeTerms =
                        discriminativeTerms,

                    MatchedDiscriminativeTerms =
                        matchedTerms,

                    MatchedDiscriminativeTermCount =
                        matchedTerms.Length
                });
        }

        return new ChatbotDiscriminativeEvidence
        {
            UserTerms =
                userTerms,

            Candidates =
                results
        };
    }

    private IReadOnlyList<string> GetTerms(
        string? text)
    {
        IReadOnlyList<string> tokens =
            _wordTokenizer.Tokenize(text);

        return _termFilter
            .Filter(tokens)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private HashSet<string> GetKnowledgeItemTerms(
        ChatbotKnowledgeItem item)
    {
        IEnumerable<string> texts =
            new[]
            {
                item.Question
            }
            .Concat(
                item.AlternativeQuestions);

        return texts
            .SelectMany(GetTerms)
            .ToHashSet(
                StringComparer.Ordinal);
    }

    private sealed record CandidateTerms(
        ChatbotKnowledgeItem Item,
        HashSet<string> Terms);
}