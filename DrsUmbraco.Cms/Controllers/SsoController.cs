using DrsUmbraco.Cms.Models.Sso;
using DrsUmbraco.Cms.Services;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Controllers;

[Route("sso")]
public sealed class SsoController : Controller
{
    private readonly ICrmAuthenticationService _crmAuthenticationService;

    private readonly IConfiguration _configuration;

    private readonly ICrmSessionService _crmSessionService;

    public SsoController(
    ICrmAuthenticationService crmAuthenticationService,
    ICrmSessionService crmSessionService,
    IConfiguration configuration)
    {
        _crmAuthenticationService =
            crmAuthenticationService;

        _crmSessionService =
            crmSessionService;

        _configuration = configuration;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        PreventBrowserCache();

        if (Request.Cookies.ContainsKey("CrmGatewaySession"))
        {
            return Redirect("/dashboard");
        }

        var configuredPublicBaseUrl =
            _configuration["Site:PublicBaseUrl"];

        if (!Uri.TryCreate(
                configuredPublicBaseUrl,
                UriKind.Absolute,
                out var publicBaseUri))
        {
            throw new InvalidOperationException(
                "Site:PublicBaseUrl is missing or invalid.");
        }

        var model = new LoginViewModel(
            PublicHomeUrl:
                new Uri(publicBaseUri, "/").AbsoluteUri);

        return View(model);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromForm] LoginRequestModel model,
        CancellationToken cancellationToken)
    {
        PreventBrowserCache();

        if (string.IsNullOrWhiteSpace(model.UserNo) ||
            string.IsNullOrWhiteSpace(model.WebPWD))
        {
            return BadRequest(
                "Username and password are required.");
        }

        var loginResult =
        await _crmAuthenticationService.LoginAsync(
            model.UserNo.Trim(),
            model.WebPWD,
            cancellationToken);

        if (loginResult.IsUpstreamUnavailable)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                "CRM service is unavailable.");
        }

        if (!loginResult.IsSuccess)
        {
            return Unauthorized("Login failed.");
        }

        _crmSessionService.EstablishSession(Response, loginResult);

        var viewModel =
            new LoginSuccessViewModel(
                loginResult.ResponseBody);

        return View(
            "LoginSuccess",
            viewModel);
    }

    [HttpGet("logout")]
    [HttpGet("logout/{**catchAll}")]
    [HttpGet("~/login")]
    [HttpGet("~/login/{**catchAll}")]
    public IActionResult Logout()
    {
        PreventBrowserCache();

        var redirectUrl =
            _configuration["Crm:LogoutRedirectUrl"]
            ?? "https://localhost:44398/customer-portal/";

        _crmSessionService.ClearSession(Response);

        var model = new LogoutViewModel(redirectUrl);

        return View("Logout", model);
    }

    private void PreventBrowserCache()
    {
        Response.Headers.CacheControl =
            "no-store, no-cache, max-age=0, must-revalidate";

        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
    }
}