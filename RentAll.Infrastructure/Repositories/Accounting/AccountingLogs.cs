using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task LogAccountingLogAsync(AccountingLog log)
    {
        _logger.LogInformation(
            "Accounting log for invoice {InvoiceId} (split={Split}): original={OriginalAmount}, first={FirstAmount}, second={SecondAmount}, message={Message}",
            log.InvoiceId,
            log.Split,
            log.OriginalAmount,
            log.FirstAmount,
            log.SecondAmount,
            log.Message);

        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.AccountingLog_Add", new
        {
            OrganizationId = log.OrganizationId,
            OfficeId = log.OfficeId,
            PropertyId = log.PropertyId,
            InvoiceId = log.InvoiceId,
            OriginalAmount = log.OriginalAmount,
            RentalLine = log.RentalLine,
            Split = log.Split,
            FirstPeriod = log.FirstPeriod,
            SecondPeriod = log.SecondPeriod,
            FirstAmount = log.FirstAmount,
            SecondAmount = log.SecondAmount,
            Message = log.Message
        });
    }

    public async Task DeleteAllAccountingLogsByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.AccountingLog_DeleteAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });
    }
}
