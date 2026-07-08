using DrsUmbraco.Cms.Models;

namespace DrsUmbraco.Cms.Services;

public interface IElementorSubmissionService
{
    Task<decimal> CreateConsultRequestAsync(
        ConsultRequestCreateModel model,
        string referer,
        string? refererTitle,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}