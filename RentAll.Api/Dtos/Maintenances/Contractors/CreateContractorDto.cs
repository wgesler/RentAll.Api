using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Dtos.Maintenances.Contractors;

public class CreateContractorDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (Rating < 0)
            return (false, "Rating cannot be negative");

        return (true, null);
    }

    public Contractor ToModel(string code, Guid currentUser)
    {
        return new Contractor
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            ContractorCode = code,
            Name = Name,
            Phone = Phone,
            Website = Website,
            Rating = Rating,
            Notes = Notes,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}
