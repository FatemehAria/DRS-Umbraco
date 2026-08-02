namespace DrsUmbraco.Cms.Options;

public sealed class CrmOptions
{
    public const string SectionName = "Crm";

    public string InternalBaseUrl { get; init; } =
        string.Empty;

    public string LoginPath { get; init; } =
        "/api/users/login";

    public string[] GatewayHosts { get; init; } =
        Array.Empty<string>();

    public int[] GatewayPorts { get; init; } =
        Array.Empty<int>();

    public string TimeZoneId { get; init; } =
        string.Empty;

    public string LogoutRedirectUrl { get; init; } =
        "https://localhost:44398/customer-portal/";
}