namespace DrsUmbraco.Cms.Features.ConsultRequests.Configuration;

public sealed class ConsultRequestOptions
{
    public const string SectionName = "ConsultRequests";

    public string ResumeStoragePath { get; init; } = string.Empty;

    public Guid[] AllowedBackofficeGroupKeys { get; init; } = [];
}