using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private static bool IsOwnerInvoiceRentJournalEntry(JournalEntry journalEntry)
        => journalEntry.SourceTypeId == (int)SourceType.Invoice
            && journalEntry.SourceId is { } sourceId
            && sourceId != Guid.Empty
            && journalEntry.JournalEntryKindId is JournalEntryKind.OwnerExpected or JournalEntryKind.OwnerActual
            && journalEntry.AccountingPeriod != default;

    private static bool TryResolveOwnerInvoiceOutstandingSliceKey(
        JournalEntry journalEntry,
        int ownerAccountsPayableAccountId,
        out OwnerInvoiceOutstandingSliceKey sliceKey)
    {
        sliceKey = default;
        if (!IsOwnerInvoiceRentJournalEntry(journalEntry))
            return false;

        Guid? propertyId = journalEntry.JournalEntryLines
            .Where(line => line.ChartOfAccountId == ownerAccountsPayableAccountId && line.PropertyId is { } linePropertyId && linePropertyId != Guid.Empty)
            .Select(line => line.PropertyId)
            .FirstOrDefault();

        if (propertyId is not { } resolvedPropertyId || resolvedPropertyId == Guid.Empty)
        {
            resolvedPropertyId = journalEntry.JournalEntryLines
                .FirstOrDefault(line => line.PropertyId is { } linePropertyId && linePropertyId != Guid.Empty)?.PropertyId ?? Guid.Empty;
        }

        if (resolvedPropertyId == Guid.Empty || journalEntry.SourceId is not { } invoiceId || invoiceId == Guid.Empty)
            return false;

        sliceKey = new OwnerInvoiceOutstandingSliceKey(
            journalEntry.OrganizationId,
            journalEntry.OfficeId,
            resolvedPropertyId,
            invoiceId,
            journalEntry.AccountingPeriod);
        return true;
    }

    private async Task TrySyncOwnerInvoiceOutstandingForSliceAsync(OwnerInvoiceOutstandingSliceKey sliceKey)
    {
        if (!await IsAccountingFeatureEnabledAsync(sliceKey.OrganizationId))
            return;

        await _accountingRepository.RecalculateOwnerInvoiceOutstandingForSliceAsync(sliceKey);
    }

    private async Task TrySyncOwnerInvoiceOutstandingFromJournalEntryAsync(JournalEntry? journalEntry)
    {
        if (journalEntry == null || !IsOwnerInvoiceRentJournalEntry(journalEntry))
            return;

        try
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(journalEntry.OrganizationId, journalEntry.OfficeId);
            var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, journalEntry.OfficeId, accountingOffice);
            if (!TryResolveOwnerInvoiceOutstandingSliceKey(journalEntry, ownerAccountsPayableAccountId, out var sliceKey))
                return;

            await TrySyncOwnerInvoiceOutstandingForSliceAsync(sliceKey);
        }
        catch (Exception ex)
        {
            await LogAccountingErrorAsync(
                trigger: "OwnerInvoiceOutstandingSync",
                organizationId: journalEntry.OrganizationId,
                officeId: journalEntry.OfficeId,
                sourceTypeId: journalEntry.SourceTypeId,
                sourceId: journalEntry.SourceId,
                documentCode: journalEntry.SourceCode,
                accountingPeriod: journalEntry.AccountingPeriod,
                amount: null,
                message: ex.Message,
                currentUser: journalEntry.ModifiedBy != Guid.Empty ? journalEntry.ModifiedBy : journalEntry.CreatedBy);
        }
    }

    private async Task TrySyncOwnerInvoiceOutstandingAfterDeleteAsync(JournalEntry? journalEntry)
    {
        if (journalEntry == null || !IsOwnerInvoiceRentJournalEntry(journalEntry))
            return;

        try
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(journalEntry.OrganizationId, journalEntry.OfficeId);
            var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, journalEntry.OfficeId, accountingOffice);
            if (!TryResolveOwnerInvoiceOutstandingSliceKey(journalEntry, ownerAccountsPayableAccountId, out var sliceKey))
                return;

            await TrySyncOwnerInvoiceOutstandingForSliceAsync(sliceKey);
        }
        catch (Exception ex)
        {
            await LogAccountingErrorAsync(
                trigger: "OwnerInvoiceOutstandingSyncDelete",
                organizationId: journalEntry.OrganizationId,
                officeId: journalEntry.OfficeId,
                sourceTypeId: journalEntry.SourceTypeId,
                sourceId: journalEntry.SourceId,
                documentCode: journalEntry.SourceCode,
                accountingPeriod: journalEntry.AccountingPeriod,
                amount: null,
                message: ex.Message,
                currentUser: journalEntry.ModifiedBy != Guid.Empty ? journalEntry.ModifiedBy : journalEntry.CreatedBy);
        }
    }
}
