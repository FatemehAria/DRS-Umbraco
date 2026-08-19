using Microsoft.ML.Tokenizers;

public sealed class XlmRobertaPairTokenizer
{
    private readonly SentencePieceTokenizer _tokenizer;

    private const long BeginningOfSentenceTokenId = 0;
    private const long EndOfSentenceTokenId = 2;
    private const long UnknownTokenId = 3;

    private const int MaxSequenceLength = 512;

    // <s> A </s></s> B </s>
    // چهار special token داریم.
    private const int MaxContentTokenCount =
        MaxSequenceLength - 4;

    public XlmRobertaPairTokenizer(
        string tokenizerPath)
    {
        if (!File.Exists(tokenizerPath))
        {
            throw new FileNotFoundException(
                "Tokenizer model was not found.",
                tokenizerPath);
        }

        using FileStream stream =
            File.OpenRead(tokenizerPath);

        _tokenizer =
            SentencePieceTokenizer.Create(
                stream,
                false,
                false);
    }

    public PairModelInput Encode(
        string question,
        string candidateFaq)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question is required.",
                nameof(question));
        }

        if (string.IsNullOrWhiteSpace(candidateFaq))
        {
            throw new ArgumentException(
                "Candidate FAQ is required.",
                nameof(candidateFaq));
        }

        List<long> questionIds =
            EncodeContent(question);

        List<long> faqIds =
            EncodeContent(candidateFaq);

        // Longest-first truncation.
        while (
            questionIds.Count + faqIds.Count >
            MaxContentTokenCount)
        {
            if (questionIds.Count >= faqIds.Count)
            {
                questionIds.RemoveAt(
                    questionIds.Count - 1);
            }
            else
            {
                faqIds.RemoveAt(
                    faqIds.Count - 1);
            }
        }

        List<long> inputIds =
        [
            BeginningOfSentenceTokenId
        ];

        inputIds.AddRange(questionIds);

        inputIds.Add(
            EndOfSentenceTokenId);

        inputIds.Add(
            EndOfSentenceTokenId);

        inputIds.AddRange(faqIds);

        inputIds.Add(
            EndOfSentenceTokenId);

        long[] attentionMask =
            Enumerable
                .Repeat(1L, inputIds.Count)
                .ToArray();

        return new PairModelInput
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

public sealed class PairModelInput
{
    public required long[] InputIds { get; init; }

    public required long[] AttentionMask { get; init; }
}