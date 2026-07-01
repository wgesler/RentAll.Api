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
            EndDate = criteria.EndDate,
            ExpectedAccountId = criteria.ExpectedAccountId,
            ActualAccountId = criteria.ActualAccountId,
            PrePaidAccountId = criteria.PrePaidAccountId,
            ExpenseAccountId = criteria.ExpenseAccountId
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
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            OwnerId = e.OwnerId,
            PropertyCode = e.PropertyCode,
            OwnerName = e.OwnerName,
            Expected = e.Expected,
            PrePaid = e.PrePaid,
            Outstanding = e.Outstanding,
            Income = e.Income,
            Expenses = e.Expenses,
            Balance = e.Balance,
            WorkingCapital = e.WorkingCapital,
            WorkingCapitalBalanceDue = e.WorkingCapitalBalanceDue
        };
    }

    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetOwnerStatementJournalEntryLinesAsync(OwnerStatementJournalEntryLineGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<OwnerStatementJournalEntryLineEntity>("Accounting.OwnerStatement_JournalEntryLine_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            OwnerId = criteria.OwnerId,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<OwnerStatementJournalEntryLine>();

        return res.Select(ConvertOwnerStatementJournalEntryLineEntityToModel);
    }

    private OwnerStatementJournalEntryLine ConvertOwnerStatementJournalEntryLineEntityToModel(OwnerStatementJournalEntryLineEntity e)
    {
        return new OwnerStatementJournalEntryLine
        {
            JournalEntryLineId = e.JournalEntryLineId,
            JournalEntryId = e.JournalEntryId,
            JournalEntryCode = e.JournalEntryCode,
            TransactionDate = e.TransactionDate,
            OfficeId = e.OfficeId,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ChartOfAccountId = e.ChartOfAccountId,
            AccountNo = e.AccountNo,
            ChartOfAccountName = e.ChartOfAccountName,
            Description = e.Description,
            Debit = e.Debit,
            Credit = e.Credit,
            Category = e.Category,
            Amount = e.Amount
        };
    }
}
