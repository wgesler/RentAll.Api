using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.Payments;

public class UpdatePaymentBillDto
{
    public Guid PaymentId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? PaymentTypeId { get; set; }
    public int ChartOfAccountId { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PaymentId == Guid.Empty)
            return (false, "PaymentId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PaymentDate == default)
            return (false, "PaymentDate is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        return (true, null);
    }

    public Payment ToModel(Guid currentUser)
    {
        return new Payment
        {
            PaymentId = PaymentId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PaymentDate = PaymentDate,
            Amount = Amount,
            Description = Description,
            PaymentDirectionId = (int)PaymentDirection.Outbound,
            PaymentTypeId = PaymentTypeId is >= 0 ? PaymentTypeId : null,
            ChartOfAccountId = ChartOfAccountId,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
