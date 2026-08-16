namespace DrsUmbraco.Cms.Features.Chatbot.Contracts;

public sealed class SelectSuggestionRequest
{
    public Guid KnowledgeItemId
    {
        get;
        init;
    }
}