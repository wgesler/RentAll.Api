namespace RentAll.Api.Dtos.Accounting.Payments;

using RentAll.Domain.Models;

public class PaymentOwnerAllocationResponseDto
{
    public Guid PaymentOwnerAllocationId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public PaymentOwnerAllocationResponseDto(PaymentOwnerAllocation allocation)
    {
        PaymentOwnerAllocationId = allocation.PaymentOwnerAllocationId;
        PaymentId = allocation.PaymentId;
        OwnerId = allocation.OwnerId;
        OwnerName = allocation.OwnerName;
        PropertyId = allocation.PropertyId;
        PropertyCode = allocation.PropertyCode;
        LineNumber = allocation.LineNumber;
        Amount = allocation.Amount;
        Description = allocation.Description;
    }
}
