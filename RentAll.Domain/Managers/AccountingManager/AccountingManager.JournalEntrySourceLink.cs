using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Helpers
    private async Task<List<JournalEntry>> GetJournalEntriesForSourceAsync(Guid organizationId, int officeId, SourceType sourceType, Guid sourceId, JournalEntryKind? journalEntryKind = null)
    {
        // Invoice JEs include cash-only rows excluded from GetByCriteria — only trust FullyLoaded.
        // Other source types are complete in the office bulk load.
        if (_officeSyncCache != null
            && (_officeSyncCache.HasFullyLoadedSource((int)sourceType, sourceId)
                || IsOfficeBulkCompleteSourceType(sourceType)))
        {
            return _officeSyncCache.GetBySource((int)sourceType, sourceId, journalEntryKind).ToList();
        }

        var entries = (await _journalEntryRepository.GetJournalEntriesBySourceIdAsync(new JournalEntryGetBySourceIdCriteria
        {
            OrganizationId = organizationId,
            SourceTypeId = (int)sourceType,
            SourceId = sourceId,
            OfficeIds = officeId.ToString(),
            JournalEntryKindId = journalEntryKind.HasValue ? (int)journalEntryKind.Value : null,
            IncludeUnposted = true,
            IncludeCashOnly = true
        })).ToList();

        if (_officeSyncCache != null)
            _officeSyncCache.IndexJournalEntries(entries, markSourceFullyLoaded: journalEntryKind == null);

        return entries;
    }

    private static bool IsOfficeBulkCompleteSourceType(SourceType sourceType)
        => sourceType is SourceType.Deposit
            or SourceType.Transfer
            or SourceType.Bill
            or SourceType.BillPayment
            or SourceType.Receipt
            or SourceType.WorkOrder;
    // InvoicePayment intentionally omitted — rare/legacy source rows are not guaranteed
    // in the office bulk JE pull; always load by source id from the repository.

    private async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByPaymentIdCachedAsync(Guid organizationId, Guid paymentId)
    {
        if (_officeSyncCache != null)
            return _officeSyncCache.GetByPaymentId(paymentId);

        return (await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(new JournalEntryGetByPaymentIdCriteria
        {
            OrganizationId = organizationId,
            PaymentId = paymentId
        })).ToList();
    }

    private async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByDepositIdCachedAsync(Guid organizationId, Guid depositId)
    {
        if (_officeSyncCache != null)
            return _officeSyncCache.GetByDepositId(depositId);

        return (await _journalEntryRepository.GetJournalEntriesByDepositIdAsync(new JournalEntryGetByDepositIdCriteria
        {
            OrganizationId = organizationId,
            DepositId = depositId
        })).ToList();
    }

    private async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByTransferIdCachedAsync(Guid organizationId, Guid transferId)
    {
        if (_officeSyncCache != null)
            return _officeSyncCache.GetByTransferId(transferId);

        return (await _journalEntryRepository.GetJournalEntriesByTransferIdAsync(new JournalEntryGetByTransferIdCriteria
        {
            OrganizationId = organizationId,
            TransferId = transferId
        })).ToList();
    }

    private async Task<JournalEntry?> GetJournalEntryByIdCachedAsync(Guid journalEntryId, Guid organizationId)
    {
        if (_officeSyncCache != null && _officeSyncCache.TryGetJournalEntry(journalEntryId, out var cached))
            return cached;

        return await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, organizationId);
    }

    private async Task<JournalEntryLine?> GetJournalEntryLineByIdCachedAsync(Guid journalEntryLineId)
    {
        if (_officeSyncCache != null && _officeSyncCache.TryGetJournalEntryLine(journalEntryLineId, out var cached))
            return cached;

        return await _journalEntryRepository.GetJournalEntryLineByIdAsync(journalEntryLineId);
    }

    private async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByCriteriaCachedAsync(JournalEntryGetCriteria criteria)
    {
        if (_officeSyncCache != null
            && criteria.SourceTypeId is int sourceTypeId
            && criteria.SourceId is { } sourceId
            && sourceId != Guid.Empty)
        {
            var officeIdFilter = int.TryParse(criteria.OfficeIds?.Split(',')[0], out var officeId) ? officeId : (int?)null;
            return _officeSyncCache.GetBySource(sourceTypeId, sourceId)
                .Where(entry => officeIdFilter is null or <= 0 || entry.OfficeId == officeIdFilter.Value)
                .ToList();
        }

        if (_officeSyncCache != null && criteria.SourceTypeId is int sourceTypeOnly)
        {
            var officeIdFilter = int.TryParse(criteria.OfficeIds?.Split(',')[0], out var officeId) ? officeId : (int?)null;
            return _officeSyncCache.GetBySourceType(sourceTypeOnly, officeIdFilter is > 0 ? officeIdFilter : null);
        }

        return (await _journalEntryRepository.GetJournalEntriesAsync(criteria)).ToList();
    }

    private Task<List<JournalEntry>> GetOwnerActualJournalEntriesForInvoiceAsync(Guid organizationId, int officeId, Guid invoiceId)
        => GetJournalEntriesForSourceAsync(organizationId, officeId, SourceType.Invoice, invoiceId, JournalEntryKind.OwnerActual);

    private Task<List<JournalEntry>> GetFeesActualJournalEntriesForInvoiceAsync(Guid organizationId, int officeId, Guid invoiceId)
        => GetJournalEntriesForSourceAsync(organizationId, officeId, SourceType.Invoice, invoiceId, JournalEntryKind.FeesActual);

    private async Task<List<JournalEntry>> GetAllJournalEntriesForInvoiceAsync(Guid organizationId, int officeId, Guid invoiceId)
        => await GetJournalEntriesForSourceAsync(organizationId, officeId, SourceType.Invoice, invoiceId);

    private static bool MatchesJournalEntryAccountingPeriod(JournalEntry entry, DateOnly accountingPeriod)
    {
        if (entry.AccountingPeriod != default)
            return entry.AccountingPeriod == accountingPeriod;

        return entry.TransactionDate == accountingPeriod;
    }

    private static List<JournalEntry> FilterInvoiceJournalEntriesForAccountingPeriod(IEnumerable<JournalEntry> entries, DateOnly accountingPeriod)
        => entries.Where(entry => MatchesJournalEntryAccountingPeriod(entry, accountingPeriod)).ToList();

    private static bool IsInvoicePaymentLedgerLineJournalEntry(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
        => MatchesInvoicePaymentLedgerLineMemo(entry, invoice, paymentLedgerLine);

    private static bool MatchesInvoicePaymentLedgerLineMemo(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
    {
        if (entry.SourceId != invoice.InvoiceId || entry.SourceTypeId != (int)SourceType.Invoice)
            return false;

        return entry.JournalEntryKindId switch
        {
            JournalEntryKind.Payment => string.Equals(
                entry.Memo,
                BuildInvoicePaymentMemo(invoice.InvoiceCode, paymentLedgerLine.Description),
                StringComparison.Ordinal),
            JournalEntryKind.PrePaymentReceive or JournalEntryKind.PrePaymentApply => string.Equals(
                entry.Memo,
                BuildInvoicePrePaymentMemo(invoice.InvoiceCode, paymentLedgerLine.Description),
                StringComparison.Ordinal),
            JournalEntryKind.OwnerActual => MatchesOwnerActualPaymentJournalEntryMemo(entry, invoice, paymentLedgerLine),
            JournalEntryKind.SecurityDepositActual => MatchesSecurityDepositActualPaymentJournalEntryMemo(entry, invoice, paymentLedgerLine),
            JournalEntryKind.SecurityDepositWaiverActual => MatchesSecurityDepositWaiverActualPaymentJournalEntryMemo(entry, invoice, paymentLedgerLine),
            JournalEntryKind.FeesActual => MatchesFeesActualPaymentJournalEntryMemo(entry, invoice, paymentLedgerLine),
            _ => false
        };
    }

    private static bool MatchesOwnerActualPaymentJournalEntryMemo(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
    {
        if (entry.JournalEntryKindId != JournalEntryKind.OwnerActual
            || entry.SourceTypeId != (int)SourceType.Invoice
            || entry.SourceId != invoice.InvoiceId)
        {
            return false;
        }

        // Prefer PaymentId when both sides are stamped — same ACH/check text must not collapse two payments.
        if (paymentLedgerLine.PaymentId is { } paymentId && paymentId != Guid.Empty
            && entry.PaymentId is { } entryPaymentId && entryPaymentId != Guid.Empty)
        {
            return entryPaymentId == paymentId;
        }

        if (string.Equals(entry.Memo, BuildOwnerActualRentMemo(invoice, paymentLedgerLine), StringComparison.Ordinal))
            return true;

        var paymentDescription = (paymentLedgerLine.Description ?? string.Empty).Trim();
        if (paymentDescription.Length == 0)
            return false;

        var memo = (entry.Memo ?? string.Empty).Trim();
        return memo.Contains(": Owner: Actual:", StringComparison.Ordinal)
            && memo.EndsWith($"({paymentDescription})", StringComparison.Ordinal);
    }

    private static bool MatchesSecurityDepositActualPaymentJournalEntryMemo(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
    {
        if (entry.JournalEntryKindId != JournalEntryKind.SecurityDepositActual
            || entry.SourceTypeId != (int)SourceType.Invoice
            || entry.SourceId != invoice.InvoiceId)
        {
            return false;
        }

        var memo = (entry.Memo ?? string.Empty).Trim();
        if (!memo.Contains(": Security Deposit Actual:", StringComparison.Ordinal))
            return false;

        var paymentDescription = (paymentLedgerLine.Description ?? string.Empty).Trim();
        if (paymentDescription.Length == 0)
            return true;

        return memo.EndsWith($"({paymentDescription})", StringComparison.Ordinal);
    }

    private static bool MatchesSecurityDepositWaiverActualPaymentJournalEntryMemo(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
    {
        if (entry.JournalEntryKindId != JournalEntryKind.SecurityDepositWaiverActual
            || entry.SourceTypeId != (int)SourceType.Invoice
            || entry.SourceId != invoice.InvoiceId)
        {
            return false;
        }

        var memo = (entry.Memo ?? string.Empty).Trim();
        if (!memo.Contains(": Security Deposit Waiver Actual:", StringComparison.Ordinal))
            return false;

        var paymentDescription = (paymentLedgerLine.Description ?? string.Empty).Trim();
        if (paymentDescription.Length == 0)
            return true;

        return memo.EndsWith($"({paymentDescription})", StringComparison.Ordinal);
    }

    private static bool MatchesFeesActualPaymentJournalEntryMemo(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
    {
        if (entry.JournalEntryKindId != JournalEntryKind.FeesActual
            || entry.SourceTypeId != (int)SourceType.Invoice
            || entry.SourceId != invoice.InvoiceId)
        {
            return false;
        }

        var memo = (entry.Memo ?? string.Empty).Trim();
        if (!memo.Contains(": Fees Actual:", StringComparison.Ordinal))
            return false;

        var paymentDescription = (paymentLedgerLine.Description ?? string.Empty).Trim();
        if (paymentDescription.Length == 0)
            return true;

        return memo.EndsWith($"({paymentDescription})", StringComparison.Ordinal);
    }

    private static void AssignRebuiltJournalEntryFromExisting(JournalEntry rebuilt, JournalEntry existing, Guid currentUser)
    {
        rebuilt.JournalEntryId = existing.JournalEntryId;
        rebuilt.OrganizationId = existing.OrganizationId;
        rebuilt.OfficeId = existing.OfficeId;
        rebuilt.CreatedBy = existing.CreatedBy;
        rebuilt.ModifiedBy = currentUser;

        foreach (var line in rebuilt.JournalEntryLines)
        {
            line.JournalEntryId = existing.JournalEntryId;
            line.JournalEntryLineId = Guid.Empty;
        }
    }

    private async Task<JournalEntry?> UpsertAutoGeneratedJournalEntryAsync(JournalEntry rebuiltJournalEntry, IReadOnlyList<JournalEntry> existingEntries, Guid currentUser, Guid organizationId)
    {
        var candidates = existingEntries.ToList();

        while (candidates.Count > 1)
        {
            await DeleteOpenJournalEntryAsync(candidates[^1].JournalEntryId, organizationId);
            candidates.RemoveAt(candidates.Count - 1);
        }

        var existingEntry = candidates.FirstOrDefault();
        if (existingEntry != null)
        {
            AssignRebuiltJournalEntryFromExisting(rebuiltJournalEntry, existingEntry, currentUser);
            return await UpdateAutoGeneratedJournalEntryAsync(rebuiltJournalEntry);
        }

        return await CreateAutoGeneratedJournalEntryAsync(rebuiltJournalEntry);
    }

    /// <summary>
    /// Removes all matching entries from <paramref name="workingEntries"/> so later upserts cannot
    /// update/delete the same JE (avoids stale-id "Journal entry not found" after prune).
    /// </summary>
    private static List<JournalEntry> ClaimAllMatchingEntries(List<JournalEntry> workingEntries, Func<JournalEntry, bool> predicate)
    {
        var claimed = workingEntries.Where(predicate).ToList();
        foreach (var entry in claimed)
            workingEntries.Remove(entry);
        return claimed;
    }

    private async Task<JournalEntry?> UpsertClaimedAutoGeneratedJournalEntryAsync(JournalEntry rebuiltJournalEntry, List<JournalEntry> workingEntries, Func<JournalEntry, bool> predicate, ISet<Guid> retainedEntryIds, Guid currentUser, Guid organizationId)
    {
        var claimed = ClaimAllMatchingEntries(workingEntries, predicate);
        var updated = await UpsertAutoGeneratedJournalEntryAsync(
            rebuiltJournalEntry,
            claimed,
            currentUser,
            organizationId);
        if (updated != null)
            retainedEntryIds.Add(updated.JournalEntryId);
        return updated;
    }

    private async Task DeleteJournalEntriesExceptAsync(IEnumerable<JournalEntry> entries, IReadOnlyCollection<Guid> retainJournalEntryIds, Guid organizationId)
    {
        foreach (var entry in entries)
        {
            if (!retainJournalEntryIds.Contains(entry.JournalEntryId))
                await DeleteOpenJournalEntryAsync(entry.JournalEntryId, organizationId);
        }
    }

    private async Task DeleteClaimedOrphanJournalEntriesAsync(IEnumerable<JournalEntry> orphanEntries, Guid organizationId, Guid? currentPaymentId = null)
    {
        foreach (var entry in orphanEntries.Where(IsInvoicePaymentFlowOrphanCandidate))
        {
            if (currentPaymentId is { } paymentId && paymentId != Guid.Empty
                && entry.PaymentId is { } entryPaymentId && entryPaymentId != Guid.Empty
                && entryPaymentId != paymentId)
            {
                continue;
            }

            await DeleteOpenJournalEntryAsync(entry.JournalEntryId, organizationId);
        }
    }

    private static bool IsInvoicePaymentFlowOrphanCandidate(JournalEntry entry)
        => entry.JournalEntryKindId is JournalEntryKind.Payment
            or JournalEntryKind.PrePaymentReceive
            or JournalEntryKind.PrePaymentApply
            or JournalEntryKind.OwnerActual;

    private static (JournalEntry? Main, JournalEntry? OwnerUtility) ClassifyBillJournalEntries(IReadOnlyList<JournalEntry> entries, int ownerAccountsPayableAccountId, int pmUtilityIncomeAccountId)
    {
        JournalEntry? main = null;
        JournalEntry? ownerUtility = null;

        foreach (var entry in entries)
        {
            var accountIds = entry.JournalEntryLines
                .Select(line => line.ChartOfAccountId)
                .Where(accountId => accountId > 0)
                .ToHashSet();

            if (accountIds.Contains(ownerAccountsPayableAccountId) && accountIds.Contains(pmUtilityIncomeAccountId))
                ownerUtility = entry;
            else
                main ??= entry;
        }

        return (main, ownerUtility);
    }

    private static bool IsInvoiceChargeJournalEntry(JournalEntry entry, int accountsReceivableAccountId)
        => entry.JournalEntryKindId == JournalEntryKind.Charge
            && entry.SourceTypeId == (int)SourceType.Invoice
            && entry.JournalEntryLines.Any(line => line.ChartOfAccountId == accountsReceivableAccountId);

    private static bool IsInvoiceOwnerShareJournalEntry(JournalEntry entry)
        => entry.JournalEntryKindId == JournalEntryKind.OwnerExpected
            && entry.SourceTypeId == (int)SourceType.Invoice;

    private static bool IsInvoiceChargeOrOwnerExpectedJournalEntry(JournalEntry entry)
        => entry.SourceTypeId == (int)SourceType.Invoice
            && entry.JournalEntryKindId is JournalEntryKind.Charge or JournalEntryKind.OwnerExpected;

    private static bool IsStandardInvoicePaymentJournalEntry(JournalEntry entry)
        => entry.JournalEntryKindId == JournalEntryKind.Payment
           && entry.SourceTypeId == (int)SourceType.Invoice
           && !entry.IsCashOnly;

    private static bool IsStandardInvoicePaymentJournalEntry(JournalEntry entry, int prePaymentAccountId)
        => IsStandardInvoicePaymentJournalEntry(entry);

    private static bool IsInvoiceOwnerActualPaymentJournalEntry(JournalEntry entry)
        => entry.JournalEntryKindId == JournalEntryKind.OwnerActual
           && entry.SourceTypeId == (int)SourceType.Invoice
           && entry.IsCashOnly;

    private static bool IsInvoiceSecurityDepositActualPaymentJournalEntry(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
        => entry.JournalEntryKindId == JournalEntryKind.SecurityDepositActual
           && entry.SourceTypeId == (int)SourceType.Invoice
           && entry.IsCashOnly
           && MatchesSecurityDepositActualPaymentJournalEntryMemo(entry, invoice, paymentLedgerLine);

    private static bool IsInvoiceSecurityDepositWaiverActualPaymentJournalEntry(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
        => entry.JournalEntryKindId == JournalEntryKind.SecurityDepositWaiverActual
           && entry.SourceTypeId == (int)SourceType.Invoice
           && entry.IsCashOnly
           && MatchesSecurityDepositWaiverActualPaymentJournalEntryMemo(entry, invoice, paymentLedgerLine);

    private static bool IsInvoiceFeesActualPaymentJournalEntry(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
        => entry.JournalEntryKindId == JournalEntryKind.FeesActual
           && entry.SourceTypeId == (int)SourceType.Invoice
           && entry.IsCashOnly
           && MatchesFeesActualPaymentJournalEntryMemo(entry, invoice, paymentLedgerLine);

    private static bool IsOwnerActualJournalEntryForInvoice(JournalEntry entry, Invoice invoice)
        => entry.JournalEntryKindId == JournalEntryKind.OwnerActual
           && entry.SourceTypeId == (int)SourceType.Invoice
           && entry.SourceId == invoice.InvoiceId
           && MatchesJournalEntryAccountingPeriod(entry, invoice.AccountingPeriod);

    private static bool IsOwnerActualJournalEntryForPaymentLedgerLine(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
        => IsOwnerActualJournalEntryForInvoice(entry, invoice)
           && MatchesOwnerActualPaymentJournalEntryMemo(entry, invoice, paymentLedgerLine);

    private static bool IsInvoiceOwnerActualPaymentJournalEntry(JournalEntry entry, Invoice invoice, LedgerLine paymentLedgerLine)
        => IsInvoiceOwnerActualPaymentJournalEntry(entry)
           && MatchesInvoicePaymentLedgerLineMemo(entry, invoice, paymentLedgerLine);

    private static bool IsInvoicePrePaymentReceivedJournalEntry(JournalEntry entry, int prePaymentAccountId)
        => entry.JournalEntryKindId == JournalEntryKind.PrePaymentReceive
           && entry.SourceTypeId == (int)SourceType.Invoice;

    private static bool IsInvoicePrePaymentApplyJournalEntry(JournalEntry entry, int prePaymentAccountId)
        => entry.JournalEntryKindId == JournalEntryKind.PrePaymentApply
           && entry.SourceTypeId == (int)SourceType.Invoice;
    #endregion
}
