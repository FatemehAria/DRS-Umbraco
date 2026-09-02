using Microsoft.ML.Tokenizers;
using System.Diagnostics;

namespace DrsUmbraco.Cms.Features.Chatbot.Embeddings;

public sealed class XlmRobertaEmbeddingTokenizer
    : ILocalEmbeddingTokenizer
{
    private readonly SentencePieceTokenizer _tokenizer;
    private readonly ILogger<XlmRobertaEmbeddingTokenizer> _logger;
    private const long BeginningOfSentenceTokenId = 0;
    private const long EndOfSentenceTokenId = 2;
    private const long UnknownTokenId = 3;

    private const int MaxSequenceLength = 512;
    private const int MaxContentTokenCount = MaxSequenceLength - 2;
    public XlmRobertaEmbeddingTokenizer(
        IWebHostEnvironment environment,
        ILogger<XlmRobertaEmbeddingTokenizer> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        // 1. مسیر sentencepiece.bpe.model
        string tokenizerPath = Path.Combine(
            environment.ContentRootPath,
            "AIModel",
            "multilingual-e5-small",
            "sentencepiece.bpe.model");
        // 2. بررسی وجود فایل
        if (!File.Exists(tokenizerPath))
        {
            throw new FileNotFoundException(
                "Embedding tokenizer model was not found.",
                tokenizerPath);
        }

        Stopwatch tokenizerLoadStopwatch = Stopwatch.StartNew();

        // 3. باز کردن فایل
        using FileStream stream =
            File.OpenRead(tokenizerPath);

        _tokenizer = SentencePieceTokenizer.Create(
            stream,
            false,
            false);

        tokenizerLoadStopwatch.Stop();

        _logger.LogInformation(
            "Performance metric {MetricName} completed in {ElapsedMs} ms.",
            "E5TokenizerLoad",
            tokenizerLoadStopwatch.ElapsedMilliseconds);
        // 4. ساخت SentencePieceTokenizer

    }

    public EmbeddingModelInput Encode(string text)
    {
        // بررسی ورودی
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Text is required.",
                nameof(text));
        }
        //صدا زدن SentencePiece
        IReadOnlyList<int> sentencePieceIds =
            _tokenizer.EncodeToIds(
                text,
                false,
                false);

        IEnumerable<int> truncatedIds = sentencePieceIds.Take(MaxContentTokenCount);
        List<long> inputIds =
        [
            BeginningOfSentenceTokenId
        ];

        foreach (int sentencePieceId in truncatedIds)
        {
            long xlmRobertaId = ConvertSentencePieceIdToXlmRobertaId(sentencePieceId);

            inputIds.Add(xlmRobertaId);
        }

        inputIds.Add(EndOfSentenceTokenId);

        long[] attentionMask = Enumerable.Repeat(1L, inputIds.Count).ToArray();

        long[] tokenTypeIds =
            new long[inputIds.Count];

        return new EmbeddingModelInput
        {
            InputIds = inputIds.ToArray(),
            AttentionMask = attentionMask,
            TokenTypeIds = tokenTypeIds
        };
    }

    private static long ConvertSentencePieceIdToXlmRobertaId(
    int sentencePieceId)
    {
        if (sentencePieceId == 0)
        {
            return UnknownTokenId;
        }

        return sentencePieceId + 1L;
    }
}