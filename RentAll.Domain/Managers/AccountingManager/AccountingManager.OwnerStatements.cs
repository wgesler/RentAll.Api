using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<IEnumerable<OwnerStatementSummary>> GetOwnerStatementsAsync(OwnerStatementGetCriteria criteria)
    {
        var summaries = (await _accountingRepository.GetOwnerStatementsAsync(criteria)).ToList();
        if (summaries.Count == 0)
            return summaries;

        var ownerPropertyKeys = summaries
            .Where(summary => summary.OwnerId.HasValue && summary.OwnerId.Value != Guid.Empty && summary.PropertyId != Guid.Empty)
            .Select(summary => (summary.PropertyId, OwnerId: summary.OwnerId!.Value))
            .Distinct()
            .ToList();
        if (ownerPropertyKeys.Count == 0)
            return summaries;
        var ownerPropertyKeySet = ownerPropertyKeys.ToHashSet();

        var expectedByKey = await GetExpectedIncomeByOwnerPropertyAsync(criteria, summaries);
        var prePaidByKey = new Dictionary<(Guid PropertyId, Guid OwnerId), decimal>();
        var propertyById = new Dictionary<Guid, Property?>();
        var officeCostCodeByOfficeId = new Dictionary<int, Dictionary<int, CostCode>>();

        async Task<Property?> GetPropertyAsync(Guid propertyId)
        {
            if (!propertyById.TryGetValue(propertyId, out var property))
            {
                property = await _propertyRepository.GetPropertyByIdAsync(propertyId, criteria.OrganizationId);
                propertyById[propertyId] = property;
            }

            return property;
        }

        async Task<Dictionary<int, CostCode>> GetOfficeCostCodeByIdAsync(int officeId)
        {
            if (!officeCostCodeByOfficeId.TryGetValue(officeId, out var costCodeById))
            {
                var officeCostCodes = await _accountingRepository.GetCostCodesByOfficeIdAsync(criteria.OrganizationId, officeId);
                costCodeById = officeCostCodes.ToDictionary(costCode => costCode.CostCodeId);
                officeCostCodeByOfficeId[officeId] = costCodeById;
            }

            return costCodeById;
        }

        static decimal GetAmountByKey(IReadOnlyDictionary<(Guid PropertyId, Guid OwnerId), decimal> source, (Guid PropertyId, Guid OwnerId) key)
        {
            return source.TryGetValue(key, out var amount) ? amount : 0m;
        }

        var invoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = true,
            IncludeInactive = false,
            IncludePaid = true,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        })).ToList();

        foreach (var invoice in invoices)
        {
            var propertyId = await ResolveInvoicePropertyIdAsync(invoice);
            if (!propertyId.HasValue || propertyId.Value == Guid.Empty)
                continue;

            var property = await GetPropertyAsync(propertyId.Value);
            if (property == null || !property.Owner1Id.HasValue || property.Owner1Id.Value == Guid.Empty || property.PropertyLeaseType != PropertyLeaseType.PropertyManagement)
                continue;

            var ownerKey = (PropertyId: propertyId.Value, OwnerId: property.Owner1Id.Value);
            if (!ownerPropertyKeySet.Contains(ownerKey))
                continue;

            var costCodeById = await GetOfficeCostCodeByIdAsync(invoice.OfficeId);
            foreach (var ledgerLine in invoice.LedgerLines.Where(line => line.Amount != 0))
            {
                if (!costCodeById.TryGetValue(ledgerLine.CostCodeId, out var costCode) || !IsPaymentLedgerLine(costCode))
                    continue;

                if (!IsInvoicePrePayment(invoice, ledgerLine))
                    continue;

                prePaidByKey[ownerKey] = GetAmountByKey(prePaidByKey, ownerKey) + ledgerLine.Amount;
            }
        }

        foreach (var summary in summaries)
        {
            if (!summary.OwnerId.HasValue || summary.OwnerId.Value == Guid.Empty || summary.PropertyId == Guid.Empty)
                continue;

            var key = (PropertyId: summary.PropertyId, OwnerId: summary.OwnerId.Value);
            var expected = GetAmountByKey(expectedByKey, key);
            var prePaid = GetAmountByKey(prePaidByKey, key);
            var outstanding = expected - prePaid;
            var workingCapitalBalanceDue = summary.Balance - summary.WorkingCapital;

            summary.Expected = expected;
            summary.PrePaid = prePaid;
            summary.Outstanding = outstanding;
            summary.WorkingCapitalBalanceDue = workingCapitalBalanceDue;
        }

        return summaries;
    }

    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetOwnerStatementJournalEntryLinesAsync(OwnerStatementJournalEntryLineGetCriteria criteria)
    {
        return await _accountingRepository.GetOwnerStatementJournalEntryLinesAsync(criteria);
    }

    public async Task<IEnumerable<OwnerStatementPropertyActivityLine>> GetOwnerStatementPropertyActivityLinesAsync(OwnerStatementPropertyActivityGetCriteria criteria)
    {
        if (criteria.PropertyId == Guid.Empty)
            return Enumerable.Empty<OwnerStatementPropertyActivityLine>();

        var property = await _propertyRepository.GetPropertyByIdAsync(criteria.PropertyId, criteria.OrganizationId);
        if (property == null)
            return Enumerable.Empty<OwnerStatementPropertyActivityLine>();

        var agreement = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(criteria.PropertyId);
        var lines = new List<OwnerStatementPropertyActivityLine>();

        var invoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = true,
            IncludeInactive = false,
            IncludePaid = true,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        })).ToList();

        foreach (var invoice in invoices)
        {
            var invoicePropertyId = await ResolveInvoicePropertyIdAsync(invoice);
            if (!invoicePropertyId.HasValue || invoicePropertyId.Value != criteria.PropertyId)
                continue;

            if (!IsWithinAccountingPeriodRange(invoice.AccountingPeriod, criteria.StartDate, criteria.EndDate))
                continue;

            var expectedIncome = 0m;
            if (agreement != null && TryGetInvoiceRentalLineAmount(invoice, out _))
            {
                var ownerPercentageBase = await GetOwnerPercentageBaseAsync(invoice);
                expectedIncome = await ResolveOwnerExpectedAmountFromInvoiceAsync(invoice, agreement, ownerPercentageBase);
            }

            lines.Add(new OwnerStatementPropertyActivityLine
            {
                ActivityId = invoice.InvoiceId,
                ActivityType = "Reservation",
                ActivityDate = invoice.AccountingPeriod != default ? invoice.AccountingPeriod : invoice.InvoiceDate,
                DocumentCode = invoice.InvoiceCode,
                Description = string.IsNullOrWhiteSpace(invoice.ReservationCode) ? invoice.InvoiceCode : invoice.ReservationCode.Trim(),
                ExpectedIncome = expectedIncome,
                Expenses = 0m
            });
        }

        foreach (var receiptKind in new[] { ReceiptKind.Bill, ReceiptKind.Card })
        {
            var receipts = await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = criteria.OfficeIds,
                PropertyId = criteria.PropertyId,
                IsActive = true,
                IncludeInactive = false,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate,
                ReceiptKind = receiptKind
            });

            foreach (var receipt in receipts)
            {
                if (!(receipt.PropertyIds ?? []).Any(propertyId => propertyId == criteria.PropertyId))
                    continue;

                if (!IsWithinAccountingPeriodRange(receipt.AccountingPeriod, criteria.StartDate, criteria.EndDate))
                    continue;

                lines.Add(new OwnerStatementPropertyActivityLine
                {
                    ActivityId = receipt.ReceiptId,
                    ActivityType = receiptKind == ReceiptKind.Bill ? "Bill" : "Receipt",
                    ActivityDate = receipt.AccountingPeriod != default ? receipt.AccountingPeriod : receipt.ReceiptDate,
                    DocumentCode = !string.IsNullOrWhiteSpace(receipt.BillNumber)
                        ? receipt.BillNumber.Trim()
                        : receipt.ReceiptCode,
                    Description = !string.IsNullOrWhiteSpace(receipt.VendorName)
                        ? receipt.VendorName!.Trim()
                        : receipt.ReceiptCode,
                    ExpectedIncome = 0m,
                    Expenses = receipt.Amount
                });
            }
        }

        var workOrders = await _maintenanceRepository.GetWorkOrdersByCriteriaAsync(new WorkOrderGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = true,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        foreach (var workOrder in workOrders)
        {
            if (workOrder.PropertyId == Guid.Empty || workOrder.PropertyId != criteria.PropertyId)
                continue;

            if (!IsWithinAccountingPeriodRange(workOrder.WorkOrderDate, criteria.StartDate, criteria.EndDate))
                continue;

            var workOrderIncome = 0m;
            var workOrderExpenses = 0m;
            if (workOrder.WorkOrderType == WorkOrderType.Tenant)
                workOrderIncome = workOrder.Amount;
            else
                workOrderExpenses = workOrder.Amount;

            lines.Add(new OwnerStatementPropertyActivityLine
            {
                ActivityId = workOrder.WorkOrderId,
                ActivityType = "WorkOrder",
                ActivityDate = workOrder.WorkOrderDate,
                DocumentCode = workOrder.WorkOrderCode,
                Description = string.IsNullOrWhiteSpace(workOrder.Description) ? workOrder.WorkOrderCode : workOrder.Description.Trim(),
                ExpectedIncome = workOrderIncome,
                Expenses = workOrderExpenses
            });
        }

        return lines
            .OrderBy(line => line.ActivityDate)
            .ThenBy(line => line.ActivityType)
            .ThenBy(line => line.DocumentCode)
            .ToList();
    }

    private async Task<Dictionary<(Guid PropertyId, Guid OwnerId), decimal>> GetExpectedIncomeByOwnerPropertyAsync(
        OwnerStatementGetCriteria criteria,
        IReadOnlyCollection<OwnerStatementSummary> summaries)
    {
        var expectedByKey = new Dictionary<(Guid PropertyId, Guid OwnerId), decimal>();
        var validKeys = summaries
            .Where(summary => summary.OwnerId.HasValue && summary.OwnerId.Value != Guid.Empty && summary.PropertyId != Guid.Empty)
            .Select(summary => (summary.PropertyId, OwnerId: summary.OwnerId!.Value))
            .ToHashSet();
        if (validKeys.Count == 0)
            return expectedByKey;

        var officeOwnerMap = summaries
            .Where(summary => summary.OwnerId.HasValue && summary.OwnerId.Value != Guid.Empty && summary.PropertyId != Guid.Empty)
            .GroupBy(summary => summary.OfficeId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(summary => summary.OwnerId!.Value).ToHashSet());

        foreach (var (officeId, officeOwnerIds) in officeOwnerMap)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(criteria.OrganizationId, officeId);
            var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice);
            var ownerShareLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                ChartOfAccountId = ownerAccountsPayableAccountId,
                SourceTypeId = (int)SourceType.Invoice,
                IncludeVoided = false,
                IncludeUnposted = true,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate
            });

            foreach (var line in ownerShareLines)
            {
                if (!line.PropertyId.HasValue || line.PropertyId.Value == Guid.Empty || !line.ContactId.HasValue || line.ContactId.Value == Guid.Empty)
                    continue;

                if (!officeOwnerIds.Contains(line.ContactId.Value))
                    continue;

                var key = (PropertyId: line.PropertyId.Value, OwnerId: line.ContactId.Value);
                if (!validKeys.Contains(key))
                    continue;

                var expectedIncome = line.Credit - line.Debit;
                if (expectedIncome == 0)
                    continue;

                expectedByKey[key] = expectedByKey.GetValueOrDefault(key, 0m) + expectedIncome;
            }
        }

        return expectedByKey;
    }

    private async Task<decimal> ResolveOwnerExpectedAmountFromInvoiceAsync(Invoice invoice, PropertyAgreement agreement, decimal ownerPercentageBase)
    {
        switch (agreement.ManagementFeeType)
        {
            case ManagementFeeType.FlatRate:
                return await GetProratedOwnerFlatAmountAsync(invoice, agreement.FlatRateAmount);
            case ManagementFeeType.Percentage:
                return ownerPercentageBase * agreement.RevenueSplitOwner / 100m;
            case ManagementFeeType.Minimum:
            {
                var ownerPercentageAmount = ownerPercentageBase * agreement.RevenueSplitOwner / 100m;
                var proratedMinimum = await GetProratedOwnerFlatAmountAsync(invoice, agreement.FlatRateAmount);
                return ownerPercentageAmount < proratedMinimum ? proratedMinimum : ownerPercentageAmount;
            }
            default:
                return 0m;
        }
    }

    private static bool IsWithinAccountingPeriodRange(DateOnly accountingPeriod, DateOnly? startDate, DateOnly? endDate)
    {
        if (accountingPeriod == default)
            return true;

        if (startDate.HasValue && accountingPeriod < startDate.Value)
            return false;

        if (endDate.HasValue && accountingPeriod > endDate.Value)
            return false;

        return true;
    }
}
