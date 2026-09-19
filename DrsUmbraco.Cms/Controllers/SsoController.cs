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
        _crmAuthenticationService = crmAuthenticationService;

        _crmSessionService = crmSessionService;

        _crmOptions = crmOptions.Value;

        _siteOptions = siteOptions.Value;
    }

    [HttpGet("~/signin")]
    public IActionResult Login()
    {
        PreventBrowserCache();

        if (Request.Cookies.ContainsKey("CrmGatewaySession"))
        {
            return Redirect("/dashboard");
        }

        return View(CreateLoginViewModel());
    }

    [HttpPost("~/signin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] LoginRequestModel model,
        CancellationToken cancellationToken)
    {
        PreventBrowserCache();

        var userNo = model.UserNo?.Trim();

        if (string.IsNullOrWhiteSpace(userNo) || string.IsNullOrWhiteSpace(model.WebPWD))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;

            return View(
                "Login",
                CreateLoginViewModel(userNo, "وارد کردن نام کاربری و رمز عبور الزامی است."));
        }

        var loginResult = await _crmAuthenticationService.LoginAsync(userNo, model.WebPWD, cancellationToken);

        if (loginResult.IsUpstreamUnavailable)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

            return View(
                "Login",
                CreateLoginViewModel(userNo, "در حال حاضر ارتباط با سامانه برقرار نیست. لطفاً کمی بعد دوباره تلاش کنید."));
        }

        if (!loginResult.IsSuccess)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;

            return View(
                "Login",
                CreateLoginViewModel(userNo, "نام کاربری یا رمز عبور صحیح نیست."));
        }

        _crmSessionService.EstablishSession(Response, loginResult);

        var viewModel = new LoginSuccessViewModel(loginResult.ResponseBody);

        return View("LoginSuccess", viewModel);
    }

    [HttpGet("~/signout")]
    [HttpGet("~/signout/{**catchAll}")]
    [HttpGet("~/login")]
    [HttpGet("~/login/{**catchAll}")]
    public IActionResult Logout()
    {
        PreventBrowserCache();

        _crmSessionService.ClearSession(Response);

        var model = new LogoutViewModel(_crmOptions.LogoutRedirectUrl);

        return View("Logout", model);
    }

    private void PreventBrowserCache()
    {
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate";

        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
    }

    private LoginViewModel CreateLoginViewModel(
        string? userNo = null,
        string? errorMessage = null)
    {
        var publicBaseUri = new Uri(_siteOptions.PublicBaseUrl, UriKind.Absolute);

        var publicHomeUrl = new Uri(publicBaseUri, "/").AbsoluteUri;

        return new LoginViewModel(
            PublicHomeUrl: publicHomeUrl,
            UserNo: userNo,
            ErrorMessage: errorMessage);
    }
}