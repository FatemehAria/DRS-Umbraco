using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class ChatbotMessageServiceTests
{
    [Fact]
    public void Process_WhenExactMatchExists_ShouldReturnExactAnswer()
    {
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = true,
                    KnowledgeItemId = Guid.NewGuid(),
                    Answer = "Exact answer"
                });

        FakeSemanticSearchService semanticSearchService =
            new(null);

        FakeSemanticDecisionService semanticDecisionService =
            new(
                new ChatbotSemanticDecision
                {
                    Type = ChatbotSemanticDecisionType.NoMatch
                });

        FakeClarificationExactMatchingService
            clarificationExactMatchingService =
                new(
                    new ChatbotMatchResult
                    {
                        IsMatch = false
                    });

        ChatbotMessageService service =
        new(
            matchingService,
            semanticSearchService,
            semanticDecisionService,
            clarificationExactMatchingService);

        ChatbotMessageResult result =
            service.Process("Question");

        Assert.Equal(
            "Exact answer",
            result.Reply);

        Assert.Equal(
            0,
            semanticSearchService.CallCount);

        Assert.Equal(
            0,
            semanticDecisionService.CallCount);
    }

    [Fact]
    public void Process_WhenSemanticResultIsConfident_ShouldReturnSemanticAnswer()
    {
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        ChatbotSemanticSearchResult searchResult =
            CreateSearchResult(
                answer: "Semantic answer");

        FakeSemanticSearchService semanticSearchService =
            new(searchResult);

        FakeSemanticDecisionService semanticDecisionService =
            new(
                new ChatbotSemanticDecision
                {
                    Type =
                        ChatbotSemanticDecisionType.Confident,
                    SearchResult = searchResult
                });

        FakeClarificationExactMatchingService
            clarificationExactMatchingService =
                new(
                    new ChatbotMatchResult
                    {
                        IsMatch = false
                    });

        ChatbotMessageService service =
            new(
                matchingService,
                semanticSearchService,
                semanticDecisionService,
                clarificationExactMatchingService);

        ChatbotMessageResult result =
            service.Process("Question");

        Assert.Equal(
            "Semantic answer",
            result.Reply);

        Assert.Equal(
            1,
            semanticSearchService.CallCount);

        Assert.Equal(
            1,
            semanticDecisionService.CallCount);
    }

    [Fact]
    public void Process_WhenSemanticResultIsAmbiguous_ShouldReturnClarificationMessage()
    {
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        ChatbotSemanticSearchResult searchResult =
            CreateSearchResult(
                answer: "Some answer");

        FakeSemanticSearchService semanticSearchService =
            new(searchResult);

        FakeSemanticDecisionService semanticDecisionService =
            new(
                new ChatbotSemanticDecision
                {
                    Type =
                        ChatbotSemanticDecisionType.Ambiguous,
                    SearchResult = searchResult
                });

        FakeClarificationExactMatchingService
            clarificationExactMatchingService =
                new(
                    new ChatbotMatchResult
                    {
                        IsMatch = false
                    });

        ChatbotMessageService service =
            new(
                matchingService,
                semanticSearchService,
                semanticDecisionService,
                clarificationExactMatchingService);

        ChatbotMessageResult result =
            service.Process("Question");

        Assert.Equal(
            "سؤال شما به چند موضوع نزدیک است. لطفاً کمی دقیق‌تر توضیح دهید.",
            result.Reply);
    }

    [Fact]
    public void Process_WhenNoMatchExists_ShouldReturnFallbackMessage()
    {
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeSemanticSearchService semanticSearchService =
            new(null);

        FakeSemanticDecisionService semanticDecisionService =
            new(
                new ChatbotSemanticDecision
                {
                    Type =
                        ChatbotSemanticDecisionType.NoMatch
                });

        FakeClarificationExactMatchingService
            clarificationExactMatchingService =
                new(
                    new ChatbotMatchResult
                    {
                        IsMatch = false
                    });

        ChatbotMessageService service =
            new(
                matchingService,
                semanticSearchService,
                semanticDecisionService,
                clarificationExactMatchingService);

        ChatbotMessageResult result =
            service.Process("Unknown question");

        Assert.Equal(
            "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید.",
            result.Reply);
    }

    [Fact]
    public void Process_WhenExactClarificationExists_ShouldReturnClarificationAnswer()
    {
        // Arrange
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeClarificationExactMatchingService
            clarificationExactMatchingService =
                new(
                    new ChatbotMatchResult
                    {
                        IsMatch = true,
                        KnowledgeItemId = Guid.NewGuid(),
                        Answer = "Clarification answer"
                    });

        FakeSemanticSearchService semanticSearchService =
            new(null);

        FakeSemanticDecisionService semanticDecisionService =
            new(
                new ChatbotSemanticDecision
                {
                    Type =
                        ChatbotSemanticDecisionType.NoMatch
                });

        ChatbotMessageService service =
            new(
                matchingService,
                semanticSearchService,
                semanticDecisionService,
                clarificationExactMatchingService);

        // Act
        ChatbotMessageResult result =
            service.Process("Question");

        // Assert
        Assert.Equal(
            "Clarification answer",
            result.Reply);

        Assert.Equal(
            1,
            clarificationExactMatchingService.CallCount);

        Assert.Equal(
            0,
            semanticSearchService.CallCount);

        Assert.Equal(
            0,
            semanticDecisionService.CallCount);
    }

    private sealed class FakeClarificationExactMatchingService
        : IChatbotClarificationExactMatchingService
    {
        private readonly ChatbotMatchResult _result;

        public FakeClarificationExactMatchingService(
            ChatbotMatchResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public ChatbotMatchResult Find(
            string question)
        {
            CallCount++;

            return _result;
        }
    }

    private static ChatbotSemanticSearchResult CreateSearchResult(
        string answer)
    {
        return new ChatbotSemanticSearchResult
        {
            KnowledgeItemId = Guid.NewGuid(),
            Answer = answer,
            MatchedText = "Matched question",
            Score = 0.95f,
            Margin = 0.10f
        };
    }

    private sealed class FakeMatchingService
        : IChatbotMatchingService
    {
        private readonly ChatbotMatchResult _result;

        public FakeMatchingService(
            ChatbotMatchResult result)
        {
            _result = result;
        }

        public ChatbotMatchResult FindMatch(
            string? question)
        {
            return _result;
        }
    }

    private sealed class FakeSemanticSearchService
        : IChatbotSemanticSearchService
    {
        private readonly ChatbotSemanticSearchResult? _result;

        public FakeSemanticSearchService(
            ChatbotSemanticSearchResult? result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public ChatbotSemanticSearchResult? FindBest(
            string question,
            ChatbotKnowledgeItemKind? kind = null)
        {
            CallCount++;

            return _result;
        }
    }

    private sealed class FakeSemanticDecisionService
        : IChatbotSemanticDecisionService
    {
        private readonly ChatbotSemanticDecision _decision;

        public FakeSemanticDecisionService(
            ChatbotSemanticDecision decision)
        {
            _decision = decision;
        }

        public int CallCount { get; private set; }

        public ChatbotSemanticDecision Decide(
            ChatbotSemanticSearchResult? result)
        {
            CallCount++;

            return _decision;
        }
    }
}