using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class ReceiptResponseDto
{
    public int ReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public List<Guid> PropertyIds { get; set; } = new List<Guid>();
    public DateOnly ReceiptDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? BankCardId { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public List<ReceiptSplitDto> Splits { get; set; } = new List<ReceiptSplitDto>();
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }
    public string BankCardDisplayName { get; set; } = string.Empty;

    public ReceiptResponseDto(Receipt receipt)
    {
        ReceiptId = receipt.ReceiptId;
        OrganizationId = receipt.OrganizationId;
        OfficeId = receipt.OfficeId;
        OfficeName = receipt.OfficeName;
        PropertyIds = receipt.PropertyIds;
        ReceiptDate = receipt.ReceiptDate;
        Amount = receipt.Amount;
        Description = receipt.Description;
        BankCardId = receipt.BankCardId;
        VendorId = receipt.VendorId;
        VendorName = receipt.VendorName;
        Splits = (receipt.Splits ?? new List<ReceiptSplit>()).Select(split => new ReceiptSplitDto(split)).ToList();
        ReceiptPath = receipt.ReceiptPath;
        FileDetails = receipt.FileDetails;
        IsActive = receipt.IsActive;
        CreatedBy = receipt.CreatedByName;
        ModifiedOn = receipt.ModifiedOn;
        ModifiedBy = receipt.ModifiedByName;
        BankCardDisplayName = receipt.BankCardDisplayName;
    }
}
