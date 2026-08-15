using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;

namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public interface ILexicalCorpusStatisticsBuilder
{
    LexicalCorpusStatistics Build(
        IReadOnlyList<ChatbotSemanticCandidate> candidates);
}