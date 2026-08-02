namespace DrsUmbraco.Cms.Models.Sso;

public sealed record CrmLoginResult(
    bool IsSuccess,
    bool IsUpstreamUnavailable,
    string ResponseBody,
    IReadOnlyCollection<string> SetCookieHeaders);