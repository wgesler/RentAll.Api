using RentAll.Domain.Models.Companies;

namespace RentAll.Api.Dtos.Companies;

public class CompanyResponseDto
{
	public Guid CompanyId { get; set; }
	public string CompanyCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Address1 { get; set; } = string.Empty;
	public string? Address2 { get; set; }
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string? Website { get; set; }
	public int IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }

	public CompanyResponseDto(Company company)
	{
		CompanyId = company.CompanyId;
		CompanyCode = company.CompanyCode;
		Name = company.Name;
		Address1 = company.Address1;
		Address2 = company.Address2;
		City = company.City;
		State = company.State;
		Zip = company.Zip;
		Phone = company.Phone;
		Website = company.Website;
		IsActive = company.IsActive;
		CreatedOn = company.CreatedOn;
		CreatedBy = company.CreatedBy;
		ModifiedOn = company.ModifiedOn;
		ModifiedBy = company.ModifiedBy;
	}
}

