using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class CreateReceiptDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public List<Guid> PropertyIds { get; set; } = new List<Guid>();
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<ReceiptSplitDto> Splits { get; set; } = new List<ReceiptSplitDto>();
    public string? ReceiptPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyIds == null || PropertyIds.Count == 0)
            return (false, "At least one PropertyId is required");

        if (PropertyIds.Any(id => id == Guid.Empty))
            return (false, "PropertyIds cannot contain empty Guid values");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (Splits == null || Splits.Count == 0)
            return (false, "At least one split is required");

        foreach (var split in Splits)
        {
            var (isValid, errorMessage) = split.IsValid();
            if (!isValid)
                return (false, $"Split validation failed: {errorMessage}");
        }

        return (true, null);
    }

    public Receipt ToModel(Guid currentUser)
    {
        return new Receipt
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyIds = PropertyIds,
            Amount = Amount,
            Description = Description,
            Splits = Splits.Select(split => split.ToModel()).ToList(),
            ReceiptPath = null, // Will be set by controller after file save
            IsActive = true,
            CreatedBy = currentUser
        };
    }
}
