using DrsUmbraco.Cms.Features.ConsultRequests.Contracts;

namespace DrsUmbraco.Cms.Features.ConsultRequests;

public interface IConsultRequestQueryService
{
    Task<IReadOnlyList<JobApplicationSummary>>
        GetLatestJobApplicationsAsync(
            int take,
            CancellationToken cancellationToken = default);

    Task<ResumeDownload?>
        GetResumeDownloadAsync(
            decimal submissionId,
            CancellationToken cancellationToken = default);
}