using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RentAll.Api.Filters;

public class ControllerExceptionLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<ControllerExceptionLoggingFilter> _logger;

    public ControllerExceptionLoggingFilter(ILogger<ControllerExceptionLoggingFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            var executedContext = await next();
            if (executedContext.Exception is { } exception && !executedContext.ExceptionHandled)
                LogException(context, exception);
        }
        catch (Exception ex)
        {
            LogException(context, ex);
            throw;
        }
    }

    private void LogException(ActionExecutingContext context, Exception ex)
    {
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var controller = descriptor?.ControllerName ?? "UnknownController";
        var action = descriptor?.ActionName ?? "UnknownAction";
        var method = context.HttpContext.Request.Method;
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;

        _logger.LogError(ex, "Unhandled controller exception in {Controller}.{Action} for {Method} {Path}", controller, action, method, path);
    }
}
