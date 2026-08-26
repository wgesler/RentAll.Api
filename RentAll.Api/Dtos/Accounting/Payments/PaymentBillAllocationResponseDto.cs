namespace RentAll.Api.Dtos.Accounting.Payments;

using RentAll.Domain.Models;

public class PaymentBillAllocationResponseDto
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

    public PaymentBillAllocationResponseDto(PaymentBillAllocation allocation)
    {
        PaymentBillAllocationId = allocation.PaymentBillAllocationId;
        PaymentId = allocation.PaymentId;
        ReceiptId = allocation.ReceiptId;
        ReceiptCode = allocation.ReceiptCode;
        VendorName = allocation.VendorName;
        LineNumber = allocation.LineNumber;
        Amount = allocation.Amount;
        CostCodeId = allocation.CostCodeId;
        CostCodeDescription = allocation.CostCodeDescription;
        Description = allocation.Description;
    }
}
