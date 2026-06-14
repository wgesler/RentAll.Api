using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Models;

public class Receipt
{
    public int ReceiptId { get; set; }
    public Guid ReceiptGuid { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public List<Guid> PropertyIds { get; set; } = new();
    public DateOnly ReceiptDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public string? BillNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? PaidDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? BankCardId { get; set; }
    public string BankCardDisplayName { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public List<ReceiptSplit> Splits { get; set; } = new();
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public int PaymentTypeId { get; set; }
    public bool CheckPaid { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
}
