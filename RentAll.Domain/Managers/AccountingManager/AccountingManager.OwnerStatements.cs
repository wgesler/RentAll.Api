using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private const string OwnerStartingBalanceMemoPrefix = "Owner: Starting Balance:";

    public async Task<JournalEntry?> CreateOwnerStatementStartingBalanceJournalEntryAsync(Guid organizationId, int officeId, Guid ownerId, Guid propertyId, DateOnly transactionDate, decimal amount, Guid currentUser)
    {
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return null;
        if (officeId <= 0 || ownerId == Guid.Empty || propertyId == Guid.Empty || transactionDate == default || amount == 0)
            throw new Exception("Office, owner, property, transaction date, and non-zero amount are required to create owner starting balance.");

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
        var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice);
        var ownerExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice);
        var memo = $"{OwnerStartingBalanceMemoPrefix} {transactionDate:MM/yyyy}";
        var startingBalance = Math.Abs(amount);
        var isPositive = amount > 0;
        var journalEntry = new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            TransactionDate = transactionDate,
            SourceTypeId = (int)SourceType.Adjustment,
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine>
            {
                new JournalEntryLine
                {
                    ChartOfAccountId = ownerExpenseAccountId,
                    PropertyId = propertyId,
                    ContactId = ownerId,
                    Debit = isPositive ? startingBalance : 0,
                    Credit = isPositive ? 0 : startingBalance,
                    Memo = memo,
                    CreatedBy = currentUser
                },
                new JournalEntryLine
                {
                    ChartOfAccountId = ownerAccountsPayableAccountId,
                    PropertyId = propertyId,
                    ContactId = ownerId,
                    Debit = isPositive ? 0 : startingBalance,
                    Credit = isPositive ? startingBalance : 0,
                    Memo = memo,
                    CreatedBy = currentUser
                }
            },
            CreatedBy = currentUser
        };
        var createdJournalEntry = await CreateJournalEntryAsync(journalEntry);
        if (createdJournalEntry == null)
            return null;

        return await PostJournalEntryAsync(createdJournalEntry.JournalEntryId, organizationId, currentUser);
    }

    public async Task<OwnerStatementStartingBalanceEntry?> GetOwnerStatementStartingBalanceAsync(Guid organizationId, int officeId, Guid ownerId, Guid propertyId)
    {
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return null;
        if (officeId <= 0 || ownerId == Guid.Empty || propertyId == Guid.Empty)
            return null;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
        var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice);
        var lines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            SourceTypeId = (int)SourceType.Adjustment,
            ChartOfAccountId = ownerAccountsPayableAccountId,
            PropertyId = propertyId,
            ContactId = ownerId,
            IncludeVoided = false,
            IncludeUnposted = true
        });
        var current = lines
            .Where(line => IsOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .FirstOrDefault();
        if (current == null)
            return null;

        return new OwnerStatementStartingBalanceEntry
        {
            JournalEntryId = current.JournalEntryId,
            OfficeId = current.OfficeId,
            OwnerId = current.ContactId ?? Guid.Empty,
            PropertyId = current.PropertyId ?? Guid.Empty,
            TransactionDate = current.TransactionDate,
            Amount = current.Credit - current.Debit,
            Memo = (current.JournalEntryMemo ?? current.Memo ?? string.Empty).Trim(),
            IsPosted = current.IsPosted
        };
    }

    private static bool IsOwnerStartingBalanceMemo(string? journalMemo, string? lineMemo)
    {
        var summaryMemo = (journalMemo ?? string.Empty).Trim();
        var detailMemo = (lineMemo ?? string.Empty).Trim();
        return summaryMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase)
            || detailMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
