using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.Contractors;

public class UpdateContractorDto
{
    public Guid ContractorId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string ContractorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ContractorId == Guid.Empty)
            return (false, "ContractorId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (string.IsNullOrWhiteSpace(ContractorCode))
            return (false, "ContractorCode is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (Rating < 0)
            return (false, "Rating cannot be negative");

        return (true, null);
    }

    public Contractor ToModel(Guid currentUser)
    {
        return new Contractor
        {
            ContractorId = ContractorId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            ContractorCode = ContractorCode,
            Name = Name,
            Phone = Phone,
            Website = Website,
            Rating = Rating,
            Notes = Notes,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
