using DrsUmbraco.Cms.Models.Sso;
using Microsoft.AspNetCore.Mvc;

namespace DrsUmbraco.Cms.Controllers;

[Route("sso")]
public sealed class SsoController : Controller
{
    private readonly IConfiguration _configuration;

    public SsoController(IConfiguration configuration)
    {
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

    private void PreventBrowserCache()
    {
        Response.Headers.CacheControl =
            "no-store, no-cache, max-age=0, must-revalidate";

        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";
    }
}