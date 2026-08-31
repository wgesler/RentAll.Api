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
            var chartOfAccountId = GetDefaultBankAccount(chartOfAccounts, payment.OfficeId, accountingOffice);
            var paymentReference = FormatOwnerPaymentReference(payment.PaymentType);
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
