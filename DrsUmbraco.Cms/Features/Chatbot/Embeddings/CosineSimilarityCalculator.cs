namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public static class CosineSimilarityCalculator
{
    public static float Calculate(
        float[] first,
        float[] second)
    {
        if (first.Length != second.Length)
        {
            throw new ArgumentException(
                "Vectors must have the same length.");
        }

        double dotProduct = 0;
        double firstMagnitudeSquared = 0;
        double secondMagnitudeSquared = 0;

        for (int i = 0; i < first.Length; i++)
        {
            dotProduct += first[i] * second[i];

            firstMagnitudeSquared +=
                first[i] * first[i];

            secondMagnitudeSquared +=
                second[i] * second[i];
        }

        double firstMagnitude =
            Math.Sqrt(firstMagnitudeSquared);

        double secondMagnitude =
            Math.Sqrt(secondMagnitudeSquared);

        if (firstMagnitude == 0 ||
            secondMagnitude == 0)
        {
            throw new InvalidOperationException(
                "Cannot calculate cosine similarity for a zero vector.");
        }

        return (float)(
            dotProduct /
            (firstMagnitude * secondMagnitude));
    }
}