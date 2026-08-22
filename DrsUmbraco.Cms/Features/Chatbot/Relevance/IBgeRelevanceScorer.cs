namespace DrsUmbraco.Cms.Features.Chatbot.Relevance;

public interface IBgeRelevanceScorer
{
    float Score(
        string question,
        string candidateText);
}