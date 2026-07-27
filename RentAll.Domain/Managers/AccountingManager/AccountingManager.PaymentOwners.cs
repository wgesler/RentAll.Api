using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Owners
    public async Task<OwnerPayment> ApplyPaymentToOwnersAsync(IReadOnlyList<OwnerPaymentLine> lines, Guid organizationId, string offices, int chartOfAccountId, string description, DateOnly paymentDate, PaymentType paymentType, Guid currentUser)
    {
        if (lines.Count == 0)
            throw new Exception("No owner statement lines submitted for payment");

        var accessOfficeIds = ParseOfficeIdsFromAccess(offices);
        var paymentApplications = new List<OwnerPaymentApplication>();
        foreach (var line in lines)
        {
            if (accessOfficeIds.Count > 0 && !accessOfficeIds.Contains(line.OfficeId))
                throw new Exception($"Office access denied for office {line.OfficeId}");

            var property = await _propertyRepository.GetPropertyByIdAsync(line.PropertyId, organizationId);
            if (property == null || property.OfficeId != line.OfficeId)
                throw new Exception("Invalid property for owner payment");

            if (property.Owner1Id != line.OwnerId && property.Owner2Id != line.OwnerId)
                throw new Exception("Owner does not match property for owner payment");

            string? ownerName = null;
            var contact = await _contactRepository.GetContactByIdAsync(line.OwnerId, organizationId);
            if (contact != null)
                ownerName = NormalizeOptionalString(contact.DisplayName ?? contact.CompanyName ?? contact.FullName);

            paymentApplications.Add(new OwnerPaymentApplication
            {
                OrganizationId = organizationId,
                OfficeId = line.OfficeId,
                OwnerId = line.OwnerId,
                PropertyId = line.PropertyId,
                PropertyCode = NormalizeOptionalString(property.PropertyCode) ?? string.Empty,
                OwnerName = ownerName,
                AmountApplied = line.Amount,
                PaymentDate = paymentDate,
                ChartOfAccountId = chartOfAccountId,
                Description = description.Trim(),
                PaymentType = paymentType
            });
        }

        return new OwnerPayment
        {
            PaymentApplications = paymentApplications
        };
    }
    #endregion
}
