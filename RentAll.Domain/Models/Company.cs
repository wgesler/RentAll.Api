using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Models;

public class Company
{
    public Guid CompanyId { get; set; }
    public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string OfficeName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
	public string? City { get; set; }
	public string? State { get; set; }
	public string? Zip { get; set; }
	public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
	public string? Notes { get; set; }
	public bool IsInternational { get; set; }
	public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}



