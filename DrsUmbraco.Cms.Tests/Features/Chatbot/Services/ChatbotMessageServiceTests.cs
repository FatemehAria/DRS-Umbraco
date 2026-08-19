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

        FakeCandidateEvidenceService candidateEvidenceService =
            new();

        FakeKnowledgeService knowledgeService =
            new();

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

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
    public void Process_WhenNoCandidatesExist_ShouldReturnFallbackMessage()
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

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        FakeCandidateEvidenceService candidateEvidenceService =
            new([]);

        FakeKnowledgeService knowledgeService =
            new();

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

        // Act
        ChatbotMessageResult result =
            service.Process("Unknown question");

        // Assert
        Assert.Equal(
            ChatbotResponseType.Fallback,
            result.ResponseType);

        Assert.Equal(
            "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید.",
            result.Reply);

        Assert.Equal(
            1,
            candidateEvidenceService.CallCount);

        // چون candidate نداریم، اصلاً نباید به NoMatchDecision برسیم.
        Assert.Equal(
            0,
            noMatchService.CallCount);
    }

    [Fact]
    public void Process_WhenNoMatchDecisionIsNoMatch_ShouldReturnFallbackMessage()
    {
        // Arrange
        Guid candidateId =
            Guid.NewGuid();

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

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.NoMatch);

        FakeCandidateEvidenceService candidateEvidenceService =
            new(
                [
                    new ChatbotCandidateEvidence
                {
                    KnowledgeItemId = candidateId,
                    Answer = "Some answer",
                    SemanticRank = 1,
                    SemanticScore = 0.80f
                }
                ]);

        FakeKnowledgeService knowledgeService =
            new();

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

        // Act
        ChatbotMessageResult result =
            service.Process(
                "Out of domain question");

        // Assert
        Assert.Equal(
            ChatbotResponseType.Fallback,
            result.ResponseType);

        Assert.Equal(
            "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید.",
            result.Reply);

        Assert.Equal(
            1,
            candidateEvidenceService.CallCount);

        Assert.Equal(
            1,
            noMatchService.CallCount);

        // چون NoMatch شد، نباید اصلاً وارد ساخت Suggestions شویم.
        Assert.Equal(
            0,
            knowledgeService.GetByIdCallCount);
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

        FakeCandidateEvidenceService candidateEvidenceService =
            new();

        FakeKnowledgeService knowledgeService =
            new();

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

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

    [Fact]
    public void Process_WhenNonExactQuestionIsInDomain_ShouldReturnTopThreeSuggestions()
    {
        // Arrange
        Guid mobileId = Guid.NewGuid();
        Guid emailId = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();
        Guid passwordId = Guid.NewGuid();

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
                        IsMatch = false
                    });

        FakeRerankingService rerankingService =
            new(
                CreateRerankingResult(
                    answer: "Some candidate answer",
                    selectedStrategy: "Centroid"));

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        FakeCandidateEvidenceService
            candidateEvidenceService =
                new(
                    [
                        new ChatbotCandidateEvidence
                    {
                        KnowledgeItemId = mobileId,
                        Answer = "Mobile answer"
                    },
                    new ChatbotCandidateEvidence
                    {
                        KnowledgeItemId = emailId,
                        Answer = "Email answer"
                    },
                    new ChatbotCandidateEvidence
                    {
                        KnowledgeItemId = profileId,
                        Answer = "Profile answer"
                    },
                    new ChatbotCandidateEvidence
                    {
                        KnowledgeItemId = passwordId,
                        Answer = "Password answer"
                    }
                    ]);

        FakeKnowledgeService knowledgeService =
            new(
                [
                    new ChatbotKnowledgeItem
                {
                    Id = mobileId,
                    Question =
                        "چطور شماره موبایل حسابم را تغییر بدهم؟",
                    Answer = "Mobile answer",
                    Kind = ChatbotKnowledgeItemKind.Answer
                },

                new ChatbotKnowledgeItem
                {
                    Id = emailId,
                    Question =
                        "چطور ایمیل حساب کاربری را تغییر بدهم؟",
                    Answer = "Email answer",
                    Kind = ChatbotKnowledgeItemKind.Answer
                },

                new ChatbotKnowledgeItem
                {
                    Id = profileId,
                    Question =
                        "چطور اطلاعات پروفایلم را ویرایش کنم؟",
                    Answer = "Profile answer",
                    Kind = ChatbotKnowledgeItemKind.Answer
                },

                new ChatbotKnowledgeItem
                {
                    Id = passwordId,
                    Question =
                        "چطور رمز عبورم را تغییر بدهم؟",
                    Answer = "Password answer",
                    Kind = ChatbotKnowledgeItemKind.Answer
                }
                ]);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

        // Act
        ChatbotMessageResult result =
            service.Process(
                "شماره حسابم نیاز به تغییر دارد");

        // Assert
        Assert.Equal(
            ChatbotResponseType.Suggestions,
            result.ResponseType);

        Assert.Equal(
            "منظورتان کدام مورد است؟",
            result.Reply);

        Assert.Equal(
            3,
            result.Suggestions.Count);

        Assert.Equal(
            mobileId,
            result.Suggestions[0].KnowledgeItemId);

        Assert.Equal(
            "چطور شماره موبایل حسابم را تغییر بدهم؟",
            result.Suggestions[0].Label);

        Assert.Equal(
            emailId,
            result.Suggestions[1].KnowledgeItemId);

        Assert.Equal(
            "چطور ایمیل حساب کاربری را تغییر بدهم؟",
            result.Suggestions[1].Label);

        Assert.Equal(
            profileId,
            result.Suggestions[2].KnowledgeItemId);

        Assert.Equal(
            "چطور اطلاعات پروفایلم را ویرایش کنم؟",
            result.Suggestions[2].Label);

        Assert.Equal(
            1,
            candidateEvidenceService.CallCount);

        Assert.Equal(
            3,
            candidateEvidenceService.LastLimit);

        Assert.Equal(
            ChatbotKnowledgeItemKind.Answer,
            candidateEvidenceService.LastKind);

        Assert.Equal(
            3,
            knowledgeService.GetByIdCallCount);
    }

    [Fact]
    public void Process_WhenInDomainButNoSuggestionsExist_ShouldReturnFallback()
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
                        IsMatch = false
                    });

        FakeRerankingService rerankingService =
            new(
                CreateRerankingResult(
                    answer: "Some answer",
                    selectedStrategy: "Agreement"));

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        FakeCandidateEvidenceService candidateEvidenceService =
            new([]);

        FakeKnowledgeService knowledgeService =
            new([]);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

        // Act
        ChatbotMessageResult result =
            service.Process("Question");

        // Assert
        Assert.Equal(
            ChatbotResponseType.Fallback,
            result.ResponseType);

        Assert.Equal(
            "پاسخ دقیقی برای سؤال شما پیدا نکردم. لطفاً با پشتیبانی تماس بگیرید.",
            result.Reply);

        Assert.Empty(
            result.Suggestions);
    }

    [Fact]
    public void SelectSuggestion_WhenKnowledgeItemDoesNotExist_ShouldReturnNull()
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

        FakeCandidateEvidenceService candidateEvidenceService =
            new();

        FakeKnowledgeService knowledgeService =
            new([]);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

        // Act
        ChatbotMessageResult? result =
            service.SelectSuggestion(
                Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SelectSuggestion_WhenItemIsClarification_ShouldReturnNull()
    {
        // Arrange
        Guid clarificationId = Guid.NewGuid();

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

        FakeCandidateEvidenceService candidateEvidenceService =
            new();

        FakeKnowledgeService knowledgeService =
            new(
                [
                    new ChatbotKnowledgeItem
                {
                    Id = clarificationId,
                    Question = "Clarification question",
                    Answer = "Clarification answer",
                    Kind =
                        ChatbotKnowledgeItemKind.Clarification
                }
                ]);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

        // Act
        ChatbotMessageResult? result =
            service.SelectSuggestion(
                clarificationId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SelectSuggestion_WhenAnswerExists_ShouldReturnAnswer()
    {
        // Arrange
        Guid answerId = Guid.NewGuid();

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

        FakeCandidateEvidenceService candidateEvidenceService =
            new();

        FakeKnowledgeService knowledgeService =
            new(
                [
                    new ChatbotKnowledgeItem
                {
                    Id = answerId,
                    Question =
                        "چطور شماره موبایل حسابم را تغییر بدهم؟",
                    Answer =
                        "برای تغییر شماره موبایل وارد تنظیمات حساب کاربری شوید.",
                    Kind =
                        ChatbotKnowledgeItemKind.Answer
                }
                ]);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                new FakeRelevanceVerifier(true));

        // Act
        ChatbotMessageResult? result =
            service.SelectSuggestion(answerId);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            ChatbotResponseType.Answer,
            result.ResponseType);

        Assert.Equal(
            "برای تغییر شماره موبایل وارد تنظیمات حساب کاربری شوید.",
            result.Reply);

        Assert.Empty(
            result.Suggestions);
    }

    [Fact]
    public void Process_WhenVerifierRejectsAllCandidates_ShouldReturnFallback()
    {
        // Arrange
        Guid candidateId =
            Guid.NewGuid();

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
                        IsMatch = false
                    });

        FakeNoMatchDecisionService noMatchService =
            new(ChatbotNoMatchDecision.InDomain);

        FakeCandidateEvidenceService
            candidateEvidenceService =
                new(
                    [
                        new ChatbotCandidateEvidence
                    {
                        KnowledgeItemId = candidateId,
                        Answer = "Support answer",
                        SemanticRank = 1,
                        SemanticScore = 0.90f
                    }
                    ]);

        FakeKnowledgeService knowledgeService =
            new(
                [
                    new ChatbotKnowledgeItem
                {
                    Id = candidateId,
                    Question =
                        "چطور با پشتیبانی تماس بگیرم؟",
                    Answer =
                        "Support answer",
                    Kind =
                        ChatbotKnowledgeItemKind.Answer
                }
                ]);

        FakeRelevanceVerifier relevanceVerifier =
            new(false);

        ChatbotMessageService service =
            new(
                matchingService,
                clarificationService,
                noMatchService,
                candidateEvidenceService,
                knowledgeService,
                relevanceVerifier);

        // Act
        ChatbotMessageResult result =
            service.Process(
                "چه مرورگرهایی برای استفاده از سایت پشتیبانی می‌شوند؟");

        // Assert
        Assert.Equal(
            ChatbotResponseType.Fallback,
            result.ResponseType);

        Assert.Empty(result.Suggestions);

        Assert.Equal(
            1,
            relevanceVerifier.CallCount);
    }
    
    private sealed class FakeRelevanceVerifier
    : IChatbotRelevanceVerifier
    {
        private readonly bool _isRelevant;

        public FakeRelevanceVerifier(
            bool isRelevant)
        {
            _isRelevant = isRelevant;
        }

        public int CallCount { get; private set; }

        public bool IsRelevant(
            string question,
            ChatbotKnowledgeItem candidate)
        {
            CallCount++;

            return _isRelevant;
        }
    }

    private sealed class FakeCandidateEvidenceService
    : IChatbotCandidateEvidenceService
    {
        private readonly IReadOnlyList<ChatbotCandidateEvidence>
            _results;

        public FakeCandidateEvidenceService(
            IReadOnlyList<ChatbotCandidateEvidence>? results = null)
        {
            _results = results ?? [];
        }

        public int CallCount { get; private set; }

        public int? LastLimit { get; private set; }

        public ChatbotKnowledgeItemKind? LastKind
        {
            get;
            private set;
        }

        public IReadOnlyList<ChatbotCandidateEvidence> Find(
            string question,
            int limit,
            ChatbotKnowledgeItemKind? kind = null)
        {
            CallCount++;

            LastLimit = limit;
            LastKind = kind;

            return _results;
        }
    }

    private sealed class FakeKnowledgeService
    : IChatbotKnowledgeService
    {
        private readonly IReadOnlyList<ChatbotKnowledgeItem>
            _items;

        public FakeKnowledgeService(
            IReadOnlyList<ChatbotKnowledgeItem>? items = null)
        {
            _items = items ?? [];
        }

        public int GetAllCallCount { get; private set; }

        public int GetByIdCallCount { get; private set; }

        public IReadOnlyList<ChatbotKnowledgeItem> GetAll()
        {
            GetAllCallCount++;

            return _items;
        }

        public ChatbotKnowledgeItem? GetById(
            Guid id)
        {
            GetByIdCallCount++;

            return _items.FirstOrDefault(
                item => item.Id == id);
        }
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