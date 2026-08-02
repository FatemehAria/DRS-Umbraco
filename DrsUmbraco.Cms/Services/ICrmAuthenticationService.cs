using DrsUmbraco.Cms.Models.Sso;

namespace DrsUmbraco.Cms.Services;

public interface ICrmAuthenticationService
{
    Task<CrmLoginResult> LoginAsync(
        string userNo,
        string password,
        CancellationToken cancellationToken);
}