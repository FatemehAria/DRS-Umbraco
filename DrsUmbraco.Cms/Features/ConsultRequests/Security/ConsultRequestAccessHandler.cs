using DrsUmbraco.Cms.Features.ConsultRequests.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;

namespace DrsUmbraco.Cms.Features.ConsultRequests.Security;

public sealed class ConsultRequestAccessHandler : AuthorizationHandler<ConsultRequestAccessRequirement>
{
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IOptions<ConsultRequestOptions> _options;

    public ConsultRequestAccessHandler(
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IOptions<ConsultRequestOptions> options)
    {
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;

        _options = options;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ConsultRequestAccessRequirement requirement)
    {
        var currentUser =
            _backOfficeSecurityAccessor
                .BackOfficeSecurity?
                .CurrentUser;

        if (currentUser is null)
        {
            return Task.CompletedTask;
        }

        bool isAdministrator =
            currentUser.Groups.Any(
                group =>
                    string.Equals(
                        group.Alias,
                        Constants.Security.AdminGroupAlias,
                        StringComparison.OrdinalIgnoreCase));

        Guid[] allowedGroupKeys = _options.Value.AllowedBackofficeGroupKeys;

        bool belongsToAllowedGroup =
            currentUser.Groups.Any(
                group =>
                    allowedGroupKeys.Contains(
                        group.Key));

        if (isAdministrator || belongsToAllowedGroup)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}