using DrsUmbraco.Cms.Features.Chatbot.Embeddings;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Services;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Services;

public sealed class ChatbotMessageServiceTests
{
    [Fact]
    public void Process_WhenExactMatchExists_ShouldReturnExactAnswer()
    {
        // Arrange
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = true,
                    KnowledgeItemId = Guid.NewGuid(),
                    Answer = "Exact answer"
                });

        FakeClarificationExactMatchingService
            clarificationService =
                new(
                    new ChatbotMatchResult
                    {
                        IsMatch = false
                    });

        FakeRerankingService rerankingService =
            new(null);

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                rerankingService,
                noMatchService);

        // Act
        ChatbotMessageResult result =
            service.Process("Question");

        // Assert
        Assert.Equal(
            "Exact answer",
            result.Reply);

        Assert.Equal(
            0,
            clarificationService.CallCount);

        Assert.Equal(
            0,
            rerankingService.CallCount);

        Assert.Equal(
            0,
            noMatchService.CallCount);
    }

    [Fact]
    public void Process_WhenRerankerAgrees_ShouldReturnRerankedAnswer()
    {
        // Arrange
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeClarificationExactMatchingService clarificationService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeRerankingService rerankingService =
            new(
                CreateRerankingResult(
                    answer: "Reranked answer",
                    selectedStrategy: "Agreement"));

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                rerankingService,
                noMatchService);

        // Act
        ChatbotMessageResult result =
            service.Process("Question");

        // Assert
        Assert.Equal(
            "Reranked answer",
            result.Reply);

        Assert.Equal(
            1,
            rerankingService.CallCount);

        Assert.Equal(
            1,
            noMatchService.CallCount);
    }

    [Fact]
    public void Process_WhenRerankerDoesNotAgree_ShouldReturnClarificationMessage()
    {
        // Arrange
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeClarificationExactMatchingService clarificationService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeRerankingService rerankingService =
            new(
                CreateRerankingResult(
                    answer: "Some answer",
                    selectedStrategy: "Centroid"));

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                rerankingService,
                noMatchService);

        // Act
        ChatbotMessageResult result =
            service.Process("Question");

        // Assert
        Assert.Equal(
            "سؤال شما به چند موضوع نزدیک است. لطفاً کمی دقیق‌تر توضیح دهید.",
            result.Reply);
    }

    [Fact]
    public void Process_WhenRerankerReturnsNull_ShouldReturnFallbackMessage()
    {
        // Arrange
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeClarificationExactMatchingService clarificationService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeRerankingService rerankingService =
            new(null);

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                rerankingService,
                noMatchService);

        // Act
        ChatbotMessageResult result =
            service.Process("Unknown question");

        // Assert
        Assert.Equal(
            "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید.",
            result.Reply);

        Assert.Equal(
            1,
            rerankingService.CallCount);

        Assert.Equal(
            0,
            noMatchService.CallCount);
    }

    [Fact]
    public void Process_WhenNoMatchDecisionIsNoMatch_ShouldReturnFallbackMessage()
    {
        // Arrange
        FakeMatchingService matchingService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeClarificationExactMatchingService clarificationService =
            new(
                new ChatbotMatchResult
                {
                    IsMatch = false
                });

        FakeRerankingService rerankingService =
            new(
                CreateRerankingResult(
                    answer: "Some answer",
                    selectedStrategy: "Agreement"));

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.NoMatch);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                rerankingService,
                noMatchService);

        // Act
        ChatbotMessageResult result =
            service.Process("Out of domain question");

        // Assert
        Assert.Equal(
            "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید.",
            result.Reply);

        Assert.Equal(
            1,
            noMatchService.CallCount);
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
            clarificationService =
                new(
                    new ChatbotMatchResult
                    {
                        IsMatch = true,
                        KnowledgeItemId = Guid.NewGuid(),
                        Answer = "Clarification answer"
                    });

        FakeRerankingService rerankingService =
            new(null);

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                rerankingService,
                noMatchService);

        // Act
        ChatbotMessageResult result =
            service.Process("Question");

        // Assert
        Assert.Equal(
            "Clarification answer",
            result.Reply);

        Assert.Equal(
            1,
            clarificationService.CallCount);

        Assert.Equal(
            0,
            rerankingService.CallCount);

        Assert.Equal(
            0,
            noMatchService.CallCount);
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

    private sealed class FakeRerankingService
        : IChatbotRerankingService
    {
        private readonly ChatbotRerankResult? _result;

        public FakeRerankingService(
            ChatbotRerankResult? result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public ChatbotRerankResult? FindBest(
            string question,
            ChatbotKnowledgeItemKind? kind = null)
        {
            CallCount++;

            return _result;
        }
    }

    private sealed class FakeNoMatchDecisionService
        : IChatbotNoMatchDecisionService
    {
        private readonly ChatbotNoMatchDecision _decision;

        public FakeNoMatchDecisionService(
            ChatbotNoMatchDecision decision)
        {
            _decision = decision;
        }

        public int CallCount { get; private set; }

        public ChatbotNoMatchDecision Decide(
            float semanticTopScore)
        {
            CallCount++;

            return _decision;
        }
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

    private static ChatbotRerankResult CreateRerankingResult(
        string answer,
        string selectedStrategy)
    {
        return new ChatbotRerankResult
        {
            KnowledgeItemId = Guid.NewGuid(),
            Answer = answer,
            SelectedStrategy = selectedStrategy,
            SemanticTopScore = 0.95f,
            SemanticMargin = 0.02f,
            CentroidTopScore = 0.94f,
            CentroidMargin = 0.02f
        };
    }
}