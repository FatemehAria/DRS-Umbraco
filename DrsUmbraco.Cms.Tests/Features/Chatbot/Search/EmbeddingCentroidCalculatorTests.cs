public sealed class EmbeddingCentroidCalculatorTests
{
    private readonly EmbeddingCentroidCalculator _calculator =
        new();

    [Fact]
    public void Calculate_WithSingleEmbedding_ShouldReturnSameDirection()
    {
        float[] result =
            _calculator.Calculate(
                [
                    [1f, 0f]
                ]);

        Assert.Equal(1f, result[0], 5);
        Assert.Equal(0f, result[1], 5);
    }

    [Fact]
    public void Calculate_ShouldAverageEmbeddingsAndNormalize()
    {
        float[] result =
            _calculator.Calculate(
                [
                    [1f, 0f],
                    [0f, 1f]
                ]);

        float expected =
            (float)(1.0 / Math.Sqrt(2));

        Assert.Equal(expected, result[0], 5);
        Assert.Equal(expected, result[1], 5);
    }

    [Fact]
    public void Calculate_WithDifferentDimensions_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => _calculator.Calculate(
                [
                    [1f, 0f],
                    [1f, 0f, 0f]
                ]));
    }
}