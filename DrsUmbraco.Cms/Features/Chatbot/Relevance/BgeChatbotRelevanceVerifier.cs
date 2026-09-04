using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Features.Chatbot.Relevance;

public sealed class BgeChatbotRelevanceVerifier
    : IChatbotRelevanceVerifier
{
    // private readonly IBgeRelevanceScorer _scorer;
    private readonly float _threshold;
    private readonly Lazy<IBgeRelevanceScorer> _lazyScorer;
    public BgeChatbotRelevanceVerifier(
        // IBgeRelevanceScorer scorer,
        Lazy<IBgeRelevanceScorer> lazyScorer,
        IOptions<BgeRelevanceOptions> options)
    {
        _lazyScorer =
            lazyScorer
            ?? throw new ArgumentNullException(
                nameof(lazyScorer));

        ArgumentNullException.ThrowIfNull(options);

        _threshold = options.Value.Threshold;
    }

    public bool IsRelevant(
        string question,
        ChatbotKnowledgeItem candidate)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question is required.",
                nameof(question));
        }

        ArgumentNullException.ThrowIfNull(candidate);

        string candidateText =
            BuildCandidateText(candidate);

        float score =
            _lazyScorer.Value.Score(
                question,
                candidateText);

        return score >= _threshold;
    }

    private static string BuildCandidateText(
        ChatbotKnowledgeItem candidate)
    {
        IEnumerable<string> parts =
        [
            candidate.Question,
            .. candidate.AlternativeQuestions,
            candidate.Answer
        ];

        return string.Join(
            "\n",
            parts.Where(
                part =>
                    !string.IsNullOrWhiteSpace(part)));
    }
}