using System.Net.Http.Json;
using DrsUmbraco.Cms.Models.Sso;
using DrsUmbraco.Cms.Options;
using Microsoft.Extensions.Options;

namespace DrsUmbraco.Cms.Services;

public sealed class CrmAuthenticationService
    : ICrmAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly CrmOptions _crmOptions;

    public CrmAuthenticationService(
    HttpClient httpClient,
    IOptions<CrmOptions> crmOptions)
    {
        _httpClient = httpClient;
        _crmOptions = crmOptions.Value;
    }

    public async Task<CrmLoginResult> LoginAsync(
        string userNo,
        string password,
        CancellationToken cancellationToken)
    {

        var payload = new
        {
            UserNo = userNo,
            WebPWD = password,
            SystemName = "sama"
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(_crmOptions.LoginPath, payload, cancellationToken);

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