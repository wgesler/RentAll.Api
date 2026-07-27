using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Owners
    public async Task<List<JournalEntry>> ApplyPaymentToOwnersAsync(OwnerPayments ownerPayments, Guid organizationId, string offices, Guid currentUser)
    {
        if (ownerPayments.Payments.Count == 0)
            throw new Exception("No owner payments submitted");

        var accessOfficeIds = ParseOfficeIdsFromAccess(offices);
        var paymentApplications = new List<OwnerPaymentApplication>();
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
            var contact = await _contactRepository.GetContactByIdAsync(payment.OwnerId, organizationId);
            if (contact != null)
                ownerName = NormalizeOptionalString(contact.DisplayName ?? contact.CompanyName ?? contact.FullName);

            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, payment.OfficeId);
            var chartOfAccountId = GetDefaultBankAccount(chartOfAccounts, payment.OfficeId, accountingOffice);

            paymentApplications.Add(new OwnerPaymentApplication
            {
                OrganizationId = organizationId,
                OfficeId = payment.OfficeId,
                OwnerId = payment.OwnerId,
                PropertyId = payment.PropertyId,
                PropertyCode = NormalizeOptionalString(property.PropertyCode) ?? string.Empty,
                OwnerName = ownerName,
                AmountApplied = payment.Amount,
                PaymentDate = ownerPayments.PaymentDate,
                ChartOfAccountId = chartOfAccountId,
                Description = FormatOwnerPaymentReference(payment.PaymentType),
                PaymentType = payment.PaymentType
            });
        }

        var ownerPaymentBatch = new OwnerPaymentBatch
        {
            PaymentApplications = paymentApplications
        };

        return await CreateJournalEntriesFromOwnerPaymentAsync(ownerPaymentBatch, currentUser);
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
