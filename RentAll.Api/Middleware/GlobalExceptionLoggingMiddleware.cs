using RentAll.Api.Logging;
using System.Text;
using System.Text.Json;

namespace RentAll.Api.Middleware;

public class GlobalExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionLoggingMiddleware> _logger;

    public GlobalExceptionLoggingMiddleware(RequestDelegate next, ILogger<GlobalExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            ApplicationErrorLogger.Log(
                _logger,
                ex,
                operation: $"{context.Request.Method} {context.Request.Path}",
                organizationId: ResolveOrganizationId(context),
                officeId: ResolveInt(context, "officeId"),
                httpContext: context);

            throw;
        }
    }

    private static Guid? ResolveOrganizationId(HttpContext context)
    {
        var organizationId = ResolveGuid(context, "organizationId");
        if (organizationId.HasValue)
            return organizationId.Value;

        var userClaim = context.User.FindFirst("user");
        if (userClaim == null || string.IsNullOrWhiteSpace(userClaim.Value))
            return null;

        try
        {
            var userJsonBytes = Convert.FromBase64String(userClaim.Value);
            var userJson = Encoding.UTF8.GetString(userJsonBytes);
            var userObject = JsonSerializer.Deserialize<JsonElement>(userJson);

            string[] possibleOrgPropertyNames = ["organizationId", "OrganizationId"];
            foreach (var propName in possibleOrgPropertyNames)
            {
                if (!userObject.TryGetProperty(propName, out var orgIdElement))
                    continue;

                var orgIdString = orgIdElement.GetString();
                if (!string.IsNullOrWhiteSpace(orgIdString) && Guid.TryParse(orgIdString, out var orgId))
                    return orgId;
            }
        }
        catch
        {
            // Ignore claim parsing errors and fall back to null.
        }

        return null;
    }

    private static Guid? ResolveGuid(HttpContext context, string key)
    {
        if (TryGetValue(context, key, out var rawValue) && Guid.TryParse(rawValue, out var value))
            return value;

        return null;
    }

    private static int? ResolveInt(HttpContext context, string key)
    {
        if (TryGetValue(context, key, out var rawValue) && int.TryParse(rawValue, out var value))
            return value;

        return null;
    }

    private static bool TryGetValue(HttpContext context, string key, out string value)
    {
        if (TryGetRouteValue(context, key, out value))
            return true;

        if (TryGetQueryValue(context, key, out value))
            return true;

        value = string.Empty;
        return false;
    }

    private static bool TryGetRouteValue(HttpContext context, string key, out string value)
    {
        var match = context.Request.RouteValues
            .FirstOrDefault(routeValue => routeValue.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(match.Key) && match.Value != null)
        {
            value = match.Value.ToString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetQueryValue(HttpContext context, string key, out string value)
    {
        var match = context.Request.Query
            .FirstOrDefault(queryValue => queryValue.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(match.Key) && !string.IsNullOrWhiteSpace(match.Value))
        {
            value = match.Value.ToString();
            return true;
        }

        value = string.Empty;
        return false;
    }

}
