using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.Owners;

public class OwnerPaymentRequestDto
{
    public int OfficeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public int PaymentTypeId { get; set; }
    public int ChartOfAccountId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (OwnerId == Guid.Empty)
            return (false, "OwnerId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (!Enum.IsDefined(typeof(PaymentType), PaymentTypeId))
            return (false, $"Invalid PaymentType value: {PaymentTypeId}");

        if (Amount == 0)
            return (false, "Amount is required");

        return (true, null);
    }

    public OwnerPayment ToModel()
    {
        return new OwnerPayment
        {
            OfficeId = OfficeId,
            OwnerId = OwnerId,
            PropertyId = PropertyId,
            PaymentType = (PaymentType)PaymentTypeId,
            ChartOfAccountId = ChartOfAccountId,
            Description = Description?.Trim() ?? string.Empty,
            Amount = Amount
        };
    }
}
