using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Vendors;

public class VendorResponseDto
{
    public Guid VendorId { get; set; }
    public Guid OrganizationId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
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
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public VendorResponseDto(Vendor vendor)
    {
        VendorId = vendor.VendorId;
        OrganizationId = vendor.OrganizationId;
        VendorCode = vendor.VendorCode;
        Name = vendor.Name;
        Address1 = vendor.Address1;
        Address2 = vendor.Address2;
        Suite = vendor.Suite;
        City = vendor.City;
        State = vendor.State;
        Zip = vendor.Zip;
        Phone = vendor.Phone;
        Website = vendor.Website;
        LogoPath = vendor.LogoPath;
        IsActive = vendor.IsActive;
        CreatedOn = vendor.CreatedOn;
        CreatedBy = vendor.CreatedBy;
        ModifiedOn = vendor.ModifiedOn;
        ModifiedBy = vendor.ModifiedBy;
    }
}



