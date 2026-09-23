using Microsoft.Extensions.Logging;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Application Logging
    private void LogApplicationDiagnostic(string operation, string message, Exception? exception = null, Guid? organizationId = null, int? officeId = null)
    {
        if (exception != null)
        {
            _logger.LogError(
                exception,
                "[ApplicationError:{Operation}] Org={OrganizationId} Office={OfficeId} {Message}",
                operation,
                organizationId,
                officeId,
                message);
            return;
        }

        _logger.LogError(
            "[ApplicationError:{Operation}] Org={OrganizationId} Office={OfficeId} {Message}",
            operation,
            organizationId,
            officeId,
            message);
    }
    #endregion
}
