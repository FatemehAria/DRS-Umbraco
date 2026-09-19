using DrsUmbraco.Cms.Features.ConsultRequests.Contracts;
using DrsUmbraco.Cms.Features.ConsultRequests.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace DrsUmbraco.Cms.Features.ConsultRequests.Controllers;

[ApiExplorerSettings(GroupName = "Consult requests")]
[VersionedApiBackOfficeRoute("consult-requests")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[Authorize(Policy = ConsultRequestAuthorizationPolicies.BackofficeAccess)]
public sealed class JobApplicationsManagementController : ManagementApiControllerBase
{
    private readonly IConsultRequestQueryService _queryService;

    public JobApplicationsManagementController(IConsultRequestQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet("job-applications")]
    [ProducesResponseType<IReadOnlyList<JobApplicationSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobApplications(
            [FromQuery] int take = 50,
            CancellationToken cancellationToken = default)
    {
        var applications = await _queryService.GetLatestJobApplicationsAsync(take, cancellationToken);

        return Ok(applications);
    }

    [HttpGet("job-applications/{submissionId:decimal}/resume")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadResume(
            decimal submissionId,
            CancellationToken cancellationToken = default)
    {
        if (submissionId <= 0)
        {
            return NotFound();
        }

        var resume = await _queryService.GetResumeDownloadAsync(submissionId, cancellationToken);

        if (resume is null)
        {
            return NotFound();
        }

        Response.Headers["Cache-Control"] = "private, no-store";

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return PhysicalFile(
            resume.PhysicalPath,
            contentType: "application/pdf",
            fileDownloadName: resume.DownloadFileName,
            enableRangeProcessing: false);
    }
}