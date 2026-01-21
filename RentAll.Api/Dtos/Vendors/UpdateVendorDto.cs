using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Vendors;

public class UpdateVendorDto
{
    public Guid OrganizationId { get; set; }
    public Guid VendorId { get; set; }
	public int OfficeId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
	public string? Address1 { get; set; }
	public string? Address2 { get; set; }
	public string? Suite { get; set; }
	public string? City { get; set; }
	public string? State { get; set; }
	public string? Zip { get; set; }
	public string? Phone { get; set; }
	public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
    {
        if (id == Guid.Empty)
            return (false, "Vendor ID is required");

        if (VendorId != id)
            return (false, "Vendor ID mismatch");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (string.IsNullOrWhiteSpace(VendorCode))
            return (false, "Vendor Code is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (string.IsNullOrWhiteSpace(Address1))
            return (false, "Address1 is required");

        if (string.IsNullOrWhiteSpace(City))
            return (false, "City is required");

        if (string.IsNullOrWhiteSpace(State))
            return (false, "State is required");

        if (string.IsNullOrWhiteSpace(Zip))
            return (false, "Zip is required");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        return (true, null);
    }

    public Vendor ToModel(Guid currentUser)
    {
        return new Vendor
        {
            OrganizationId = OrganizationId,
            VendorId = VendorId,
			OfficeId = OfficeId,
            VendorCode = VendorCode,
            Name = Name,
            Address1 = Address1,
            Address2 = Address2,
            Suite = Suite,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Website = Website,
            LogoPath = LogoPath, // Will be updated by controller if FileDetails provided
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}



