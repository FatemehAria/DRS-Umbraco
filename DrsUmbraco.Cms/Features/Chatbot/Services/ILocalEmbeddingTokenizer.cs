namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public interface ILocalEmbeddingTokenizer
{
    EmbeddingModelInput Encode(string text);
}