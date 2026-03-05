using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.Contractors;

public class ContractorResponseDto
{
    public Guid ContractorId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string ContractorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public ContractorResponseDto(Contractor contractor)
    {
        ContractorId = contractor.ContractorId;
        OrganizationId = contractor.OrganizationId;
        OfficeId = contractor.OfficeId;
        OfficeName = contractor.OfficeName;
        ContractorCode = contractor.ContractorCode;
        Name = contractor.Name;
        Phone = contractor.Phone;
        Website = contractor.Website;
        Rating = contractor.Rating;
        Notes = contractor.Notes;
        IsActive = contractor.IsActive;
        CreatedOn = contractor.CreatedOn;
        CreatedBy = contractor.CreatedBy;
        ModifiedOn = contractor.ModifiedOn;
        ModifiedBy = contractor.ModifiedBy;
    }
}
