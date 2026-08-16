using System.Text.Json.Serialization;

namespace DrsUmbraco.Cms.Features.Chatbot.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatbotResponseType
{
    Answer,
    Clarification,
    Suggestions,
    Fallback
}