namespace DrsUmbraco.Cms.Features.Chatbot.Relevance;

public sealed class BgeRelevanceScorer
    : IBgeRelevanceScorer
{
    private readonly BgeRelevanceTokenizer _tokenizer;
    private readonly BgeRelevanceModel _model;

    public BgeRelevanceScorer(
        BgeRelevanceTokenizer tokenizer,
        BgeRelevanceModel model)
    {
        _tokenizer =
            tokenizer
            ?? throw new ArgumentNullException(
                nameof(tokenizer));

        _model =
            model
            ?? throw new ArgumentNullException(
                nameof(model));
    }

    public float Score(
        string question,
        string candidateText)
    {
        BgeRelevanceModelInput input =
            _tokenizer.Encode(
                question,
                candidateText);

        return _model.Run(input);
    }
}