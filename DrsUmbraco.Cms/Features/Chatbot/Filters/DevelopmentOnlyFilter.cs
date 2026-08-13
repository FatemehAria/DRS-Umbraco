using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrsUmbraco.Cms.Features.Chatbot.Filters;

public sealed class DevelopmentOnlyFilter
    : IAsyncActionFilter
{
    private readonly IWebHostEnvironment _environment;

    public DevelopmentOnlyFilter(
        IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!_environment.IsDevelopment())
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}