using RentAll.Domain.Models.Common;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class CreateReceiptDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? ReceiptPath { get; set; }
    public string? WorkOrderCode { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        return (true, null);
    }

    public Receipt ToModel(Guid currentUser)
    {
        return new Receipt
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            Description = Description,
            Amount = Amount,
            ReceiptPath = null, // Will be set by controller after file save
            WorkOrderCode = WorkOrderCode,
            IsActive = true,
            CreatedBy = currentUser
        };
    }
}
