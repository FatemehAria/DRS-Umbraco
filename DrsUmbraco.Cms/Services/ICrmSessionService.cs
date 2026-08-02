using DrsUmbraco.Cms.Models.Sso;

namespace DrsUmbraco.Cms.Services;

public interface ICrmSessionService
{
    void EstablishSession(
        HttpResponse response,
        CrmLoginResult loginResult);

    void ClearSession(HttpResponse response);
}