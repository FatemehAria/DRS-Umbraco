namespace DrsUmbraco.Cms.Options;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string PublicBaseUrl { get; init; } =
        string.Empty;
}