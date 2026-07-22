namespace RentAll.Domain.Models;

public class Payment
{
    public Guid PaymentId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public int CostCodeId { get; set; }
    public string CostCodeDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? PaymentTypeId { get; set; }
    public string PaymentTypeDescription { get; set; } = string.Empty;
    public Guid? DepositId { get; set; }
    public string DepositCode { get; set; } = string.Empty;
    public int? PostingStatusId { get; set; }
    public bool IsActive { get; set; }
    public List<PaymentLedgerLine> LedgerLines { get; set; } = new();
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
}
