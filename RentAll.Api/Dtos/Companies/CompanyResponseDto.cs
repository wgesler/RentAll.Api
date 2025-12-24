using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Companies;

public class CompanyResponseDto
{
    public Guid CompanyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
	public string? Notes { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }


	public CompanyResponseDto(Company company)
    {
        CompanyId = company.CompanyId;
        OrganizationId = company.OrganizationId;
        CompanyCode = company.CompanyCode;
        Name = company.Name;
        Address1 = company.Address1;
        Address2 = company.Address2;
        Suite = company.Suite;
        City = company.City;
        State = company.State;
        Zip = company.Zip;
        Phone = company.Phone;
        Website = company.Website;
        LogoPath = company.LogoPath;
		Notes = company.Notes;
		IsActive = company.IsActive;
		CreatedOn = company.CreatedOn;
		CreatedBy = company.CreatedBy;
		ModifiedOn = company.ModifiedOn;
		ModifiedBy = company.ModifiedBy;
	}
}
