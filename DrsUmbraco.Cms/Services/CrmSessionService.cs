using System.Globalization;
using System.Text.Json;
using DrsUmbraco.Cms.Models.Sso;

namespace DrsUmbraco.Cms.Services;

public sealed class CrmSessionService : ICrmSessionService
{
    private static readonly string[] CrmCookieNames =
    [
        ".SAMA.Session",
        "SystemName",
        "X-Token",
        "X-Ip",
        "CrmGatewaySession"
    ];

    private readonly IConfiguration _configuration;

    public CrmSessionService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void EstablishSession(
        HttpResponse response,
        CrmLoginResult loginResult)
    {
        CopyCrmCookies(
            response,
            loginResult.SetCookieHeaders);

        CreateGatewaySessionCookie(
            response,
            loginResult.ResponseBody);
    }

    public void ClearSession(HttpResponse response)
    {
        foreach (var cookieName in CrmCookieNames)
        {
            response.Cookies.Delete(
                cookieName,
                CreateCookieOptions());
        }
    }

    private static void CopyCrmCookies(
        HttpResponse response,
        IEnumerable<string> setCookieHeaders)
    {
        foreach (var cookie in setCookieHeaders)
        {
            response.Headers.Append(
                "Set-Cookie",
                cookie);
        }
    }

    private void CreateGatewaySessionCookie(
        HttpResponse response,
        string responseBody)
    {
        var expires =
            GetGatewaySessionExpires(responseBody);

        var maxAge =
            expires - DateTimeOffset.UtcNow;

        if (maxAge <= TimeSpan.Zero)
        {
            expires =
                DateTimeOffset.UtcNow.AddMinutes(30);

            maxAge =
                TimeSpan.FromMinutes(30);
        }

        response.Cookies.Append(
            "CrmGatewaySession",
            "1",
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = expires,
                MaxAge = maxAge
            });
    }

    private DateTimeOffset GetGatewaySessionExpires(
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

            var unspecifiedCrmDateTime =
                DateTime.SpecifyKind(
                    crmLocalDateTime,
                    DateTimeKind.Unspecified);

            var utcDateTime =
                TimeZoneInfo.ConvertTimeToUtc(
                    unspecifiedCrmDateTime,
                    GetCrmTimeZone());

            var expires =
                new DateTimeOffset(
                    utcDateTime,
                    TimeSpan.Zero);

            return expires > DateTimeOffset.UtcNow
                ? expires
                : fallbackExpires;
        }
        catch (JsonException)
        {
            return fallbackExpires;
        }
        catch (FormatException)
        {
            return fallbackExpires;
        }
        catch (TimeZoneNotFoundException)
        {
            return fallbackExpires;
        }
        catch (InvalidTimeZoneException)
        {
            return fallbackExpires;
        }
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
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Tehran");
        }
    }

    private static string?
        GetJsonStringCaseInsensitive(
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

    private static CookieOptions CreateCookieOptions()
    {
        return new CookieOptions
        {
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.Lax
        };
    }
}