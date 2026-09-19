using DrsUmbraco.Cms.Features.ConsultRequests;
using DrsUmbraco.Cms.Models;
using DrsUmbraco.Cms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DrsUmbraco.Cms.Controllers;

[ApiController]
[Route("api/consult-requests")]
public sealed class ConsultRequestsController : ControllerBase
{
    private readonly IElementorSubmissionService _elementorSubmissionService;
    private readonly ILogger<ConsultRequestsController> _logger;

    private const long MaxResumeFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string>
        AllowedFormNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "contact_form",
            "consult_form",
            "product_demo_form",
            "job_interest_form"
            };

    public ConsultRequestsController(
        IElementorSubmissionService elementorSubmissionService,
        ILogger<ConsultRequestsController> logger)
    {
        _elementorSubmissionService = elementorSubmissionService;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.PublicFormByIp)]
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

        var formName = model.FormName?.Trim();

        if (string.IsNullOrWhiteSpace(formName) || !AllowedFormNames.Contains(formName))
        {
            ModelState.AddModelError(nameof(model.FormName), "نوع فرم معتبر نیست.");

            return ValidationProblem(ModelState);
        }

        formName = formName.ToLowerInvariant();
        model.FormName = formName;

        if (IsJobInterestForm(formName))
        {
            var resumeError = await ValidateResumeFileAsync(model.ResumeFile, cancellationToken);

            if (!string.IsNullOrWhiteSpace(resumeError))
            {
                ModelState.AddModelError(nameof(model.ResumeFile), resumeError);
                return ValidationProblem(ModelState);
            }
        }
        else if (model.ResumeFile is not null)
        {
            ModelState.AddModelError(nameof(model.ResumeFile), "ارسال فایل برای این فرم مجاز نیست.");

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

        var header = new byte[5];

        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        var looksLikePdf =
                read == header.Length &&
                header[0] == '%' &&
                header[1] == 'P' &&
                header[2] == 'D' &&
                header[3] == 'F' &&
                header[4] == '-';

        if (!looksLikePdf)
        {
            return "فایل انتخاب‌شده PDF معتبر نیست.";
        }

        return null;
    }
}

