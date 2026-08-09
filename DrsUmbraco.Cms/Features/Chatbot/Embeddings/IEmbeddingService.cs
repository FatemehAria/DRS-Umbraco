namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public interface IEmbeddingService
{
    float[] Generate(string text);
}