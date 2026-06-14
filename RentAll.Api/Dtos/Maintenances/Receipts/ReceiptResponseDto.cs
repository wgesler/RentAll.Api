using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class ReceiptResponseDto
{
    public int ReceiptId { get; set; }
    public Guid ReceiptGuid { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public List<Guid> PropertyIds { get; set; } = new List<Guid>();
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
    public List<ReceiptSplitDto> Splits { get; set; } = new List<ReceiptSplitDto>();
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public int PaymentTypeId { get; set; }
    public bool CheckPrinted { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }

    public ReceiptResponseDto(Receipt receipt)
    {
        ReceiptId = receipt.ReceiptId;
        ReceiptGuid = receipt.ReceiptGuid;
        ReceiptCode = receipt.ReceiptCode;
        OrganizationId = receipt.OrganizationId;
        OfficeId = receipt.OfficeId;
        OfficeName = receipt.OfficeName;
        PropertyIds = receipt.PropertyIds;
        ReceiptDate = receipt.ReceiptDate;
        DueDate = receipt.DueDate;
        AccountingPeriod = receipt.AccountingPeriod;
        BillNumber = receipt.BillNumber;
        Amount = receipt.Amount;
        PaidAmount = receipt.PaidAmount;
        PaidDate = receipt.PaidDate;
        Description = receipt.Description;
        BankCardId = receipt.BankCardId;
        BankCardDisplayName = receipt.BankCardDisplayName;
        VendorId = receipt.VendorId;
        VendorName = receipt.VendorName;
        Splits = (receipt.Splits ?? new List<ReceiptSplit>()).Select(split => new ReceiptSplitDto(split)).ToList();
        ReceiptPath = receipt.ReceiptPath;
        FileDetails = receipt.FileDetails;
        PaymentTypeId = receipt.PaymentTypeId;
        CheckPrinted = receipt.CheckPrinted;
        IsActive = receipt.IsActive;
        CreatedOn = receipt.CreatedOn;
        CreatedBy = receipt.CreatedByName;
        ModifiedOn = receipt.ModifiedOn;
        ModifiedBy = receipt.ModifiedByName;
    }
}
