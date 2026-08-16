namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IChatbotNoMatchDecisionService
{
    ChatbotNoMatchDecision Decide(
        float semanticTopScore);
}