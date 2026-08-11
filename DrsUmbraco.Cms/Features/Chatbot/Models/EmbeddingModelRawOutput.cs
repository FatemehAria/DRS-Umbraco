namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class EmbeddingModelRawOutput
{
    public required float[] Values { get; init; }

    public required long[] Shape { get; init; }
}