using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task<IEnumerable<OwnerStatementSummary>> GetOwnerStatementsAsync(OwnerStatementGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerStatementSummaryEntity>("Accounting.OwnerStatement_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<OwnerStatementSummary>();

        return res.Select(ConvertOwnerStatementEntityToModel);
    }

    private OwnerStatementSummary ConvertOwnerStatementEntityToModel(OwnerStatementSummaryEntity e)
    {
        return new OwnerStatementSummary
        {
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            OwnerName = e.OwnerName,
            Income = e.Income,
            Expenses = e.Expenses,
            Balance = e.Balance
        };
    }
}
