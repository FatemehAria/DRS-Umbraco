using DrsUmbraco.Cms.Controllers;
using DrsUmbraco.Cms.Models.Sso;
using DrsUmbraco.Cms.Options;
using DrsUmbraco.Cms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Tests.Controllers;

public sealed class SsoControllerTests
{
    [Fact]
    public void Login_get_without_session_returns_login_view()
    {
        var authenticationService = new FakeCrmAuthenticationService();
        var sessionService = new FakeCrmSessionService();

        var controller = CreateController(
            authenticationService,
            sessionService);

        var result = controller.Login();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LoginViewModel>(view.Model);

        Assert.Equal("https://example.test/", model.PublicHomeUrl);
        Assert.Equal("no-store, no-cache, max-age=0, must-revalidate",
            controller.Response.Headers.CacheControl);
    }

    [Fact]
    public void Login_get_with_gateway_session_redirects_to_dashboard()
    {
        var authenticationService = new FakeCrmAuthenticationService();
        var sessionService = new FakeCrmSessionService();

        var controller = CreateController(
            authenticationService,
            sessionService);

        controller.Request.Headers.Cookie = "CrmGatewaySession=1";

        var result = controller.Login();

        var redirect = Assert.IsType<RedirectResult>(result);

        Assert.Equal("/dashboard", redirect.Url);
    }

    [Fact]
    public async Task Login_post_with_missing_credentials_returns_bad_request()
    {
        var authenticationService = new FakeCrmAuthenticationService();
        var sessionService = new FakeCrmSessionService();

        var controller = CreateController(
            authenticationService,
            sessionService);

        var result = await controller.Login(
            new LoginRequestModel
            {
                UserNo = " ",
                WebPWD = null
            },
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);

        Assert.Equal(StatusCodes.Status400BadRequest, controller.Response.StatusCode);
        Assert.Equal("Login", view.ViewName);
        Assert.Equal(0, authenticationService.CallCount);
    }

    [Fact]
    public async Task Login_post_with_invalid_credentials_returns_unauthorized()
    {
        var authenticationService = new FakeCrmAuthenticationService
        {
            Result = FailedLogin()
        };

        var sessionService = new FakeCrmSessionService();

        var controller = CreateController(
            authenticationService,
            sessionService);

        var result = await controller.Login(
            ValidLoginRequest(),
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, controller.Response.StatusCode);
        Assert.Equal("Login", view.ViewName);
        Assert.Equal(0, sessionService.EstablishCallCount);
    }

    [Fact]
    public async Task Login_post_when_crm_is_unavailable_returns_service_unavailable()
    {
        var authenticationService = new FakeCrmAuthenticationService
        {
            Result = new CrmLoginResult(
                IsSuccess: false,
                IsUpstreamUnavailable: true,
                ResponseBody: string.Empty,
                SetCookieHeaders: Array.Empty<string>())
        };

        var sessionService = new FakeCrmSessionService();

        var controller = CreateController(
            authenticationService,
            sessionService);

        var result = await controller.Login(
            ValidLoginRequest(),
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, controller.Response.StatusCode);
        Assert.Equal("Login", view.ViewName);
        Assert.Equal(0, sessionService.EstablishCallCount);
    }

    [Fact]
    public async Task Login_post_with_valid_credentials_establishes_session()
    {
        var loginResult = new CrmLoginResult(
            IsSuccess: true,
            IsUpstreamUnavailable: false,
            ResponseBody: """{"expireDateTime":"2026-12-31T12:00:00"}""",
            SetCookieHeaders: Array.Empty<string>());

        var authenticationService = new FakeCrmAuthenticationService
        {
            Result = loginResult
        };

        var sessionService = new FakeCrmSessionService();

        var controller = CreateController(
            authenticationService,
            sessionService);

        var result = await controller.Login(
            ValidLoginRequest(),
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);

        Assert.Equal("LoginSuccess", view.ViewName);
        Assert.Equal(1, sessionService.EstablishCallCount);
        Assert.Same(loginResult, sessionService.LastLoginResult);
    }

    [Fact]
    public void Logout_clears_session_and_returns_logout_view()
    {
        var authenticationService = new FakeCrmAuthenticationService();
        var sessionService = new FakeCrmSessionService();

        var controller = CreateController(
            authenticationService,
            sessionService);

        var result = controller.Logout();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LogoutViewModel>(view.Model);

        Assert.Equal("Logout", view.ViewName);
        Assert.Equal(1, sessionService.ClearCallCount);
        Assert.Equal("https://example.test/", model.RedirectUrl);
    }

    private static SsoController CreateController(
        FakeCrmAuthenticationService authenticationService,
        FakeCrmSessionService sessionService)
    {
        var controller = new SsoController(
            authenticationService,
            sessionService,
            Microsoft.Extensions.Options.Options.Create(new CrmOptions
            {
                LogoutRedirectUrl = "https://example.test/"
            }),
            Microsoft.Extensions.Options.Options.Create(new SiteOptions
            {
                PublicBaseUrl = "https://example.test/"
            }));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static LoginRequestModel ValidLoginRequest()
    {
        return new LoginRequestModel
        {
            UserNo = "test-user",
            WebPWD = "test-password"
        };
    }

    private static CrmLoginResult FailedLogin()
    {
        return new CrmLoginResult(
            IsSuccess: false,
            IsUpstreamUnavailable: false,
            ResponseBody: string.Empty,
            SetCookieHeaders: Array.Empty<string>());
    }

    private sealed class FakeCrmAuthenticationService
        : ICrmAuthenticationService
    {
        public int CallCount { get; private set; }

        public CrmLoginResult Result { get; init; } = FailedLogin();

        public Task<CrmLoginResult> LoginAsync(
            string userNo,
            string password,
            CancellationToken cancellationToken)
        {
            CallCount++;

            return Task.FromResult(Result);
        }
    }

    private sealed class FakeCrmSessionService
        : ICrmSessionService
    {
        public int EstablishCallCount { get; private set; }

        public int ClearCallCount { get; private set; }

        public CrmLoginResult? LastLoginResult { get; private set; }

        public void EstablishSession(
            HttpResponse response,
            CrmLoginResult loginResult)
        {
            EstablishCallCount++;
            LastLoginResult = loginResult;
        }

        public void ClearSession(HttpResponse response)
        {
            ClearCallCount++;
        }
    }
}