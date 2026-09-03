using System.Diagnostics;

namespace DrsUmbraco.Cms.Features.Chatbot.Relevance;

public sealed class BgeRelevanceScorer
    : IBgeRelevanceScorer
{
    private readonly BgeRelevanceTokenizer _tokenizer;
    private readonly BgeRelevanceModel _model;
    private readonly ILogger<BgeRelevanceScorer> _logger;

    public BgeRelevanceScorer(
        BgeRelevanceTokenizer tokenizer,
        BgeRelevanceModel model,
        ILogger<BgeRelevanceScorer> logger)
    {
        _tokenizer =
            tokenizer
            ?? throw new ArgumentNullException(
                nameof(tokenizer));

        _model =
            model
            ?? throw new ArgumentNullException(
                nameof(model));

        _logger = logger;
    }

    public float Score(
        string question,
        string candidateText)
    {
        Stopwatch totalStopwatch = Stopwatch.StartNew();

        Stopwatch tokenizationStopwatch = Stopwatch.StartNew();

        BgeRelevanceModelInput input =
            _tokenizer.Encode(
                question,
                candidateText);

        tokenizationStopwatch.Stop();

        Stopwatch inferenceStopwatch = Stopwatch.StartNew();

        float score = _model.Run(input);

        inferenceStopwatch.Stop();
        totalStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {TotalMs} ms. " +
            "TokenizationMs={TokenizationMs}, InferenceMs={InferenceMs}, InputTokenCount={InputTokenCount}.",
            "BgeScoring",
            totalStopwatch.Elapsed.TotalMilliseconds,
            tokenizationStopwatch.Elapsed.TotalMilliseconds,
            inferenceStopwatch.Elapsed.TotalMilliseconds,
            input.InputIds.Length);

        return score;
    }
}