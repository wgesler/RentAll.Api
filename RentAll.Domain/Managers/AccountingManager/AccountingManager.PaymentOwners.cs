using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Payment Document
    public async Task<List<JournalEntry>> ApplyPaymentToOwnersAsync(OwnerPayments ownerPayments, Guid organizationId, string offices, Guid currentUser)
    {
        if (ownerPayments.Payments.Count == 0)
            throw new Exception("No owner payments submitted");

        var accessOfficeIds = ParseOfficeIdsFromAccess(offices);
        var journalEntries = new List<JournalEntry>();
        foreach (var payment in ownerPayments.Payments)
        {
            if (accessOfficeIds.Count > 0 && !accessOfficeIds.Contains(payment.OfficeId))
                throw new Exception($"Office access denied for office {payment.OfficeId}");

            var property = await _propertyRepository.GetPropertyByIdAsync(payment.PropertyId, organizationId);
            if (property == null || property.OfficeId != payment.OfficeId)
                throw new Exception("Invalid property for owner payment");

            if (property.Owner1Id != payment.OwnerId && property.Owner2Id != payment.OwnerId)
                throw new Exception("Owner does not match property for owner payment");

            string? ownerName = null;
            var contact = await _contactRepository.GetContactByIdsAsync(payment.OwnerId, organizationId);
            if (contact != null)
                ownerName = NormalizeOptionalString(contact.DisplayName ?? contact.CompanyName ?? contact.FullName);

            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, payment.OfficeId);
            var chartOfAccountId = payment.ChartOfAccountId > 0
                ? payment.ChartOfAccountId
                : GetDefaultBankAccount(chartOfAccounts, payment.OfficeId, accountingOffice);
            var paymentReference = string.IsNullOrWhiteSpace(payment.Description)
                ? FormatOwnerPaymentReference(payment.PaymentType)
                : payment.Description.Trim();
            var propertyCode = NormalizeOptionalString(property.PropertyCode) ?? string.Empty;
            var ownerPayment = new Payment
            {
                OrganizationId = organizationId,
                OfficeId = payment.OfficeId,
                PaymentDate = ownerPayments.PaymentDate,
                Amount = payment.Amount,
                Description = paymentReference,
                PaymentKindId = (int)PaymentKind.Owner,
                PaymentTypeId = (int)payment.PaymentType,
                ChartOfAccountId = chartOfAccountId,
                IsActive = true,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            EnsureOwnerPayment(ownerPayment);
            ownerPayment.CostCodeId = await ResolveOwnerPaymentCostCodeIdAsync(organizationId, payment.OfficeId);

            var allocation = new PaymentOwnerAllocation
            {
                OwnerId = payment.OwnerId,
                OwnerName = ownerName ?? string.Empty,
                PropertyId = payment.PropertyId,
                PropertyCode = propertyCode,
                LineNumber = 1,
                Amount = payment.Amount,
                Description = paymentReference
            };
            ValidateExplicitOwnerPaymentAllocations(ownerPayment, [allocation]);

            Payment? createdPayment = null;
            try
            {
                await EnsurePaymentCodeAsync(ownerPayment);
                createdPayment = await _accountingRepository.CreatePaymentWithOwnerAllocationsAsync(ownerPayment, [allocation], currentUser);
                var createdEntries = await CreateJournalEntriesFromOwnerPaymentDocumentAsync(createdPayment.PaymentId, organizationId, currentUser);
                journalEntries.AddRange(createdEntries);
            }
            catch
            {
                if (createdPayment != null)
                    await TryDeleteIncompleteOwnerPaymentAsync(createdPayment.PaymentId, organizationId, currentUser);

                throw;
            }
        }

        return journalEntries;
    }

    public async Task<Payment> CreatePaymentWithOwnerAllocationsAsync(Payment payment, IReadOnlyList<PaymentOwnerAllocation> allocations, Guid currentUser)
    {
        EnsureOwnerPayment(payment);

        if (payment.ChartOfAccountId is not > 0)
            throw new ArgumentException("ChartOfAccountId is required.", nameof(payment));

        var resolvedAllocations = await ResolveOwnerPaymentAllocationsAsync(payment, allocations);
        ValidateExplicitOwnerPaymentAllocations(payment, resolvedAllocations);
        payment.CostCodeId = await ResolveOwnerPaymentCostCodeIdAsync(payment.OrganizationId, payment.OfficeId);

        Payment? createdPayment = null;
        try
        {
            await EnsurePaymentCodeAsync(payment);
            createdPayment = await _accountingRepository.CreatePaymentWithOwnerAllocationsAsync(payment, resolvedAllocations, currentUser);
            await CreateJournalEntriesFromOwnerPaymentDocumentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);
        }
        catch
        {
            if (createdPayment != null)
                await TryDeleteIncompleteOwnerPaymentAsync(createdPayment.PaymentId, payment.OrganizationId, currentUser);

            throw;
        }

        return await _accountingRepository.GetPaymentByIdAsync(createdPayment!.PaymentId, payment.OrganizationId)
            ?? createdPayment;
    }

    public async Task<Payment> UpdatePaymentWithOwnerAllocationsAsync(Payment payment, IReadOnlyList<PaymentOwnerAllocation> allocations, Guid currentUser)
    {
        if (payment.PaymentId == Guid.Empty)
            throw new ArgumentException("PaymentId is required.", nameof(payment));

        EnsureOwnerPayment(payment);

        if (payment.ChartOfAccountId is not > 0)
            throw new ArgumentException("ChartOfAccountId is required.", nameof(payment));

        var existing = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, payment.OrganizationId)
            ?? throw new Exception("Payment record not found");

        if (existing.PaymentKindId != (int)PaymentKind.Owner)
            throw new Exception("Payment is not an owner payment.");

        var resolvedAllocations = await ResolveOwnerPaymentAllocationsAsync(payment, allocations);
        ValidateExplicitOwnerPaymentAllocations(payment, resolvedAllocations);

        payment.PaymentCode = existing.PaymentCode;
        payment.PostingStatusId = existing.PostingStatusId;
        payment.CostCodeId = await ResolveOwnerPaymentCostCodeIdAsync(payment.OrganizationId, payment.OfficeId);

        await ClearPaymentDocumentLinksAsync(existing.OrganizationId, existing.PaymentId, currentUser);
        await DeleteJournalEntriesForPaymentAsync(existing);

        var updatedPayment = await _accountingRepository.UpdatePaymentWithOwnerAllocationsAsync(payment, resolvedAllocations, currentUser);
        await CreateJournalEntriesFromOwnerPaymentDocumentAsync(updatedPayment.PaymentId, payment.OrganizationId, currentUser);

        return await _accountingRepository.GetPaymentByIdAsync(updatedPayment.PaymentId, payment.OrganizationId)
            ?? updatedPayment;
    }

    private async Task<List<PaymentOwnerAllocation>> ResolveOwnerPaymentAllocationsAsync(Payment payment, IReadOnlyList<PaymentOwnerAllocation> allocations)
    {
        var resolved = new List<PaymentOwnerAllocation>();
        var lineNumber = 1;
        foreach (var allocation in allocations)
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(allocation.PropertyId, payment.OrganizationId);
            if (property == null || property.OfficeId != payment.OfficeId)
                throw new Exception("Invalid property for owner payment");

            if (property.Owner1Id != allocation.OwnerId && property.Owner2Id != allocation.OwnerId)
                throw new Exception("Owner does not match property for owner payment");

            string? ownerName = NormalizeOptionalString(allocation.OwnerName);
            if (string.IsNullOrWhiteSpace(ownerName))
            {
                var contact = await _contactRepository.GetContactByIdsAsync(allocation.OwnerId, payment.OrganizationId);
                if (contact != null)
                    ownerName = NormalizeOptionalString(contact.DisplayName ?? contact.CompanyName ?? contact.FullName);
            }

            resolved.Add(new PaymentOwnerAllocation
            {
                OwnerId = allocation.OwnerId,
                OwnerName = ownerName ?? string.Empty,
                PropertyId = allocation.PropertyId,
                PropertyCode = NormalizeOptionalString(property.PropertyCode) ?? allocation.PropertyCode,
                LineNumber = lineNumber++,
                Amount = allocation.Amount,
                Description = string.IsNullOrWhiteSpace(allocation.Description)
                    ? payment.Description
                    : allocation.Description.Trim()
            });
        }

        return resolved;
    }

    private static void EnsureOwnerPayment(Payment payment)
    {
        if (payment.PaymentKindId != (int)PaymentKind.Owner)
            throw new ArgumentException("Owner allocations are only supported for owner payments.", nameof(payment));
    }

    private static void ValidateExplicitOwnerPaymentAllocations(Payment payment, IReadOnlyList<PaymentOwnerAllocation> allocations)
    {
        if (allocations == null || allocations.Count == 0)
            throw new ArgumentException("At least one owner allocation is required.", nameof(allocations));

        var allocationTotal = allocations.Sum(allocation => allocation.Amount);
        if (allocationTotal != payment.Amount)
            throw new ArgumentException("Allocation total must equal the payment amount.", nameof(allocations));
    }

    private async Task<int> ResolveOwnerPaymentCostCodeIdAsync(Guid organizationId, int officeId)
    {
        var costCodeById = await LoadCostCodeByOfficeIdAsync(organizationId, officeId);
        var firstActive = costCodeById.Values.FirstOrDefault(costCode => costCode.OfficeId == officeId && costCode.IsActive);
        if (firstActive?.CostCodeId is > 0)
            return firstActive.CostCodeId;

        throw new Exception("Unable to resolve a cost code for the owner payment. Add cost codes for this office.");
    }

    private async Task TryDeleteIncompleteOwnerPaymentAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        try
        {
            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (payment == null)
                return;

            await DeletePaymentAsync(paymentId, organizationId, currentUser);
        }
        catch
        {
            // Best-effort cleanup after a failed create.
        }
    }

    private static string FormatOwnerPaymentReference(PaymentType paymentType)
    {
        return paymentType switch
        {
            PaymentType.Check => "Check",
            PaymentType.Ach => "ACH",
            PaymentType.Eft => "EFT",
            PaymentType.OnlineBanking => "Online banking",
            PaymentType.WireTransfer => "Wire transfer",
            PaymentType.CreditCard => "Credit card",
            _ => paymentType.ToString()
        };
    }
    #endregion
}
