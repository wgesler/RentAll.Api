namespace RentAll.Api.Dtos.Accounting.Payments;

public class PaymentResponseDto
{
    public Guid PaymentId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string PaymentCode { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public int CostCodeId { get; set; }
    public string CostCodeDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PaymentKindId { get; set; }
    public int? PaymentTypeId { get; set; }
    public string PaymentTypeDescription { get; set; } = string.Empty;
    public int? ChartOfAccountId { get; set; }
    public Guid? DepositId { get; set; }
    public string DepositCode { get; set; } = string.Empty;
    public int? PostingStatusId { get; set; }
    public bool IsActive { get; set; }
    public List<PaymentInvoiceAllocationResponseDto> InvoiceAllocations { get; set; } = new();
    public List<PaymentBillAllocationResponseDto> BillAllocations { get; set; } = new();
    public List<PaymentOwnerAllocationResponseDto> OwnerAllocations { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;

    public PaymentResponseDto(Payment payment)
    {
        PaymentId = payment.PaymentId;
        OrganizationId = payment.OrganizationId;
        OfficeId = payment.OfficeId;
        PaymentCode = payment.PaymentCode;
        OfficeName = payment.OfficeName;
        PaymentDate = payment.PaymentDate;
        Amount = payment.Amount;
        CostCodeId = payment.CostCodeId;
        CostCodeDescription = payment.CostCodeDescription;
        Description = payment.Description;
        PaymentKindId = payment.PaymentKindId;
        PaymentTypeId = payment.PaymentTypeId;
        PaymentTypeDescription = payment.PaymentTypeDescription;
        ChartOfAccountId = payment.ChartOfAccountId;
        DepositId = payment.DepositId is { } depositId && depositId != Guid.Empty ? depositId : null;
        DepositCode = payment.DepositCode;
        PostingStatusId = payment.PostingStatusId;
        IsActive = payment.IsActive;
        InvoiceAllocations = (payment.LedgerLines ?? new List<PaymentLedgerLine>())
            .Select(line => new PaymentInvoiceAllocationResponseDto(line))
            .ToList();
        BillAllocations = (payment.BillAllocations ?? new List<PaymentBillAllocation>())
            .Select(allocation => new PaymentBillAllocationResponseDto(allocation))
            .ToList();
        OwnerAllocations = (payment.OwnerAllocations ?? new List<PaymentOwnerAllocation>())
            .Select(allocation => new PaymentOwnerAllocationResponseDto(allocation))
            .ToList();
        CreatedOn = payment.CreatedOn;
        CreatedBy = payment.CreatedByName;
        ModifiedOn = payment.ModifiedOn;
        ModifiedBy = payment.ModifiedByName;
    }
}
