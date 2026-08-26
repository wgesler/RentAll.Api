using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Active only during Sync / Repair / document-link passes.
    /// JE get helpers prefer this over per-document repository round-trips.
    /// </summary>
    private AccountingSyncOfficeCache? _officeSyncCache;

    /// <summary>
    /// Office-scoped snapshot: documents + journal entries loaded once, then resolved in memory.
    /// </summary>
    private sealed class AccountingSyncOfficeCache
    {
        public Guid OrganizationId { get; init; }
        public string OfficeIds { get; init; } = string.Empty;

        public List<Payment> Payments { get; init; } = [];
        public List<Deposit> Deposits { get; init; } = [];
        public List<Transfer> Transfers { get; init; } = [];
        public Dictionary<Guid, Payment> PaymentsById { get; } = new();
        public Dictionary<Guid, Deposit> DepositsById { get; } = new();
        public Dictionary<Guid, Transfer> TransfersById { get; } = new();
        public Dictionary<Guid, Invoice> InvoicesById { get; } = new();

        public Dictionary<Guid, JournalEntry> JournalEntriesById { get; } = new();
        public Dictionary<Guid, JournalEntryLine> JournalEntryLinesById { get; } = new();
        public Dictionary<(int SourceTypeId, Guid SourceId), List<JournalEntry>> BySource { get; } = new();
        public Dictionary<Guid, List<JournalEntry>> ByPaymentId { get; } = new();
        public Dictionary<Guid, List<JournalEntry>> ByDepositId { get; } = new();
        public Dictionary<Guid, List<JournalEntry>> ByTransferId { get; } = new();
        public Dictionary<Guid, Guid> DepositIdByJournalEntryLineId { get; } = new();
        public Dictionary<Guid, Guid> PaymentIdByJournalEntryLineId { get; } = new();
        public HashSet<(int SourceTypeId, Guid SourceId)> FullyLoadedSources { get; } = new();

        private readonly Dictionary<(int OfficeId, int EscrowAccountId), List<TransferDepositInvoiceEscrowMatch>> _transferInvoiceMatches = new();
        private readonly Dictionary<(int OfficeId, int EscrowAccountId), List<EscrowDepositLineCandidate>> _escrowCandidates = new();
        private readonly Dictionary<(int OfficeId, int UndepositedFundsAccountId), List<UndepositedPaymentLineCandidate>> _undepositedCandidates = new();

        public void IndexDocuments(
            IEnumerable<Payment> payments,
            IEnumerable<Deposit> deposits,
            IEnumerable<Transfer> transfers,
            IEnumerable<Invoice>? invoices = null)
        {
            Payments.Clear();
            Deposits.Clear();
            Transfers.Clear();
            PaymentsById.Clear();
            DepositsById.Clear();
            TransfersById.Clear();
            InvoicesById.Clear();

            foreach (var payment in payments)
            {
                Payments.Add(payment);
                if (payment.PaymentId != Guid.Empty)
                    PaymentsById[payment.PaymentId] = payment;
            }

            foreach (var deposit in deposits)
            {
                Deposits.Add(deposit);
                if (deposit.DepositId != Guid.Empty)
                    DepositsById[deposit.DepositId] = deposit;
            }

            foreach (var transfer in transfers)
            {
                Transfers.Add(transfer);
                if (transfer.TransferId != Guid.Empty)
                    TransfersById[transfer.TransferId] = transfer;
            }

            if (invoices == null)
                return;

            foreach (var invoice in invoices)
            {
                if (invoice.InvoiceId != Guid.Empty)
                    InvoicesById[invoice.InvoiceId] = invoice;
            }
        }

        public void IndexJournalEntries(IEnumerable<JournalEntry> entries, bool markSourceFullyLoaded = false)
        {
            foreach (var entry in entries)
                UpsertJournalEntry(entry, markSourceFullyLoaded);
        }

        public void UpsertJournalEntry(JournalEntry entry, bool markSourceFullyLoaded = false)
        {
            if (entry.JournalEntryId == Guid.Empty)
                return;

            // RemoveJournalEntry invalidates when the JE already existed; always invalidate
            // after upsert so same-pass creates also refresh rematch candidate lists.
            RemoveJournalEntry(entry.JournalEntryId, keepLineIndexCleanupOnly: false);
            InvalidateDerivedRematchIndexes();

            JournalEntriesById[entry.JournalEntryId] = entry;

            if (entry.SourceId is { } sourceId
                && sourceId != Guid.Empty
                && entry.SourceTypeId is int sourceTypeId
                && sourceTypeId > 0)
            {
                var sourceKey = (sourceTypeId, sourceId);
                if (!BySource.TryGetValue(sourceKey, out var bySource))
                {
                    bySource = [];
                    BySource[sourceKey] = bySource;
                }

                bySource.Add(entry);
                if (markSourceFullyLoaded)
                    FullyLoadedSources.Add(sourceKey);
            }

            if (NormalizeOptionalGuid(entry.PaymentId) is { } paymentId)
                AddToIndex(ByPaymentId, paymentId, entry);

            var depositId = NormalizeOptionalGuid(entry.DepositId);
            if (depositId == null && entry.SourceTypeId == (int)SourceType.Deposit)
                depositId = NormalizeOptionalGuid(entry.SourceId);
            if (depositId is { } linkedDepositId)
                AddToIndex(ByDepositId, linkedDepositId, entry);

            var transferId = NormalizeOptionalGuid(entry.TransferId);
            if (transferId == null && entry.SourceTypeId == (int)SourceType.Transfer)
                transferId = NormalizeOptionalGuid(entry.SourceId);
            if (transferId is { } linkedTransferId)
                AddToIndex(ByTransferId, linkedTransferId, entry);

            foreach (var line in entry.JournalEntryLines ?? [])
            {
                if (line.JournalEntryLineId == Guid.Empty)
                    continue;

                JournalEntryLinesById[line.JournalEntryLineId] = line;
                if (depositId is { } lineDepositId)
                    DepositIdByJournalEntryLineId[line.JournalEntryLineId] = lineDepositId;
                if (NormalizeOptionalGuid(entry.PaymentId) is { } linePaymentId)
                    PaymentIdByJournalEntryLineId[line.JournalEntryLineId] = linePaymentId;
            }
        }

        public void RemoveJournalEntry(Guid journalEntryId, bool keepLineIndexCleanupOnly = true)
        {
            if (journalEntryId == Guid.Empty)
                return;

            if (!JournalEntriesById.TryGetValue(journalEntryId, out var existing))
                return;

            JournalEntriesById.Remove(journalEntryId);

            if (existing.SourceId is { } sourceId
                && sourceId != Guid.Empty
                && existing.SourceTypeId is int sourceTypeId
                && sourceTypeId > 0)
            {
                RemoveFromIndex(BySource, (sourceTypeId, sourceId), journalEntryId);
            }

            if (NormalizeOptionalGuid(existing.PaymentId) is { } paymentId)
                RemoveFromIndex(ByPaymentId, paymentId, journalEntryId);

            var depositId = NormalizeOptionalGuid(existing.DepositId);
            if (depositId == null && existing.SourceTypeId == (int)SourceType.Deposit)
                depositId = NormalizeOptionalGuid(existing.SourceId);
            if (depositId is { } linkedDepositId)
                RemoveFromIndex(ByDepositId, linkedDepositId, journalEntryId);

            var transferId = NormalizeOptionalGuid(existing.TransferId);
            if (transferId == null && existing.SourceTypeId == (int)SourceType.Transfer)
                transferId = NormalizeOptionalGuid(existing.SourceId);
            if (transferId is { } linkedTransferId)
                RemoveFromIndex(ByTransferId, linkedTransferId, journalEntryId);

            foreach (var line in existing.JournalEntryLines ?? [])
            {
                if (line.JournalEntryLineId == Guid.Empty)
                    continue;

                JournalEntryLinesById.Remove(line.JournalEntryLineId);
                DepositIdByJournalEntryLineId.Remove(line.JournalEntryLineId);
                PaymentIdByJournalEntryLineId.Remove(line.JournalEntryLineId);
            }

            InvalidateDerivedRematchIndexes();
            _ = keepLineIndexCleanupOnly;
        }

        private void InvalidateDerivedRematchIndexes()
        {
            _transferInvoiceMatches.Clear();
            _escrowCandidates.Clear();
            _undepositedCandidates.Clear();
        }

        public void ReplaceDeposit(Deposit deposit)
        {
            if (deposit.DepositId == Guid.Empty)
                return;

            DepositsById[deposit.DepositId] = deposit;
            for (var index = 0; index < Deposits.Count; index++)
            {
                if (Deposits[index].DepositId != deposit.DepositId)
                    continue;
                Deposits[index] = deposit;
                return;
            }

            Deposits.Add(deposit);
        }

        public void ReplaceTransfer(Transfer transfer)
        {
            if (transfer.TransferId == Guid.Empty)
                return;

            TransfersById[transfer.TransferId] = transfer;
            for (var index = 0; index < Transfers.Count; index++)
            {
                if (Transfers[index].TransferId != transfer.TransferId)
                    continue;
                Transfers[index] = transfer;
                return;
            }

            Transfers.Add(transfer);
        }

        public bool TryGetJournalEntry(Guid journalEntryId, out JournalEntry? entry)
            => JournalEntriesById.TryGetValue(journalEntryId, out entry);

        public bool TryGetJournalEntryLine(Guid journalEntryLineId, out JournalEntryLine? line)
            => JournalEntryLinesById.TryGetValue(journalEntryLineId, out line);

        public bool TryGetDepositIdForLine(Guid journalEntryLineId, out Guid depositId)
            => DepositIdByJournalEntryLineId.TryGetValue(journalEntryLineId, out depositId);

        public bool TryGetPaymentIdForLine(Guid journalEntryLineId, out Guid paymentId)
            => PaymentIdByJournalEntryLineId.TryGetValue(journalEntryLineId, out paymentId);

        public IReadOnlyList<JournalEntry> GetBySource(int sourceTypeId, Guid sourceId, JournalEntryKind? journalEntryKind = null)
        {
            if (!BySource.TryGetValue((sourceTypeId, sourceId), out var entries))
                return [];

            // Always copy — callers update JEs (TouchOfficeSyncCache) which mutates these indexes.
            if (journalEntryKind == null)
                return entries.ToList();

            return entries.Where(entry => entry.JournalEntryKindId == journalEntryKind.Value).ToList();
        }

        public bool HasFullyLoadedSource(int sourceTypeId, Guid sourceId)
            => FullyLoadedSources.Contains((sourceTypeId, sourceId));

        public IReadOnlyList<JournalEntry> GetByPaymentId(Guid paymentId)
            => ByPaymentId.TryGetValue(paymentId, out var entries) ? entries.ToList() : [];

        public IReadOnlyList<JournalEntry> GetByDepositId(Guid depositId)
            => ByDepositId.TryGetValue(depositId, out var entries) ? entries.ToList() : [];

        public IReadOnlyList<JournalEntry> GetByTransferId(Guid transferId)
            => ByTransferId.TryGetValue(transferId, out var entries) ? entries.ToList() : [];

        public IReadOnlyList<JournalEntry> GetBySourceType(int sourceTypeId, int? officeId = null)
        {
            // Snapshot Values — TouchOfficeSyncCache mutates JournalEntriesById during sync loops.
            var matches = new List<JournalEntry>();
            foreach (var entry in JournalEntriesById.Values.ToList())
            {
                if (entry.SourceTypeId != sourceTypeId)
                    continue;
                if (officeId is > 0 && entry.OfficeId != officeId.Value)
                    continue;
                matches.Add(entry);
            }

            return matches;
        }

        public HashSet<Guid> GetClaimedTransferLineIdsExcluding(Guid transferId)
        {
            var claimedLineIds = new HashSet<Guid>();
            foreach (var otherTransfer in Transfers.ToList())
            {
                if (otherTransfer.TransferId == transferId || otherTransfer.IsActive == false)
                    continue;

                foreach (var split in otherTransfer.Splits ?? [])
                {
                    if (split.JournalEntryLineId is { } journalEntryLineId && journalEntryLineId != Guid.Empty)
                        claimedLineIds.Add(journalEntryLineId);
                }
            }

            return claimedLineIds;
        }

        public HashSet<Guid> GetClaimedDepositLineIdsExcluding(Guid depositId)
        {
            var claimedLineIds = new HashSet<Guid>();
            foreach (var otherDeposit in Deposits.ToList())
            {
                if (otherDeposit.DepositId == depositId || otherDeposit.IsActive == false)
                    continue;

                foreach (var split in otherDeposit.Splits ?? [])
                {
                    if (split.JournalEntryLineId is { } journalEntryLineId && journalEntryLineId != Guid.Empty)
                        claimedLineIds.Add(journalEntryLineId);
                }
            }

            return claimedLineIds;
        }

        public List<TransferDepositInvoiceEscrowMatch> GetOrBuildTransferInvoiceMatches(
            Transfer transfer,
            int escrowDepositAccountId,
            Func<Deposit, int, JournalEntryLine?> resolveEscrowLine)
        {
            var key = (transfer.OfficeId, escrowDepositAccountId);
            if (_transferInvoiceMatches.TryGetValue(key, out var cached))
                return cached.ToList();

            var matches = new List<TransferDepositInvoiceEscrowMatch>();
            foreach (var deposit in Deposits.ToList())
            {
                if (deposit.OfficeId != transfer.OfficeId
                    || deposit.IsActive == false
                    || deposit.Splits == null
                    || deposit.Splits.Count == 0)
                {
                    continue;
                }

                var escrowLine = resolveEscrowLine(deposit, escrowDepositAccountId);
                if (escrowLine == null)
                    continue;

                var escrowAmount = RoundCurrency(escrowLine.Debit - escrowLine.Credit);
                if (Math.Abs(escrowAmount) <= 0.005m)
                    continue;

                foreach (var split in deposit.Splits)
                {
                    if (Math.Abs(split.Amount) <= 0.005m)
                        continue;

                    var invoiceSourceCode = ResolveDepositSplitInvoiceSourceCode(split);
                    if (string.IsNullOrWhiteSpace(invoiceSourceCode))
                        continue;

                    matches.Add(new TransferDepositInvoiceEscrowMatch
                    {
                        DepositId = deposit.DepositId,
                        DepositCode = deposit.DepositCode ?? string.Empty,
                        EscrowJournalEntryLineId = escrowLine.JournalEntryLineId,
                        EscrowLineAmount = escrowAmount,
                        InvoiceSourceCode = invoiceSourceCode,
                        PropertyId = NormalizeOptionalGuid(split.PropertyId),
                        DepositSplitAmount = RoundCurrency(split.Amount),
                        DepositDate = deposit.DepositDate
                    });
                }
            }

            _transferInvoiceMatches[key] = matches;
            return matches.ToList();
        }

        public List<EscrowDepositLineCandidate> GetOrBuildEscrowCandidates(Transfer transfer, int escrowDepositAccountId)
        {
            var key = (transfer.OfficeId, escrowDepositAccountId);
            if (_escrowCandidates.TryGetValue(key, out var cached))
                return cached.ToList();

            var candidates = new List<EscrowDepositLineCandidate>();
            foreach (var depositEntry in GetBySourceType((int)SourceType.Deposit, transfer.OfficeId))
            {
                foreach (var line in depositEntry.JournalEntryLines ?? [])
                {
                    if (line.ChartOfAccountId != escrowDepositAccountId)
                        continue;

                    var netAmount = line.Debit - line.Credit;
                    if (Math.Abs(netAmount) <= 0.005m)
                        continue;

                    candidates.Add(new EscrowDepositLineCandidate
                    {
                        JournalEntryLineId = line.JournalEntryLineId,
                        NetAmount = netAmount,
                        PropertyId = NormalizeOptionalGuid(line.PropertyId),
                        ReservationId = NormalizeOptionalGuid(line.ReservationId),
                        ContactId = NormalizeOptionalGuid(line.ContactId),
                        DepositId = NormalizeOptionalGuid(depositEntry.DepositId)
                            ?? NormalizeOptionalGuid(depositEntry.SourceId),
                        TransactionDate = depositEntry.TransactionDate
                    });
                }
            }

            _escrowCandidates[key] = candidates;
            return candidates.ToList();
        }

        public List<UndepositedPaymentLineCandidate> GetOrBuildUndepositedCandidates(
            Deposit deposit,
            int undepositedFundsAccountId,
            Func<JournalEntry, bool> isPaymentCandidate,
            Func<JournalEntry, string> resolveSourceCode)
        {
            var key = (deposit.OfficeId, undepositedFundsAccountId);
            if (_undepositedCandidates.TryGetValue(key, out var cached))
                return cached.ToList();

            var candidates = new List<UndepositedPaymentLineCandidate>();
            foreach (var paymentEntry in GetBySourceType((int)SourceType.Invoice, deposit.OfficeId))
            {
                if (!isPaymentCandidate(paymentEntry))
                    continue;

                var sourceCode = resolveSourceCode(paymentEntry);
                if (string.IsNullOrWhiteSpace(sourceCode))
                    continue;

                foreach (var line in paymentEntry.JournalEntryLines ?? [])
                {
                    if (line.ChartOfAccountId != undepositedFundsAccountId)
                        continue;

                    var netAmount = line.Debit - line.Credit;
                    if (Math.Abs(netAmount) <= 0.005m)
                        continue;

                    candidates.Add(new UndepositedPaymentLineCandidate
                    {
                        JournalEntryLineId = line.JournalEntryLineId,
                        NetAmount = netAmount,
                        PropertyId = NormalizeOptionalGuid(line.PropertyId),
                        ReservationId = NormalizeOptionalGuid(line.ReservationId),
                        ContactId = NormalizeOptionalGuid(line.ContactId),
                        DepositId = NormalizeOptionalGuid(paymentEntry.DepositId),
                        SourceCode = sourceCode,
                        TransactionDate = paymentEntry.TransactionDate
                    });
                }
            }

            _undepositedCandidates[key] = candidates;
            return candidates.ToList();
        }

        /// <summary>
        /// Criteria invoice lists often omit ledger lines — only use cache when lines are present.
        /// </summary>
        public bool TryGetInvoiceWithLedgerLines(Guid invoiceId, out Invoice? invoice)
        {
            invoice = null;
            if (!InvoicesById.TryGetValue(invoiceId, out var cached))
                return false;

            if (cached.LedgerLines == null || cached.LedgerLines.Count == 0)
                return false;

            invoice = cached;
            return true;
        }

        private static void AddToIndex(Dictionary<Guid, List<JournalEntry>> index, Guid key, JournalEntry entry)
        {
            if (!index.TryGetValue(key, out var list))
            {
                list = [];
                index[key] = list;
            }

            list.Add(entry);
        }

        private static void RemoveFromIndex(Dictionary<Guid, List<JournalEntry>> index, Guid key, Guid journalEntryId)
        {
            if (!index.TryGetValue(key, out var list))
                return;

            list.RemoveAll(entry => entry.JournalEntryId == journalEntryId);
            if (list.Count == 0)
                index.Remove(key);
        }

        private static void RemoveFromIndex(
            Dictionary<(int SourceTypeId, Guid SourceId), List<JournalEntry>> index,
            (int SourceTypeId, Guid SourceId) key,
            Guid journalEntryId)
        {
            if (!index.TryGetValue(key, out var list))
                return;

            list.RemoveAll(entry => entry.JournalEntryId == journalEntryId);
            if (list.Count == 0)
                index.Remove(key);
        }
    }

    private async Task EnsureOfficeSyncCacheAsync(
        Guid organizationId,
        string officeIds,
        IReadOnlyList<Payment>? paymentsAlreadyLoaded = null,
        IReadOnlyList<Deposit>? depositsAlreadyLoaded = null,
        IReadOnlyList<Transfer>? transfersAlreadyLoaded = null)
    {
        if (_officeSyncCache != null
            && _officeSyncCache.OrganizationId == organizationId
            && string.Equals(_officeSyncCache.OfficeIds, officeIds, StringComparison.Ordinal))
        {
            return;
        }

        _officeSyncCache = await BuildAccountingSyncOfficeCacheAsync(
            organizationId,
            officeIds,
            paymentsAlreadyLoaded,
            depositsAlreadyLoaded,
            transfersAlreadyLoaded);
    }

    private void ClearOfficeSyncCache()
        => _officeSyncCache = null;

    private async Task WithOfficeSyncCacheAsync(
        Guid organizationId,
        string officeIds,
        Func<Task> action,
        IReadOnlyList<Payment>? paymentsAlreadyLoaded = null,
        IReadOnlyList<Deposit>? depositsAlreadyLoaded = null,
        IReadOnlyList<Transfer>? transfersAlreadyLoaded = null)
    {
        var ownsCache = _officeSyncCache == null;
        try
        {
            await EnsureOfficeSyncCacheAsync(
                organizationId,
                officeIds,
                paymentsAlreadyLoaded,
                depositsAlreadyLoaded,
                transfersAlreadyLoaded);
            await action();
        }
        finally
        {
            if (ownsCache)
                ClearOfficeSyncCache();
        }
    }

    private async Task<T> WithOfficeSyncCacheAsync<T>(
        Guid organizationId,
        string officeIds,
        Func<Task<T>> action,
        IReadOnlyList<Payment>? paymentsAlreadyLoaded = null,
        IReadOnlyList<Deposit>? depositsAlreadyLoaded = null,
        IReadOnlyList<Transfer>? transfersAlreadyLoaded = null)
    {
        var ownsCache = _officeSyncCache == null;
        try
        {
            await EnsureOfficeSyncCacheAsync(
                organizationId,
                officeIds,
                paymentsAlreadyLoaded,
                depositsAlreadyLoaded,
                transfersAlreadyLoaded);
            return await action();
        }
        finally
        {
            if (ownsCache)
                ClearOfficeSyncCache();
        }
    }

    private async Task<AccountingSyncOfficeCache> BuildAccountingSyncOfficeCacheAsync(
        Guid organizationId,
        string officeIds,
        IReadOnlyList<Payment>? paymentsAlreadyLoaded = null,
        IReadOnlyList<Deposit>? depositsAlreadyLoaded = null,
        IReadOnlyList<Transfer>? transfersAlreadyLoaded = null)
    {
        var payments = paymentsAlreadyLoaded?.ToList()
            ?? (await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds, (int)PaymentDirection.Inbound)).ToList();

        var deposits = depositsAlreadyLoaded?.ToList()
            ?? (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeIds,
                IncludeInactive = true
            })).ToList();

        var transfers = transfersAlreadyLoaded?.ToList()
            ?? (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeIds,
                IncludeInactive = true
            })).ToList();

        var invoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            IncludePaid = true
        })).ToList();

        // One office-wide JE pull (non-cash). Cash-only invoice/payment JEs enriched below.
        var journalEntries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            StartDate = DateOnly.MinValue,
            IncludeUnposted = true
        })).ToList();

        var cache = new AccountingSyncOfficeCache
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds
        };
        cache.IndexDocuments(payments, deposits, transfers, invoices);
        cache.IndexJournalEntries(journalEntries);

        // Cash-only payment side-effect JEs are excluded from GetByCriteria — load once per invoice.
        var invoiceIds = payments
            .SelectMany(payment => payment.LedgerLines ?? [])
            .Select(line => line.InvoiceId)
            .Where(invoiceId => invoiceId != Guid.Empty)
            .Distinct()
            .ToList();

        foreach (var invoiceId in invoiceIds)
        {
            if (cache.HasFullyLoadedSource((int)SourceType.Invoice, invoiceId))
                continue;

            var invoiceEntries = (await _journalEntryRepository.GetJournalEntriesBySourceIdAsync(new JournalEntryGetBySourceIdCriteria
            {
                OrganizationId = organizationId,
                SourceTypeId = (int)SourceType.Invoice,
                SourceId = invoiceId,
                OfficeIds = officeIds,
                IncludeUnposted = true,
                IncludeCashOnly = true
            })).ToList();

            cache.IndexJournalEntries(invoiceEntries, markSourceFullyLoaded: true);
        }

        foreach (var payment in payments.Where(payment => payment.PaymentId != Guid.Empty))
        {
            var paymentEntries = (await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(new JournalEntryGetByPaymentIdCriteria
            {
                OrganizationId = organizationId,
                PaymentId = payment.PaymentId
            })).ToList();

            if (paymentEntries.Count > 0)
                cache.IndexJournalEntries(paymentEntries);
        }

        return cache;
    }

    private void TouchOfficeSyncCache(JournalEntry? entry)
    {
        if (_officeSyncCache == null || entry == null || entry.JournalEntryId == Guid.Empty)
            return;

        _officeSyncCache.UpsertJournalEntry(entry);
    }

    private void RemoveFromOfficeSyncCache(Guid journalEntryId)
    {
        _officeSyncCache?.RemoveJournalEntry(journalEntryId);
    }

    private JournalEntryLine? TryGetDepositEscrowJournalEntryLineFromCache(Deposit deposit, int escrowDepositAccountId)
    {
        if (_officeSyncCache == null)
            return null;

        foreach (var depositEntry in _officeSyncCache.GetByDepositId(deposit.DepositId))
        {
            if (depositEntry.SourceTypeId != (int)SourceType.Deposit)
                continue;

            var escrowLine = depositEntry.JournalEntryLines?
                .FirstOrDefault(line => line.ChartOfAccountId == escrowDepositAccountId
                    && Math.Abs(line.Debit - line.Credit) > 0.005m);

            if (escrowLine != null)
                return escrowLine;
        }

        return null;
    }
}
