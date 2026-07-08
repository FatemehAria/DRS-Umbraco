using DrsUmbraco.Cms.Models;
using DrsUmbraco.Cms.Services;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Controllers;

[ApiController]
[Route("api/consult-requests")]
public sealed class ConsultRequestsController : ControllerBase
{
    private readonly IElementorSubmissionService _elementorSubmissionService;
    private readonly ILogger<ConsultRequestsController> _logger;

    public ConsultRequestsController(
        IElementorSubmissionService elementorSubmissionService,
        ILogger<ConsultRequestsController> logger)
    {
        _elementorSubmissionService = elementorSubmissionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ConsultRequestCreateModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var referer = Request.Headers.Referer.ToString();

            if (string.IsNullOrWhiteSpace(referer))
            {
                referer = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            }

            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var id = await _elementorSubmissionService.CreateConsultRequestAsync(
                model,
                referer,
                refererTitle: null,
                ipAddress,
                userAgent,
                cancellationToken);

            return Ok(new
            {
                success = true,
                id,
                message = "درخواست شما با موفقیت ثبت شد."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating consult request.");

            return Problem(
                title: "خطا در ثبت درخواست",
                detail: "در حال حاضر امکان ثبت درخواست وجود ندارد. لطفاً بعداً دوباره تلاش کنید.");
        }
    }
}