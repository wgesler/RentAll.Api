using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Companies.Vendors;

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
    public bool IsInternational { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (VendorId == Guid.Empty)
            return (false, "Vendor ID is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (string.IsNullOrWhiteSpace(VendorCode))
            return (false, "Vendor Code is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

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
            IsInternational = IsInternational,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}



