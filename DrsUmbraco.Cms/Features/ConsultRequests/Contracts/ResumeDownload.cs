namespace DrsUmbraco.Cms.Features.ConsultRequests.Contracts;

public sealed record ResumeDownload(
    string PhysicalPath,
    string DownloadFileName);