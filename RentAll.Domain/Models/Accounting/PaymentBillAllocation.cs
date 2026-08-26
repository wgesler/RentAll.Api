namespace RentAll.Domain.Models;

public class PaymentBillAllocation
{
    public Guid PaymentBillAllocationId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ReceiptId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public decimal Amount { get; set; }
    public int? CostCodeId { get; set; }
    public string CostCodeDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
