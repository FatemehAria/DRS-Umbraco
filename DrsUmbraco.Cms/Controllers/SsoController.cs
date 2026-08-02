using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using DrsUmbraco.Cms.Models.Sso;
using DrsUmbraco.Cms.Services;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Controllers;

[Route("sso")]
public sealed class SsoController : Controller
{
    private readonly ICrmAuthenticationService
    _crmAuthenticationService;

    private readonly IConfiguration _configuration;

    public SsoController(
    ICrmAuthenticationService crmAuthenticationService,
    IConfiguration configuration)
    {
        _crmAuthenticationService =
            crmAuthenticationService;

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

        CopyCrmCookies(
            loginResult.SetCookieHeaders);

        CreateGatewaySessionCookie(
            loginResult.ResponseBody);

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

        ExpireAllCrmCookies();

        var model = new LogoutViewModel(redirectUrl);

        return View("Logout", model);
    }

    private void CopyCrmCookies(
    IEnumerable<string> setCookieHeaders)
    {
        foreach (var cookie in setCookieHeaders)
        {
            Response.Headers.Append(
                "Set-Cookie",
                cookie);
        }
    }

    private void CreateGatewaySessionCookie(
        string responseBody)
    {
        var expires =
            GetCrmGatewaySessionExpires(
                responseBody);

        var maxAge =
            expires - DateTimeOffset.UtcNow;

        if (maxAge <= TimeSpan.Zero)
        {
            expires =
                DateTimeOffset.UtcNow.AddMinutes(30);

            maxAge =
                TimeSpan.FromMinutes(30);
        }

        Response.Cookies.Append(
            "CrmGatewaySession",
            "1",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = expires,
                MaxAge = maxAge
            });
    }

    private DateTimeOffset GetCrmGatewaySessionExpires(
        string responseBody)
    {
        var fallbackExpires =
            DateTimeOffset.UtcNow.AddMinutes(30);

        try
        {
            using var document =
                JsonDocument.Parse(responseBody);

            var expireText =
                GetJsonStringCaseInsensitive(
                    document.RootElement,
                    "expireDateTime");

            if (string.IsNullOrWhiteSpace(expireText))
            {
                return fallbackExpires;
            }

            if (!DateTime.TryParse(
                    expireText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var crmLocalDateTime))
            {
                return fallbackExpires;
            }

            var crmTimeZone =
                GetCrmTimeZone();

            var unspecifiedCrmDateTime =
                DateTime.SpecifyKind(
                    crmLocalDateTime,
                    DateTimeKind.Unspecified);

            var utcDateTime =
                TimeZoneInfo.ConvertTimeToUtc(
                    unspecifiedCrmDateTime,
                    crmTimeZone);

            var expires =
                new DateTimeOffset(
                    utcDateTime,
                    TimeSpan.Zero);

            return expires > DateTimeOffset.UtcNow
                ? expires
                : fallbackExpires;
        }
        catch
        {
            return fallbackExpires;
        }
    }

    private static string? GetJsonStringCaseInsensitive(
        JsonElement element,
        string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private TimeZoneInfo GetCrmTimeZone()
    {
        var configuredTimeZoneId =
            _configuration["Crm:TimeZoneId"];

        if (!string.IsNullOrWhiteSpace(
                configuredTimeZoneId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                configuredTimeZoneId);
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Iran Standard Time");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Tehran");
        }
    }

    private void ExpireAllCrmCookies()
    {
        ExpireCrmCookie(".SAMA.Session");
        ExpireCrmCookie("SystemName");
        ExpireCrmCookie("X-Token");
        ExpireCrmCookie("X-Ip");
        ExpireCrmCookie("CrmGatewaySession");
    }

    private void ExpireCrmCookie(string name)
    {
        Response.Cookies.Delete(
            name,
            new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
    }

    private void PreventBrowserCache()
    {
        Response.Headers.CacheControl =
            "no-store, no-cache, max-age=0, must-revalidate";

        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
    }
}