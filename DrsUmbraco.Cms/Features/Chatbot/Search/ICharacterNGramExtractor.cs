namespace DrsUmbraco.Cms.Features.Chatbot.Search;

public interface ICharacterNGramExtractor
{
    IReadOnlySet<string> Extract(string? text);
}