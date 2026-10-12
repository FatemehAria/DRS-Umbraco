using DrsUmbraco.Cms.Features.ConsultRequests;
using DrsUmbraco.Cms.Features.ConsultRequests.Contracts;
using DrsUmbraco.Cms.Features.ConsultRequests.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Tests.Controllers;

public sealed class JobApplicationsManagementControllerTests
{
    [Fact]
    public async Task GetJobApplications_returns_results_from_query_service()
    {
        IReadOnlyList<JobApplicationSummary> expected =
        [
            new JobApplicationSummary(
                Id: 12m,
                FullName: "فاطمه آریا",
                Mobile: "09123456789",
                RequestType: "فرصت شغلی",
                Message: null,
                CreatedAt: new DateTime(2026, 10, 12),
                HasResume: true)
        ];

        var queryService = new FakeConsultRequestQueryService
        {
            Applications = expected
        };

        var controller = new JobApplicationsManagementController(queryService);

        var result = await controller.GetJobApplications(
            take: 25,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
        Assert.Equal(25, queryService.LastTake);
    }

    [Fact]
    public async Task DownloadResume_with_non_positive_id_returns_not_found()
    {
        var queryService = new FakeConsultRequestQueryService();

        var controller = CreateController(queryService);

        var result = await controller.DownloadResume(
            submissionId: 0,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(0, queryService.GetResumeCallCount);
    }

    [Fact]
    public async Task DownloadResume_when_resume_is_missing_returns_not_found()
    {
        var queryService = new FakeConsultRequestQueryService
        {
            Resume = null
        };

        var controller = CreateController(queryService);

        var result = await controller.DownloadResume(
            submissionId: 10m,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(1, queryService.GetResumeCallCount);
    }

    [Fact]
    public async Task DownloadResume_returns_pdf_with_private_no_store_headers()
    {
        var queryService = new FakeConsultRequestQueryService
        {
            Resume = new ResumeDownload(
                PhysicalPath: @"C:\Resumes\resume-10.pdf",
                DownloadFileName: "resume-10.pdf")
        };

        var controller = CreateController(queryService);

        var result = await controller.DownloadResume(
            submissionId: 10m,
            CancellationToken.None);

        var file = Assert.IsType<PhysicalFileResult>(result);

        Assert.Equal(@"C:\Resumes\resume-10.pdf", file.FileName);
        Assert.Equal("resume-10.pdf", file.FileDownloadName);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.False(file.EnableRangeProcessing);
        Assert.Equal("private, no-store",
            controller.Response.Headers.CacheControl);
        Assert.Equal("nosniff",
            controller.Response.Headers["X-Content-Type-Options"]);
    }

    private static JobApplicationsManagementController CreateController(
        FakeConsultRequestQueryService queryService)
    {
        var controller = new JobApplicationsManagementController(queryService);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private sealed class FakeConsultRequestQueryService
        : IConsultRequestQueryService
    {
        public IReadOnlyList<JobApplicationSummary> Applications { get; init; } =
            Array.Empty<JobApplicationSummary>();

        public ResumeDownload? Resume { get; init; }

        public int LastTake { get; private set; }

        public int GetResumeCallCount { get; private set; }

        public Task<IReadOnlyList<JobApplicationSummary>>
            GetLatestJobApplicationsAsync(
                int take,
                CancellationToken cancellationToken = default)
        {
            LastTake = take;

            return Task.FromResult(Applications);
        }

        public Task<ResumeDownload?> GetResumeDownloadAsync(
            decimal submissionId,
            CancellationToken cancellationToken = default)
        {
            GetResumeCallCount++;

            return Task.FromResult(Resume);
        }
    }
}