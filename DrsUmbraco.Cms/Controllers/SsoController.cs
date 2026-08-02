using DrsUmbraco.Cms.Models.Sso;
using DrsUmbraco.Cms.Services;
using Microsoft.AspNetCore.Mvc;
using DrsUmbraco.Cms.Options;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Controllers;

[Route("sso")]
public sealed class SsoController : Controller
{
    private readonly ICrmAuthenticationService _crmAuthenticationService;
    private readonly CrmOptions _crmOptions;
    private readonly SiteOptions _siteOptions;
    private readonly ICrmSessionService _crmSessionService;

    public SsoController(
    ICrmAuthenticationService crmAuthenticationService,
    ICrmSessionService crmSessionService,
    IOptions<CrmOptions> crmOptions,
    IOptions<SiteOptions> siteOptions)
    {
        _crmAuthenticationService =
            crmAuthenticationService;

        _crmSessionService =
            crmSessionService;

        _crmOptions =
            crmOptions.Value;

        _siteOptions =
            siteOptions.Value;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        PreventBrowserCache();

        if (Request.Cookies.ContainsKey(
                "CrmGatewaySession"))
        {
            return Redirect("/dashboard");
        }

        var publicBaseUri =
            new Uri(
                _siteOptions.PublicBaseUrl,
                UriKind.Absolute);

        var model =
            new LoginViewModel(
                PublicHomeUrl:
                    new Uri(publicBaseUri, "/")
                        .AbsoluteUri);

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

        _crmSessionService.ClearSession(Response);

        var model =
            new LogoutViewModel(
                _crmOptions.LogoutRedirectUrl);

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