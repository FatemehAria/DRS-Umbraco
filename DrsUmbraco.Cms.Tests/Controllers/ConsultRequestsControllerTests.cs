using DrsUmbraco.Cms.Controllers;
using DrsUmbraco.Cms.Models;
using DrsUmbraco.Cms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DrsUmbraco.Cms.Tests.Controllers;

public sealed class ConsultRequestsControllerTests
{
    [Fact]
    public async Task Create_with_valid_consult_request_returns_success()
    {
        var service = new FakeElementorSubmissionService
        {
            ReturnedId = 42m
        };

        var controller = CreateController(service);

        var result = await controller.Create(
            CreateValidModel("consult_form"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.CallCount);
        Assert.Equal("consult_form", service.LastModel?.FormName);
    }

    [Fact]
    public async Task Create_with_invalid_form_name_returns_validation_problem()
    {
        var service = new FakeElementorSubmissionService();

        var controller = CreateController(service);

        var result = await controller.Create(
            CreateValidModel("unknown_form"),
            CancellationToken.None);

        var problem = AssertValidationProblem(result);

        Assert.Contains(nameof(ConsultRequestCreateModel.FormName), problem.Errors.Keys);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task Create_with_resume_on_non_job_form_returns_validation_problem()
    {
        var service = new FakeElementorSubmissionService();

        var controller = CreateController(service);

        var model = CreateValidModel("contact_form");
        model.ResumeFile = CreateFile(
            "resume.pdf",
            "%PDF-1.7 test file"u8.ToArray());

        var result = await controller.Create(model, CancellationToken.None);

        var problem = AssertValidationProblem(result);

        Assert.Contains(nameof(ConsultRequestCreateModel.ResumeFile), problem.Errors.Keys);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task Create_job_application_without_resume_returns_validation_problem()
    {
        var service = new FakeElementorSubmissionService();

        var controller = CreateController(service);

        var result = await controller.Create(
            CreateValidModel("job_interest_form"),
            CancellationToken.None);

        var problem = AssertValidationProblem(result);

        Assert.Contains(nameof(ConsultRequestCreateModel.ResumeFile), problem.Errors.Keys);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task Create_job_application_with_non_pdf_extension_returns_validation_problem()
    {
        var service = new FakeElementorSubmissionService();

        var controller = CreateController(service);

        var model = CreateValidModel("job_interest_form");
        model.ResumeFile = CreateFile(
            "resume.exe",
            "%PDF-1.7 test file"u8.ToArray());

        var result = await controller.Create(model, CancellationToken.None);

        var problem = AssertValidationProblem(result);

        Assert.Contains(nameof(ConsultRequestCreateModel.ResumeFile), problem.Errors.Keys);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task Create_job_application_with_fake_pdf_content_returns_validation_problem()
    {
        var service = new FakeElementorSubmissionService();

        var controller = CreateController(service);

        var model = CreateValidModel("job_interest_form");
        model.ResumeFile = CreateFile(
            "resume.pdf",
            "this is not a PDF"u8.ToArray());

        var result = await controller.Create(model, CancellationToken.None);

        var problem = AssertValidationProblem(result);

        Assert.Contains(nameof(ConsultRequestCreateModel.ResumeFile), problem.Errors.Keys);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task Create_job_application_with_valid_pdf_header_calls_submission_service()
    {
        var service = new FakeElementorSubmissionService
        {
            ReturnedId = 97m
        };

        var controller = CreateController(service);

        var model = CreateValidModel("job_interest_form");
        model.ResumeFile = CreateFile(
            "resume.pdf",
            "%PDF-1.7\nexample resume"u8.ToArray());

        var result = await controller.Create(model, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.CallCount);
        Assert.Equal("job_interest_form", service.LastModel?.FormName);
    }

    [Fact]
    public async Task Create_when_submission_service_throws_returns_problem_details()
    {
        var service = new FakeElementorSubmissionService
        {
            ExceptionToThrow = new InvalidOperationException("Database unavailable.")
        };

        var controller = CreateController(service);

        var result = await controller.Create(
            CreateValidModel("consult_form"),
            CancellationToken.None);

        var problemResult = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(problemResult.Value);

        Assert.Equal(StatusCodes.Status500InternalServerError, problemResult.StatusCode);
        Assert.Equal("خطا در ثبت درخواست", problem.Title);
        Assert.Equal(1, service.CallCount);
    }

    private static ConsultRequestsController CreateController(
        FakeElementorSubmissionService service)
    {
        var controller = new ConsultRequestsController(
            service,
            NullLogger<ConsultRequestsController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("example.test");

        return controller;
    }

    private static ConsultRequestCreateModel CreateValidModel(string formName)
    {
        return new ConsultRequestCreateModel
        {
            FullName = "فاطمه آریا",
            Mobile = "09123456789",
            RequestType = "درخواست مشاوره",
            Message = "برای دریافت اطلاعات بیشتر تماس بگیرید.",
            FormName = formName
        };
    }

    private static IFormFile CreateFile(
        string fileName,
        byte[] content)
    {
        var stream = new MemoryStream(content);

        return new FormFile(
            stream,
            baseStreamOffset: 0,
            length: content.Length,
            name: nameof(ConsultRequestCreateModel.ResumeFile),
            fileName: fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private static ValidationProblemDetails AssertValidationProblem(IActionResult result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);

        return Assert.IsType<ValidationProblemDetails>(objectResult.Value);
    }

    private sealed class FakeElementorSubmissionService : IElementorSubmissionService
    {
        public int CallCount { get; private set; }

        public decimal ReturnedId { get; init; } = 1m;

        public Exception? ExceptionToThrow { get; init; }

        public ConsultRequestCreateModel? LastModel { get; private set; }

        public Task<decimal> CreateConsultRequestAsync(
            ConsultRequestCreateModel model,
            string referer,
            string? refererTitle,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastModel = model;

            if (ExceptionToThrow is not null)
            {
                return Task.FromException<decimal>(ExceptionToThrow);
            }

            return Task.FromResult(ReturnedId);
        }
    }
}