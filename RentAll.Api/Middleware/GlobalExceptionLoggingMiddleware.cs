using RentAll.Domain.Interfaces.Repositories;
using System.Text;
using System.Text.Json;

namespace RentAll.Api.Middleware;

public class GlobalExceptionLoggingMiddleware
{
    private const int MaxMessageLength = 2500;
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionLoggingMiddleware> _logger;

    public GlobalExceptionLoggingMiddleware(RequestDelegate next, ILogger<GlobalExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILoggingRepository loggingRepository)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            try
            {
                var errorLog = BuildErrorLog(context, ex);
                await loggingRepository.AddErrorLogAsync(errorLog);
            }
            catch (Exception logException)
            {
                _logger.LogError(logException, "Failed to persist API exception to Logging.GeneralErrorLog.");
            }

            throw;
        }
    }

    private static LoggingErrorLog BuildErrorLog(HttpContext context, Exception ex)
    {
        return new LoggingErrorLog
        {
            OrganizationId = ResolveOrganizationId(context),
            OfficeId = ResolveInt(context, "officeId"),
            ReservationId = ResolveGuid(context, "reservationId"),
            PropertyId = ResolveGuid(context, "propertyId"),
            InvoiceId = ResolveGuid(context, "invoiceId"),
            ReceiptId = ResolveGuid(context, "receiptId"),
            JournalEntryId = ResolveGuid(context, "journalEntryId"),
            Message = Truncate(ex.Message, MaxMessageLength),
            Exception = ex.ToString()
        };
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

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }
}
