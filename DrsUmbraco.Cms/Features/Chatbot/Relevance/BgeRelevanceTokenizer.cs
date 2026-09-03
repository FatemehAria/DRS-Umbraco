using Microsoft.ML.Tokenizers;
using System.Diagnostics;

namespace DrsUmbraco.Cms.Features.Chatbot.Relevance;

public sealed class BgeRelevanceTokenizer
{
    private readonly SentencePieceTokenizer _tokenizer;

    private const long BeginningOfSentenceTokenId = 0;
    private const long EndOfSentenceTokenId = 2;
    private const long UnknownTokenId = 3;

    private const int MaxSequenceLength = 512;

    // <s> question </s></s> candidate </s>
    // چهار special token داریم.
    private const int MaxContentTokenCount =
        MaxSequenceLength - 4;

    public BgeRelevanceTokenizer(
        string tokenizerPath,
        ILogger<BgeRelevanceTokenizer> logger)
    {
        if (string.IsNullOrWhiteSpace(tokenizerPath))
        {
            throw new ArgumentException(
                "Tokenizer path is required.",
                nameof(tokenizerPath));
        }

        if (!File.Exists(tokenizerPath))
        {
            throw new FileNotFoundException(
                "BGE tokenizer model was not found.",
                tokenizerPath);
        }

        Stopwatch tokenizerLoadStopwatch = Stopwatch.StartNew();

        using FileStream stream =
            File.OpenRead(tokenizerPath);

        _tokenizer =
            SentencePieceTokenizer.Create(
                stream,
                false,
                false);

        tokenizerLoadStopwatch.Stop();

        logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms.",
            "BgeTokenizerLoad",
            tokenizerLoadStopwatch.ElapsedMilliseconds);
    }

    public BgeRelevanceModelInput Encode(
        string question,
        string candidateText)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question is required.",
                nameof(question));
        }

        if (string.IsNullOrWhiteSpace(candidateText))
        {
            throw new ArgumentException(
                "Candidate text is required.",
                nameof(candidateText));
        }

        List<long> questionIds =
            EncodeContent(question);

        List<long> candidateIds =
            EncodeContent(candidateText);

        // longest-first truncation
        while (
            questionIds.Count
            + candidateIds.Count
            > MaxContentTokenCount)
        {
            if (
                questionIds.Count
                >= candidateIds.Count)
            {
                questionIds.RemoveAt(
                    questionIds.Count - 1);
            }
            else
            {
                candidateIds.RemoveAt(
                    candidateIds.Count - 1);
            }
        }

        List<long> inputIds =
            [BeginningOfSentenceTokenId];

        inputIds.AddRange(questionIds);

        inputIds.Add(
            EndOfSentenceTokenId);

        inputIds.Add(
            EndOfSentenceTokenId);

        inputIds.AddRange(candidateIds);

        inputIds.Add(
            EndOfSentenceTokenId);

        long[] attentionMask =
            Enumerable
                .Repeat(1L, inputIds.Count)
                .ToArray();

        return new BgeRelevanceModelInput
        {
            InputIds = inputIds.ToArray(),
            AttentionMask = attentionMask
        };
    }

    private List<long> EncodeContent(
        string text)
    {
        IReadOnlyList<int> sentencePieceIds =
            _tokenizer.EncodeToIds(
                text,
                false,
                false);

        return sentencePieceIds
            .Select(
                ConvertSentencePieceIdToXlmRobertaId)
            .ToList();
    }

    private static long
        ConvertSentencePieceIdToXlmRobertaId(
            int sentencePieceId)
    {
        if (sentencePieceId == 0)
        {
            return UnknownTokenId;
        }

        return sentencePieceId + 1L;
    }
}