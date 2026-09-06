using DrsUmbraco.Cms.Features.Chatbot.Contracts;
using DrsUmbraco.Cms.Features.Chatbot.Controllers;
using DrsUmbraco.Cms.Features.Chatbot.Models;
using DrsUmbraco.Cms.Features.Chatbot.Readiness;
using DrsUmbraco.Cms.Features.Chatbot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Tests.Features.Chatbot.Controllers;

public sealed class ChatbotControllerTests
{
    [Fact]
    public void Send_WhenSemanticIndexIsUnavailable_ShouldReturn503()
    {
        // Arrange
        FakeChatbotMessageService messageService =
            new(
                new ChatbotMessageResult
                {
                    ResponseType =
                        ChatbotResponseType.Fallback,

                    Reply =
                        "This result must not be returned."
                });

        ChatbotSemanticIndexReadiness readiness =
            new();

        ChatbotController controller =
            CreateController(
                messageService,
                readiness);

        SendMessageRequest request =
            new()
            {
                Message =
                    "چطور رمز عبورم را تغییر بدهم؟"
            };

        // Act
        ActionResult<SendMessageResponse> actionResult =
            controller.Send(request);

        // Assert
        ObjectResult response =
            Assert.IsType<ObjectResult>(
                actionResult.Result);

        Assert.Equal(
            StatusCodes.Status503ServiceUnavailable,
            response.StatusCode);

        Assert.Equal(
            "5",
            controller.Response.Headers[
                "Retry-After"].ToString());

        Assert.Equal(
            0,
            messageService.ProcessCallCount);
    }

    [Fact]
    public void Send_WhenSemanticIndexIsReady_ShouldProcessMessage()
    {
        // Arrange
        FakeChatbotMessageService messageService =
            new(
                new ChatbotMessageResult
                {
                    ResponseType =
                        ChatbotResponseType.Answer,

                    Reply =
                        "Current answer"
                });

        ChatbotSemanticIndexReadiness readiness =
            new();

        readiness.MarkReady();

        ChatbotController controller =
            CreateController(
                messageService,
                readiness);

        SendMessageRequest request =
            new()
            {
                Message =
                    "چطور رمز عبورم را تغییر بدهم؟"
            };

        // Act
        ActionResult<SendMessageResponse> actionResult =
            controller.Send(request);

        // Assert
        OkObjectResult response =
            Assert.IsType<OkObjectResult>(
                actionResult.Result);

        SendMessageResponse body =
            Assert.IsType<SendMessageResponse>(
                response.Value);

        Assert.Equal(
            "Current answer",
            body.Reply);

        Assert.Equal(
            ChatbotResponseType.Answer,
            body.ResponseType);

        Assert.Equal(
            1,
            messageService.ProcessCallCount);
    }

    private static ChatbotController CreateController(
        IChatbotMessageService messageService,
        IChatbotSemanticIndexReadiness readiness)
    {
        ChatbotController controller =
            new(
                messageService,
                readiness);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext()
            };

        return controller;
    }

    private sealed class FakeChatbotMessageService
        : IChatbotMessageService
    {
        private readonly ChatbotMessageResult
            _processResult;

        public FakeChatbotMessageService(
            ChatbotMessageResult processResult)
        {
            _processResult =
                processResult;
        }

        public int ProcessCallCount
        {
            get;
            private set;
        }

        public ChatbotMessageResult Process(
            string message)
        {
            ProcessCallCount++;

            return _processResult;
        }

        public ChatbotMessageResult? SelectSuggestion(
            Guid knowledgeItemId)
        {
            return null;
        }
    }
}