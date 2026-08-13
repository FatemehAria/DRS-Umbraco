using DrsUmbraco.Cms.Features.Chatbot.Embeddings;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Embeddings;

public sealed class CosineSimilarityCalculatorTests
{
    [Fact]
    public void Calculate_WhenVectorsAreIdentical_ShouldReturnOne()
    {
        float[] first = [1f, 2f, 3f];
        float[] second = [1f, 2f, 3f];

        float result =
            CosineSimilarityCalculator.Calculate(
                first,
                second);

        Assert.InRange(result, 0.9999f, 1.0001f);
    }

    [Fact]
    public void Calculate_WhenVectorsAreOpposite_ShouldReturnMinusOne()
    {
        float[] first = [1f, 0f];
        float[] second = [-1f, 0f];

        float result =
            CosineSimilarityCalculator.Calculate(
                first,
                second);

        Assert.InRange(result, -1.0001f, -0.9999f);
    }

    [Fact]
    public void Calculate_WhenVectorsHaveDifferentLengths_ShouldThrow()
    {
        float[] first = [1f, 2f];
        float[] second = [1f, 2f, 3f];

        Assert.Throws<ArgumentException>(() =>
            CosineSimilarityCalculator.Calculate(
                first,
                second));
    }

    [Fact]
    public void Calculate_WhenOneVectorIsZero_ShouldThrow()
    {
        float[] first = [0f, 0f];
        float[] second = [1f, 2f];

        Assert.Throws<InvalidOperationException>(() =>
            CosineSimilarityCalculator.Calculate(
                first,
                second));
    }
}