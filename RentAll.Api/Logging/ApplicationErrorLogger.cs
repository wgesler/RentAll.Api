using Microsoft.Data.SqlClient;

namespace RentAll.Api.Logging;

/// <summary>
/// Default in-app error sink. Use when an error is not already written to
/// Database Error Log (SQL) or Accounting Log (LogAccountingErrorAsync).
/// </summary>
public static class ApplicationErrorLogger
{
    public static void Log(
        ILogger logger,
        Exception exception,
        string operation,
        Guid? organizationId = null,
        int? officeId = null,
        HttpContext? httpContext = null)
    {
        if (ShouldSkipApplicationLog(exception))
            return;

        StampContext(httpContext, organizationId, officeId);

        logger.LogError(
            exception,
            "[ApplicationError:{Operation}] Org={OrganizationId} Office={OfficeId} {Message}",
            operation,
            organizationId,
            officeId,
            exception.Message);
    }

    public static void Log(
        ILogger logger,
        string message,
        string operation,
        Guid? organizationId = null,
        int? officeId = null,
        HttpContext? httpContext = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        StampContext(httpContext, organizationId, officeId);

        logger.LogError(
            "[ApplicationError:{Operation}] Org={OrganizationId} Office={OfficeId} {Message}",
            operation,
            organizationId,
            officeId,
            message);
    }

    public static bool ShouldSkipApplicationLog(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is SqlException)
                return true;
        }

        return false;
    }

    private static void StampContext(HttpContext? httpContext, Guid? organizationId, int? officeId)
    {
        if (httpContext == null || organizationId is not { } orgId || orgId == Guid.Empty)
            return;

        ApplicationLogContext.Set(httpContext, orgId, officeId);
    }
}
