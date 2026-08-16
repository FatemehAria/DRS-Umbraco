namespace DrsUmbraco.Cms.Features.Chatbot.Services;

public interface IPersianWordTokenizer
{
    IReadOnlyList<string> Tokenize(string? text);
}