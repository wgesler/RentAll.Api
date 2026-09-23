using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task StampHealthIssuePostedJournalEntriesAsync(Guid organizationId, string officeIds, DocumentHealthResult result)
    {
        if (result.Issues.Count == 0)
            return;

        var postedIds = await LoadPostedDocumentIdsAsync(organizationId, officeIds);
        var documentType = result.Summary.DocumentType ?? string.Empty;
        OfficeAccountingGraph? graph = null;
        if (NeedsAccountingGraph(documentType))
            graph = await LoadOfficeAccountingGraphAsync(organizationId, officeIds);

        foreach (var issue in result.Issues)
        {
            if (issue.DocumentId == Guid.Empty)
                continue;

            if (graph == null)
            {
                issue.HasPostedJournalEntry = postedIds.Contains(issue.DocumentId) || (issue.RelatedId is Guid relatedId && relatedId != Guid.Empty && postedIds.Contains(relatedId));
                continue;
            }

            var chain = BuildHealthDocumentChain(documentType, issue.DocumentId, issue.RelatedId, issue.OfficeId, graph);
            issue.HasPostedJournalEntry = ChainDocumentIds(chain).Any(postedIds.Contains);
        }
    }

    public Task<JournalEntrySyncResult> RebuildHealthDocumentJournalEntriesAsync(Guid organizationId, int officeId, string documentType, Guid documentId, Guid? relatedId, Guid currentUser)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (officeId <= 0)
            throw new ArgumentException("OfficeId is required.", nameof(officeId));
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        return RebuildHealthDocumentJournalEntriesCoreAsync(organizationId, officeId, documentType, documentId, relatedId, currentUser);
    }

    private async Task<JournalEntrySyncResult> RebuildHealthDocumentJournalEntriesCoreAsync(Guid organizationId, int officeId, string documentType, Guid documentId, Guid? relatedId, Guid currentUser)
    {
        var result = new JournalEntrySyncResult { DocumentsProcessed = 1 };
        var officeIds = officeId.ToString();
        var graph = await LoadOfficeAccountingGraphAsync(organizationId, officeIds);
        var chain = BuildHealthDocumentChain(documentType, documentId, relatedId, officeId, graph);
        if (!ChainDocumentIds(chain).Any())
        {
            result.Errors.Add("Document was not found for journal entry rebuild.");
            return result;
        }

        var entries = await LoadChainJournalEntriesAsync(organizationId, chain);
        var posted = entries.FirstOrDefault(entry => entry.PostingStatusId != PostingStatus.Open);
        if (posted != null)
        {
            var postedCode = string.IsNullOrWhiteSpace(posted.JournalEntryCode) ? posted.JournalEntryId.ToString() : posted.JournalEntryCode;
            result.Errors.Add($"Posted journal entry {postedCode} blocks Fix.");
            return result;
        }

        await ClearChainSplitLinePointersAsync(organizationId, chain, currentUser);

        foreach (var entry in entries.OrderBy(DeleteRank).ThenBy(entry => entry.JournalEntryCode))
        {
            await DeleteOpenJournalEntryAsync(entry.JournalEntryId, organizationId);
            result.JournalEntriesDeleted++;
        }

        foreach (var invoiceId in chain.InvoiceIds)
            await SyncInvoiceForHealthFixAsync(organizationId, invoiceId, currentUser, result);
        foreach (var receiptId in chain.ReceiptIds)
            await SyncReceiptForHealthFixAsync(organizationId, receiptId, currentUser, result);
        foreach (var billId in chain.BillIds)
            await SyncBillForHealthFixAsync(organizationId, billId, currentUser, result);
        foreach (var workOrderId in chain.WorkOrderIds)
            await SyncWorkOrderForHealthFixAsync(organizationId, workOrderId, currentUser, result);
        foreach (var paymentId in chain.PaymentIds)
            await SyncPaymentForHealthFixAsync(organizationId, paymentId, null, currentUser, result);
        foreach (var depositId in chain.DepositIds)
            await ResaveDepositForHealthRebuildAsync(organizationId, depositId, currentUser, result);
        foreach (var transferId in chain.TransferIds)
            await ResaveTransferForHealthRebuildAsync(organizationId, transferId, currentUser, result);

        return result;
    }

    private async Task ResaveDepositForHealthRebuildAsync(Guid organizationId, Guid depositId, Guid currentUser, JournalEntrySyncResult result)
    {
        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        try
        {
            await UpdateDepositAsync(deposit, currentUser);
            result.JournalEntriesCreated++;
        }
        catch (Exception ex)
        {
            var depositCode = string.IsNullOrWhiteSpace(deposit.DepositCode) ? depositId.ToString() : deposit.DepositCode.Trim();
            result.Errors.Add($"{depositCode}: {ex.Message}");
        }
    }

    private async Task ResaveTransferForHealthRebuildAsync(Guid organizationId, Guid transferId, Guid currentUser, JournalEntrySyncResult result)
    {
        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId);
        if (transfer == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        try
        {
            await UpdateTransferAsync(transfer, currentUser);
            result.JournalEntriesCreated++;
        }
        catch (Exception ex)
        {
            var transferCode = string.IsNullOrWhiteSpace(transfer.TransferCode) ? transferId.ToString() : transfer.TransferCode.Trim();
            result.Errors.Add($"{transferCode}: {ex.Message}");
        }
    }

    private async Task<HashSet<Guid>> LoadPostedDocumentIdsAsync(Guid organizationId, string officeIds)
    {
        var entries = await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeUnposted = false,
            IncludeCashOnly = true,
            StartDate = DateOnly.MinValue
        });

        var postedIds = new HashSet<Guid>();
        foreach (var entry in entries)
        {
            if (entry.PostingStatusId == PostingStatus.Open)
                continue;

            postedIds.Add(entry.JournalEntryId);
            if (entry.SourceId is Guid sourceId && sourceId != Guid.Empty)
                postedIds.Add(sourceId);
            if (entry.PaymentId is Guid paymentId && paymentId != Guid.Empty)
                postedIds.Add(paymentId);
            if (entry.DepositId is Guid depositId && depositId != Guid.Empty)
                postedIds.Add(depositId);
            if (entry.TransferId is Guid transferId && transferId != Guid.Empty)
                postedIds.Add(transferId);
        }

        return postedIds;
    }

    private async Task<OfficeAccountingGraph> LoadOfficeAccountingGraphAsync(Guid organizationId, string officeIds)
    {
        var payments = new List<Payment>();
        foreach (var kind in new[] { PaymentKind.Invoice, PaymentKind.Bill, PaymentKind.Owner })
        {
            var rows = await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds, (int)kind);
            payments.AddRange(rows);
        }

        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true
        })).ToList();

        var transfers = (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true
        })).ToList();

        return new OfficeAccountingGraph(payments, deposits, transfers);
    }

    private static bool NeedsAccountingGraph(string documentType)
    {
        var normalized = (documentType ?? string.Empty).Trim();
        return normalized.Equals("Payment", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("PaymentInvoice", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("PaymentBill", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("PaymentOwner", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Deposit", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Transfer", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Invoice", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("DocumentLinks", StringComparison.OrdinalIgnoreCase);
    }

    private static HealthDocumentChain BuildHealthDocumentChain(string documentType, Guid documentId, Guid? relatedId, int officeId, OfficeAccountingGraph graph)
    {
        var chain = new HealthDocumentChain { OfficeId = officeId };
        AddHealthChainRoot(chain, documentType, documentId, graph);
        if (relatedId is Guid related && related != Guid.Empty && related != documentId)
            AddHealthChainRoot(chain, documentType, related, graph);
        return chain;
    }

    private static void AddHealthChainRoot(HealthDocumentChain chain, string documentType, Guid documentId, OfficeAccountingGraph graph)
    {
        var normalized = (documentType ?? string.Empty).Trim();
        if (normalized.Equals("Transfer", StringComparison.OrdinalIgnoreCase))
            AddTransferToChain(chain, documentId, graph);
        else if (normalized.Equals("Deposit", StringComparison.OrdinalIgnoreCase))
            AddDepositToChain(chain, documentId, graph);
        else if (normalized.Equals("Invoice", StringComparison.OrdinalIgnoreCase))
        {
            chain.InvoiceIds.Add(documentId);
            if (graph.PaymentsById.ContainsKey(documentId))
                AddPaymentToChain(chain, documentId, graph);
        }
        else if (normalized.Equals("Receipt", StringComparison.OrdinalIgnoreCase))
            chain.ReceiptIds.Add(documentId);
        else if (normalized.Equals("Bill", StringComparison.OrdinalIgnoreCase))
            chain.BillIds.Add(documentId);
        else if (normalized.Equals("WorkOrder", StringComparison.OrdinalIgnoreCase))
            chain.WorkOrderIds.Add(documentId);
        else if (normalized.Equals("Payment", StringComparison.OrdinalIgnoreCase) || normalized.Equals("PaymentInvoice", StringComparison.OrdinalIgnoreCase) || normalized.Equals("PaymentBill", StringComparison.OrdinalIgnoreCase) || normalized.Equals("PaymentOwner", StringComparison.OrdinalIgnoreCase))
            AddPaymentToChain(chain, documentId, graph);
        else if (graph.PaymentsById.ContainsKey(documentId))
            AddPaymentToChain(chain, documentId, graph);
        else if (graph.DepositsById.ContainsKey(documentId))
            AddDepositToChain(chain, documentId, graph);
        else if (graph.TransfersById.ContainsKey(documentId))
            AddTransferToChain(chain, documentId, graph);
    }

    private static void AddTransferToChain(HealthDocumentChain chain, Guid transferId, OfficeAccountingGraph graph)
    {
        if (transferId == Guid.Empty || !chain.TransferIds.Add(transferId))
            return;

        foreach (var deposit in graph.Deposits.Where(deposit => deposit.TransferId == transferId))
            AddDepositToChain(chain, deposit.DepositId, graph);
    }

    private static void AddDepositToChain(HealthDocumentChain chain, Guid depositId, OfficeAccountingGraph graph)
    {
        if (depositId == Guid.Empty || !chain.DepositIds.Add(depositId))
            return;

        if (graph.DepositsById.TryGetValue(depositId, out var deposit) && deposit.TransferId is Guid transferId && transferId != Guid.Empty)
            AddTransferToChain(chain, transferId, graph);

        foreach (var payment in graph.Payments.Where(payment => payment.DepositId == depositId))
            AddPaymentToChain(chain, payment.PaymentId, graph);
    }

    private static void AddPaymentToChain(HealthDocumentChain chain, Guid paymentId, OfficeAccountingGraph graph)
    {
        if (paymentId == Guid.Empty || !chain.PaymentIds.Add(paymentId))
            return;

        if (!graph.PaymentsById.TryGetValue(paymentId, out var payment))
            return;

        foreach (var line in payment.LedgerLines ?? [])
        {
            if (line.InvoiceId != Guid.Empty)
                chain.InvoiceIds.Add(line.InvoiceId);
        }

        if (payment.DepositId is Guid depositId && depositId != Guid.Empty)
            AddDepositToChain(chain, depositId, graph);
    }

    private static IEnumerable<Guid> ChainDocumentIds(HealthDocumentChain chain)
    {
        return chain.TransferIds
            .Concat(chain.DepositIds)
            .Concat(chain.PaymentIds)
            .Concat(chain.InvoiceIds)
            .Concat(chain.ReceiptIds)
            .Concat(chain.BillIds)
            .Concat(chain.WorkOrderIds);
    }

    private async Task<List<JournalEntry>> LoadChainJournalEntriesAsync(Guid organizationId, HealthDocumentChain chain)
    {
        var entries = new List<JournalEntry>();

        foreach (var transferId in chain.TransferIds)
        {
            entries.AddRange(await _journalEntryRepository.GetJournalEntriesByTransferIdAsync(new JournalEntryGetByTransferIdCriteria { OrganizationId = organizationId, TransferId = transferId }));
            if (chain.OfficeId > 0)
                entries.AddRange(await GetDocumentJournalEntriesForSyncAsync(organizationId, chain.OfficeId, SourceType.Transfer, transferId));
        }

        foreach (var depositId in chain.DepositIds)
        {
            entries.AddRange(await _journalEntryRepository.GetJournalEntriesByDepositIdAsync(new JournalEntryGetByDepositIdCriteria { OrganizationId = organizationId, DepositId = depositId }));
            if (chain.OfficeId > 0)
                entries.AddRange(await GetDocumentJournalEntriesForSyncAsync(organizationId, chain.OfficeId, SourceType.Deposit, depositId));
        }

        foreach (var paymentId in chain.PaymentIds)
        {
            entries.AddRange(await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(new JournalEntryGetByPaymentIdCriteria { OrganizationId = organizationId, PaymentId = paymentId }));
            if (chain.OfficeId > 0)
            {
                entries.AddRange(await GetDocumentJournalEntriesForSyncAsync(organizationId, chain.OfficeId, SourceType.InvoicePayment, paymentId));
                entries.AddRange(await GetDocumentJournalEntriesForSyncAsync(organizationId, chain.OfficeId, SourceType.BillPayment, paymentId));
            }
        }

        foreach (var invoiceId in chain.InvoiceIds)
        {
            if (chain.OfficeId > 0)
                entries.AddRange(await GetDocumentJournalEntriesForSyncAsync(organizationId, chain.OfficeId, SourceType.Invoice, invoiceId));
        }

        foreach (var receiptId in chain.ReceiptIds)
        {
            if (chain.OfficeId > 0)
                entries.AddRange(await GetDocumentJournalEntriesForSyncAsync(organizationId, chain.OfficeId, SourceType.Receipt, receiptId));
        }

        foreach (var billId in chain.BillIds)
        {
            if (chain.OfficeId > 0)
                entries.AddRange(await GetDocumentJournalEntriesForSyncAsync(organizationId, chain.OfficeId, SourceType.Bill, billId));
        }

        foreach (var workOrderId in chain.WorkOrderIds)
        {
            if (chain.OfficeId > 0)
                entries.AddRange(await GetDocumentJournalEntriesForSyncAsync(organizationId, chain.OfficeId, SourceType.WorkOrder, workOrderId));
        }

        return entries.GroupBy(entry => entry.JournalEntryId).Select(group => group.First()).ToList();
    }

    private async Task ClearChainSplitLinePointersAsync(Guid organizationId, HealthDocumentChain chain, Guid currentUser)
    {
        foreach (var depositId in chain.DepositIds)
        {
            var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
            if (deposit?.Splits == null || deposit.Splits.Count == 0)
                continue;
            if (deposit.Splits.All(split => split.JournalEntryLineId == null || split.JournalEntryLineId == Guid.Empty))
                continue;

            foreach (var split in deposit.Splits)
                split.JournalEntryLineId = null;

            deposit.ModifiedBy = currentUser;
            await _accountingRepository.UpdateDepositAsync(deposit);
        }

        foreach (var transferId in chain.TransferIds)
        {
            var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId);
            if (transfer?.Splits == null || transfer.Splits.Count == 0)
                continue;
            if (transfer.Splits.All(split => split.JournalEntryLineId == null || split.JournalEntryLineId == Guid.Empty))
                continue;

            foreach (var split in transfer.Splits)
                split.JournalEntryLineId = null;

            transfer.ModifiedBy = currentUser;
            await _accountingRepository.UpdateTransferAsync(transfer);
        }
    }

    private static int DeleteRank(JournalEntry entry)
    {
        var sourceType = entry.SourceTypeId;
        if (sourceType == (int)SourceType.Transfer)
            return 0;
        if (sourceType == (int)SourceType.Deposit)
            return 1;
        if (sourceType == (int)SourceType.InvoicePayment || sourceType == (int)SourceType.BillPayment || sourceType == (int)SourceType.OwnerDistribution)
            return 2;
        if (entry.TransferId is Guid transferId && transferId != Guid.Empty && (entry.DepositId == null || entry.DepositId == Guid.Empty) && (entry.PaymentId == null || entry.PaymentId == Guid.Empty))
            return 0;
        if (entry.DepositId is Guid depositId && depositId != Guid.Empty && (entry.PaymentId == null || entry.PaymentId == Guid.Empty))
            return 1;
        if (entry.PaymentId is Guid paymentId && paymentId != Guid.Empty)
            return 2;
        return 3;
    }

    private sealed class HealthDocumentChain
    {
        public int OfficeId { get; set; }
        public HashSet<Guid> TransferIds { get; } = [];
        public HashSet<Guid> DepositIds { get; } = [];
        public HashSet<Guid> PaymentIds { get; } = [];
        public HashSet<Guid> InvoiceIds { get; } = [];
        public HashSet<Guid> ReceiptIds { get; } = [];
        public HashSet<Guid> BillIds { get; } = [];
        public HashSet<Guid> WorkOrderIds { get; } = [];
    }

    private sealed class OfficeAccountingGraph
    {
        public OfficeAccountingGraph(IEnumerable<Payment> payments, IEnumerable<Deposit> deposits, IEnumerable<Transfer> transfers)
        {
            Payments = payments.ToList();
            Deposits = deposits.ToList();
            Transfers = transfers.ToList();
            PaymentsById = Payments.Where(payment => payment.PaymentId != Guid.Empty).GroupBy(payment => payment.PaymentId).ToDictionary(group => group.Key, group => group.First());
            DepositsById = Deposits.Where(deposit => deposit.DepositId != Guid.Empty).GroupBy(deposit => deposit.DepositId).ToDictionary(group => group.Key, group => group.First());
            TransfersById = Transfers.Where(transfer => transfer.TransferId != Guid.Empty).GroupBy(transfer => transfer.TransferId).ToDictionary(group => group.Key, group => group.First());
        }

        public List<Payment> Payments { get; }
        public List<Deposit> Deposits { get; }
        public List<Transfer> Transfers { get; }
        public Dictionary<Guid, Payment> PaymentsById { get; }
        public Dictionary<Guid, Deposit> DepositsById { get; }
        public Dictionary<Guid, Transfer> TransfersById { get; }
    }
}
