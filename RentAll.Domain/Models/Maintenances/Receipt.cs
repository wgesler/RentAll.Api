using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Models;

public class Receipt
{
    public int ReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ReceiptSplit> Splits { get; set; } = new();
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
}
