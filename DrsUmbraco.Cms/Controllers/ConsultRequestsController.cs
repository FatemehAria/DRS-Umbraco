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

    private const long MaxResumeFileSizeBytes = 5 * 1024 * 1024;

    public ConsultRequestsController(
        IElementorSubmissionService elementorSubmissionService,
        ILogger<ConsultRequestsController> logger)
    {
        _elementorSubmissionService = elementorSubmissionService;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        [FromForm] ConsultRequestCreateModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (IsJobInterestForm(model.FormName))
        {
            var resumeError = await ValidateResumeFileAsync(model.ResumeFile, cancellationToken);

            if (!string.IsNullOrWhiteSpace(resumeError))
            {
                ModelState.AddModelError(nameof(model.ResumeFile), resumeError);
                return ValidationProblem(ModelState);
            }
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

    private static bool IsJobInterestForm(string? formName)
    {
        return string.Equals(
            formName?.Trim(),
            "job_interest_form",
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> ValidateResumeFileAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return "آپلود رزومه الزامی است.";
        }

        if (file.Length > MaxResumeFileSizeBytes)
        {
            return "حجم رزومه نباید بیشتر از ۵ مگابایت باشد.";
        }

        var extension = Path.GetExtension(file.FileName);

        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "رزومه باید در قالب PDF باشد.";
        }

        var header = new byte[4];

        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, 4), cancellationToken);

        var looksLikePdf =
            read == 4 &&
            header[0] == '%' &&
            header[1] == 'P' &&
            header[2] == 'D' &&
            header[3] == 'F';

        if (!looksLikePdf)
        {
            return "فایل انتخاب‌شده PDF معتبر نیست.";
        }

        return null;
    }
}

