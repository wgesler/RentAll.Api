namespace RentAll.Domain.Models;

public class PaymentOwnerAllocation
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
}
