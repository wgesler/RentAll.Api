namespace RentAll.Api.Dtos.Accounting.Payments;

using RentAll.Domain.Models;

public class PaymentOwnerAllocationDto
{
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OwnerId == Guid.Empty)
            return (false, "OwnerId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (Amount == 0)
            return (false, "Amount is required");

        return (true, null);
    }

    public PaymentOwnerAllocation ToModel()
    {
        return new PaymentOwnerAllocation
        {
            OwnerId = OwnerId,
            PropertyId = PropertyId,
            Amount = Amount,
            Description = Description?.Trim() ?? string.Empty
        };
    }
}
