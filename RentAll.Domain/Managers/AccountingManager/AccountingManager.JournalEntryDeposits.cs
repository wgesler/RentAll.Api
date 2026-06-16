using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<JournalEntry> CreateJournalEntryFromDepositAsync(int officeId, Guid organizationId, int bankChartOfAccountId, string description, decimal amount, DateOnly depositDate, List<Guid> journalEntryLineIds, Guid currentUser)
    {
        if (officeId <= 0)
            throw new Exception("OfficeId is required");

        if (organizationId == Guid.Empty)
            throw new Exception("OrganizationId is required");

        if (bankChartOfAccountId <= 0)
            throw new Exception("Bank chart of account is required");

        if (amount == 0)
            throw new Exception("Deposit amount cannot be zero");

        if (depositDate == default)
            throw new Exception("Deposit date is required");

        if (journalEntryLineIds == null || journalEntryLineIds.Count == 0)
            throw new Exception("At least one journal entry line is required for a deposit");

        var distinctLineIds = journalEntryLineIds
            .Where(lineId => lineId != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctLineIds.Count != journalEntryLineIds.Count)
            throw new Exception("Duplicate journal entry lines were submitted for deposit");

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
        var bankAccountId = GetDepositBankAccountId(chartOfAccounts, officeId, bankChartOfAccountId);
        var undepositedFundsAccountIds = GetUndepositedFundsAccountIds(chartOfAccounts, officeId, accountingOffice);

        var sourceLines = await LoadDepositSourceLinesAsync(
            organizationId,
            officeId,
            distinctLineIds,
            undepositedFundsAccountIds);

        var selectedTotal = sourceLines.Sum(GetJournalEntryLineNetAmount);
        if (Math.Abs(selectedTotal - amount) > 0.005m)
            throw new Exception($"Deposit amount must equal the selected undeposited funds total ({selectedTotal:0.00})");

        var memo = string.IsNullOrWhiteSpace(description)
            ? "Deposit"
            : description.Trim();
        var depositSourceId = Guid.NewGuid();
        var journalEntryLines = new List<JournalEntryLine>
        {
            new()
            {
                ChartOfAccountId = bankAccountId,
                Debit = Math.Abs(amount),
                Credit = 0,
                Memo = memo,
                CreatedBy = currentUser
            }
        };

        foreach (var sourceLine in sourceLines)
        {
            var lineAmount = GetJournalEntryLineNetAmount(sourceLine);
            journalEntryLines.Add(new JournalEntryLine
            {
                ChartOfAccountId = sourceLine.ChartOfAccountId,
                CostCodeId = sourceLine.CostCodeId,
                PropertyId = sourceLine.PropertyId,
                ReservationId = sourceLine.ReservationId,
                ContactId = sourceLine.ContactId,
                Debit = 0,
                Credit = lineAmount,
                Memo = string.IsNullOrWhiteSpace(sourceLine.Memo) ? memo : sourceLine.Memo.Trim(),
                CreatedBy = currentUser
            });
        }

        var journalEntry = new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            TransactionDate = depositDate,
            PostingDate = depositDate,
            SourceTypeId = (int)SourceType.Deposit,
            SourceId = depositSourceId,
            Memo = memo,
            JournalEntryLines = journalEntryLines,
            CreatedBy = currentUser
        };

        return await CreateJournalEntryAsync(journalEntry);
    }

    async Task<List<JournalEntryLineSearchResult>> LoadDepositSourceLinesAsync(Guid organizationId, int officeId, List<Guid> journalEntryLineIds, HashSet<int> undepositedFundsAccountIds)
    {
        var lineIdSet = journalEntryLineIds.ToHashSet();
        var matchingLines = (await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            IncludeVoided = false,
            IncludeUnposted = true
        }))
            .Where(line =>
                lineIdSet.Contains(line.JournalEntryLineId)
                && !line.IsVoided
                && undepositedFundsAccountIds.Contains(line.ChartOfAccountId))
            .ToList();

        if (matchingLines.Count != journalEntryLineIds.Count)
            throw new Exception("One or more selected journal entry lines are invalid for deposit");

        foreach (var line in matchingLines)
        {
            if (GetJournalEntryLineNetAmount(line) <= 0)
                throw new Exception("Selected journal entry lines must have a positive undeposited funds balance");
        }

        return matchingLines
            .OrderBy(line => line.TransactionDate)
            .ThenBy(line => line.JournalEntryCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.JournalEntryLineId)
            .ToList();
    }

    static decimal GetJournalEntryLineNetAmount(JournalEntryLineSearchResult line)
    {
        return Math.Round(line.Debit - line.Credit, 2, MidpointRounding.AwayFromZero);
    }
}
