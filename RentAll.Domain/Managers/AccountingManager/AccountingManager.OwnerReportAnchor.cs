using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private static bool IsOwnerReportAnchorAffectingJournalEntry(JournalEntry journalEntry)
    {
        if (journalEntry.JournalEntryKindId is JournalEntryKind.OwnerExpected or JournalEntryKind.OwnerActual)
            return true;

        if (journalEntry.JournalEntryKindId is JournalEntryKind.OwnerUtility
            or JournalEntryKind.OwnerTransfer
            or JournalEntryKind.Expense
            or JournalEntryKind.LinenTowelFee
            or JournalEntryKind.LinenTowelUnusedReversal)
            return true;

        if (journalEntry.SourceTypeId == (int)SourceType.WorkOrder)
            return true;

        if (journalEntry.JournalEntryKindId is JournalEntryKind.Bill or JournalEntryKind.Receipt)
            return true;

        return journalEntry.JournalEntryKindId == JournalEntryKind.BillPayment
            && journalEntry.SourceTypeId is (int)SourceType.OwnerDistribution or (int)SourceType.BillPayment;
    }

    private static DateOnly ResolveOwnerReportAnchorInvalidationPeriod(JournalEntry journalEntry)
    {
        if (journalEntry.AccountingPeriod != default)
            return new DateOnly(journalEntry.AccountingPeriod.Year, journalEntry.AccountingPeriod.Month, 1);

        return new DateOnly(journalEntry.TransactionDate.Year, journalEntry.TransactionDate.Month, 1);
    }

    private static IEnumerable<Guid> ResolveOwnerReportAnchorPropertyIds(JournalEntry journalEntry)
        => (journalEntry.JournalEntryLines ?? [])
            .Where(line => line.PropertyId is { } propertyId && propertyId != Guid.Empty)
            .Select(line => line.PropertyId!.Value)
            .Distinct();

    private async Task TryInvalidateOwnerReportMonthlyAnchorsFromJournalEntryAsync(JournalEntry? journalEntry)
    {
        if (journalEntry == null || !IsOwnerReportAnchorAffectingJournalEntry(journalEntry))
            return;

        if (!await IsAccountingFeatureEnabledAsync(journalEntry.OrganizationId))
            return;

        var currentUser = journalEntry.ModifiedBy != Guid.Empty ? journalEntry.ModifiedBy : journalEntry.CreatedBy;
        var fromPeriodMonth = ResolveOwnerReportAnchorInvalidationPeriod(journalEntry);
        var propertyIds = ResolveOwnerReportAnchorPropertyIds(journalEntry).ToList();
        if (propertyIds.Count == 0)
        {
            await _accountingRepository.MarkOwnerReportMonthlyAnchorsDirtyFromPeriodAsync(
                journalEntry.OrganizationId,
                journalEntry.OfficeId,
                fromPeriodMonth,
                currentUser);
            return;
        }

        foreach (var propertyId in propertyIds)
        {
            try
            {
                await _accountingRepository.MarkOwnerReportMonthlyAnchorsDirtyFromPeriodAsync(
                    journalEntry.OrganizationId,
                    journalEntry.OfficeId,
                    fromPeriodMonth,
                    currentUser,
                    propertyId);
            }
            catch (Exception ex)
            {
                await LogAccountingErrorAsync(
                    trigger: "OwnerReportAnchorInvalidate",
                    organizationId: journalEntry.OrganizationId,
                    officeId: journalEntry.OfficeId,
                    sourceTypeId: journalEntry.SourceTypeId,
                    sourceId: journalEntry.SourceId,
                    documentCode: journalEntry.SourceCode,
                    accountingPeriod: journalEntry.AccountingPeriod,
                    amount: null,
                    message: ex.Message,
                    currentUser: currentUser);
            }
        }
    }
}
