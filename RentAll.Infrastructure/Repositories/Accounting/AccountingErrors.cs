using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task LogAccountingErrorAsync(AccountingError error)
    {
        _logger.LogError(
            "Accounting error ({Trigger}) for {DocumentCode} [{SourceTypeId}/{SourceId}]: {Message}",
            error.Trigger,
            error.DocumentCode,
            error.SourceTypeId,
            error.SourceId,
            error.Message);

        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Logging.AccountingErrorLog_Add", new
        {
            OrganizationId = error.OrganizationId,
            OfficeId = error.OfficeId,
            Trigger = error.Trigger,
            SourceTypeId = error.SourceTypeId,
            SourceId = error.SourceId,
            DocumentCode = error.DocumentCode,
            AccountingPeriod = error.AccountingPeriod,
            Amount = error.Amount,
            Message = error.Message,
            CreatedBy = error.CreatedBy
        });
    }
}
