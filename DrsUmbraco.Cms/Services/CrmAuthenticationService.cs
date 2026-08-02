using System.Net.Http.Json;
using DrsUmbraco.Cms.Models.Sso;

namespace DrsUmbraco.Cms.Services;

public sealed class CrmAuthenticationService
    : ICrmAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CrmAuthenticationService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<CrmLoginResult> LoginAsync(
        string userNo,
        string password,
        CancellationToken cancellationToken)
    {
        var loginPath =
            _configuration["Crm:LoginPath"]
            ?? "/api/users/login";

        var payload = new
        {
            UserNo = userNo,
            WebPWD = password,
            SystemName = "sama"
        };

        try
        {
            using var response =
                await _httpClient.PostAsJsonAsync(
                    loginPath,
                    payload,
                    cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            var setCookieHeaders =
                response.Headers.TryGetValues(
                    "Set-Cookie",
                    out var cookies)
                    ? cookies.ToArray()
                    : Array.Empty<string>();

            return new CrmLoginResult(
                IsSuccess: response.IsSuccessStatusCode,
                IsUpstreamUnavailable: false,
                ResponseBody: responseBody,
                SetCookieHeaders: setCookieHeaders);
        }
        catch (HttpRequestException)
        {
            return CreateUnavailableResult();
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return CreateUnavailableResult();
        }
    }

    private static CrmLoginResult CreateUnavailableResult()
    {
        return new CrmLoginResult(
            IsSuccess: false,
            IsUpstreamUnavailable: true,
            ResponseBody: string.Empty,
            SetCookieHeaders: Array.Empty<string>());
    }
}