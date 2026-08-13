using DrsUmbraco.Cms.Features.Chatbot.Embeddings;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Embeddings;

public sealed class ChatbotSemanticIndexTests
{
    [Fact]
    public void Replace_ShouldReplaceAllCandidates()
    {
        ChatbotSemanticIndex index = new();

        ChatbotSemanticCandidate first =
            CreateCandidate(
                Guid.NewGuid(),
                "Question 1");

        ChatbotSemanticCandidate second =
            CreateCandidate(
                Guid.NewGuid(),
                "Question 2");

        index.Replace([first, second]);

        IReadOnlyList<ChatbotSemanticCandidate> result =
            index.GetAll();

        Assert.Equal(2, result.Count);
        Assert.Contains(first, result);
        Assert.Contains(second, result);
    }

    [Fact]
    public void Replace_WhenCalledAgain_ShouldRemoveOldCandidates()
    {
        ChatbotSemanticIndex index = new();

        ChatbotSemanticCandidate oldCandidate =
            CreateCandidate(
                Guid.NewGuid(),
                "Old question");

        index.Replace([oldCandidate]);

        ChatbotSemanticCandidate newCandidate =
            CreateCandidate(
                Guid.NewGuid(),
                "New question");

        index.Replace([newCandidate]);

        IReadOnlyList<ChatbotSemanticCandidate> result =
            index.GetAll();

        Assert.Single(result);
        Assert.Contains(newCandidate, result);
        Assert.DoesNotContain(oldCandidate, result);
    }

    [Fact]
    public void ReplaceForKnowledgeItem_ShouldReplaceOnlySpecifiedKnowledgeItem()
    {
        ChatbotSemanticIndex index = new();

        Guid passwordFaqId = Guid.NewGuid();
        Guid supportFaqId = Guid.NewGuid();

        ChatbotSemanticCandidate oldPasswordCandidate =
            CreateCandidate(
                passwordFaqId,
                "Old password question");

        ChatbotSemanticCandidate supportCandidate =
            CreateCandidate(
                supportFaqId,
                "Support question");

        index.Replace([
            oldPasswordCandidate,
            supportCandidate
        ]);

        ChatbotSemanticCandidate newPasswordCandidate =
            CreateCandidate(
                passwordFaqId,
                "New password question");

        index.ReplaceForKnowledgeItem(
            passwordFaqId,
            [newPasswordCandidate]);

        IReadOnlyList<ChatbotSemanticCandidate> result =
            index.GetAll();

        Assert.Equal(2, result.Count);

        Assert.Contains(
            newPasswordCandidate,
            result);

        Assert.Contains(
            supportCandidate,
            result);

        Assert.DoesNotContain(
            oldPasswordCandidate,
            result);
    }

    [Fact]
    public void ReplaceForKnowledgeItem_ShouldReplaceAllCandidatesForSameFaq()
    {
        ChatbotSemanticIndex index = new();

        Guid faqId = Guid.NewGuid();

        ChatbotSemanticCandidate oldMain =
            CreateCandidate(
                faqId,
                "Old main");

        ChatbotSemanticCandidate oldAlternative =
            CreateCandidate(
                faqId,
                "Old alternative");

        index.Replace([
            oldMain,
            oldAlternative
        ]);

        ChatbotSemanticCandidate newMain =
            CreateCandidate(
                faqId,
                "New main");

        index.ReplaceForKnowledgeItem(
            faqId,
            [newMain]);

        IReadOnlyList<ChatbotSemanticCandidate> result =
            index.GetAll();

        Assert.Single(result);
        Assert.Contains(newMain, result);
        Assert.DoesNotContain(oldMain, result);
        Assert.DoesNotContain(oldAlternative, result);
    }

    [Fact]
    public void RemoveForKnowledgeItem_ShouldRemoveOnlySpecifiedKnowledgeItem()
    {
        ChatbotSemanticIndex index = new();

        Guid passwordFaqId = Guid.NewGuid();
        Guid supportFaqId = Guid.NewGuid();

        ChatbotSemanticCandidate passwordCandidate =
            CreateCandidate(
                passwordFaqId,
                "Password question");

        ChatbotSemanticCandidate supportCandidate =
            CreateCandidate(
                supportFaqId,
                "Support question");

        index.Replace([
            passwordCandidate,
            supportCandidate
        ]);

        index.RemoveForKnowledgeItem(
            passwordFaqId);

        IReadOnlyList<ChatbotSemanticCandidate> result =
            index.GetAll();

        Assert.Single(result);

        Assert.Contains(
            supportCandidate,
            result);

        Assert.DoesNotContain(
            passwordCandidate,
            result);
    }

    private static ChatbotSemanticCandidate CreateCandidate(
        Guid knowledgeItemId,
        string text)
    {
        return new ChatbotSemanticCandidate
        {
            KnowledgeItemId = knowledgeItemId,
            Text = text,
            Answer = "Test answer",
            Embedding = [1f, 0f]
        };
    }
}