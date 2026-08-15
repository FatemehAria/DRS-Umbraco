public sealed class EmbeddingCentroidCalculator
    : IEmbeddingCentroidCalculator
{
    public float[] Calculate(
        IReadOnlyList<float[]> embeddings)
    {
        ArgumentNullException.ThrowIfNull(embeddings);

        if (embeddings.Count == 0)
        {
            throw new ArgumentException(
                "At least one embedding is required.",
                nameof(embeddings));
        }

        int dimension =
            embeddings[0].Length;

        if (dimension == 0)
        {
            throw new ArgumentException(
                "Embeddings cannot be empty.",
                nameof(embeddings));
        }

        float[] centroid =
            new float[dimension];

        foreach (float[] embedding in embeddings)
        {
            if (embedding.Length != dimension)
            {
                throw new ArgumentException(
                    "All embeddings must have the same dimension.",
                    nameof(embeddings));
            }

            for (int i = 0; i < dimension; i++)
            {
                centroid[i] += embedding[i];
            }
        }

        for (int i = 0; i < centroid.Length; i++)
        {
            centroid[i] /= embeddings.Count;
        }

        Normalize(centroid);

        return centroid;
    }

    private static void Normalize(float[] vector)
    {
        double sumOfSquares = 0;

        foreach (float value in vector)
        {
            sumOfSquares += value * value;
        }

        double magnitude =
            Math.Sqrt(sumOfSquares);

        if (magnitude == 0)
        {
            throw new InvalidOperationException(
                "Cannot normalize a zero vector.");
        }

        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] =
                (float)(vector[i] / magnitude);
        }
    }
}