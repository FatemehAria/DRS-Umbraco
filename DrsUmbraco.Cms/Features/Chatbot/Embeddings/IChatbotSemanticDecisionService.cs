namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public interface IChatbotSemanticDecisionService
{
    ChatbotSemanticDecision Decide(ChatbotSemanticSearchResult? result);
}