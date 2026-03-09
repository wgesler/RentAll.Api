using RentAll.Domain.Models.Common;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class ReceiptResponseDto
{
    public int ReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
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
        PropertyId = receipt.PropertyId;
        PropertyCode = receipt.PropertyCode;
        Description = receipt.Description;
        Amount = receipt.Amount;
        ReceiptPath = receipt.ReceiptPath;
        FileDetails = receipt.FileDetails;
        IsActive = receipt.IsActive;
        ModifiedOn = receipt.ModifiedOn;
        ModifiedBy = receipt.ModifiedByName;
    }
}
