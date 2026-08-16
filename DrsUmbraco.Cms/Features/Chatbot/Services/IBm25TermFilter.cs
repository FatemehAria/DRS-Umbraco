namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IBm25TermFilter
{
    IReadOnlyList<string> Filter(
        IReadOnlyList<string> tokens);
}