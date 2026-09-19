namespace DrsUmbraco.Cms.Features.ConsultRequests.Contracts;

public sealed record JobApplicationSummary(
    decimal Id,
    string FullName,
    string Mobile,
    string RequestType,
    string? Message,
    DateTime CreatedAt,
    bool HasResume);