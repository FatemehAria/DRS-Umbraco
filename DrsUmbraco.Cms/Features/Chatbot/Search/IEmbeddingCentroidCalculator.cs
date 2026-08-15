public interface IEmbeddingCentroidCalculator
{
    float[] Calculate(
        IReadOnlyList<float[]> embeddings);
}