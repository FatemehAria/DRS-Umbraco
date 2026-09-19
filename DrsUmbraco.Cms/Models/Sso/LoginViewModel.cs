namespace DrsUmbraco.Cms.Models.Sso;

public sealed record LoginViewModel(
    string PublicHomeUrl,
    string? UserNo = null,
    string? ErrorMessage = null);