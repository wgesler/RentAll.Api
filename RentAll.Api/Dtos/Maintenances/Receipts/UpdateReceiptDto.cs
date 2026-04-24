using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class UpdateReceiptDto
{
    public int ReceiptId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ReceiptSplit> Splits { get; set; } = new List<ReceiptSplit>();
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReceiptId <= 0)
            return (false, "ReceiptId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (Splits == null || Splits.Count == 0)
            return (false, "At least one split is required");

        return (true, null);
    }

    public Receipt ToModel(Guid currentUser)
    {
        return new Receipt
        {
            ReceiptId = ReceiptId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            Description = Description,
            Amount = Amount,
            Splits = Splits,
            ReceiptPath = ReceiptPath,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
