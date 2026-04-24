using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class ReceiptResponseDto
{
    public int ReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public List<Guid> PropertyIds { get; set; } = new List<Guid>();
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ReceiptSplit> Splits { get; set; } = new List<ReceiptSplit>();
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }

    public ReceiptResponseDto(Receipt receipt)
    {
        ReceiptId = receipt.ReceiptId;
        OrganizationId = receipt.OrganizationId;
        OfficeId = receipt.OfficeId;
        OfficeName = receipt.OfficeName;
        PropertyIds = receipt.PropertyIds;
        Amount = receipt.Amount;
        Description = receipt.Description;
        Splits = receipt.Splits;
        ReceiptPath = receipt.ReceiptPath;
        FileDetails = receipt.FileDetails;
        IsActive = receipt.IsActive;
        ModifiedOn = receipt.ModifiedOn;
        ModifiedBy = receipt.ModifiedByName;
    }
}
