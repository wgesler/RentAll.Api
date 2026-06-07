using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.Organizations;

public class OrganizationResponseDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Fax { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsInternational { get; set; }
    public int CurrentInvoiceNo { get; set; }
    public decimal OfficeFee { get; set; }
    public decimal UserFee { get; set; }
    public decimal Unit50Fee { get; set; }
    public decimal Unit100Fee { get; set; }
    public decimal Unit200Fee { get; set; }
    public decimal Unit500Fee { get; set; }
    public string? SendGridName { get; set; }
    public bool IsActive { get; set; }


    public OrganizationResponseDto(Organization org)
    {
        OrganizationId = org.OrganizationId;
        OrganizationCode = org.OrganizationCode;
        Name = org.Name;
        Address1 = org.Address1;
        Address2 = org.Address2;
        Suite = org.Suite;
        City = org.City;
        State = org.State;
        Zip = org.Zip;
        Phone = org.Phone;
        Fax = org.Fax;
        ContactName = org.ContactName;
        ContactEmail = org.ContactEmail;
        Website = org.Website;
        LogoPath = org.LogoPath;
        FileDetails = org.FileDetails;
        IsInternational = org.IsInternational;
        CurrentInvoiceNo = org.CurrentInvoiceNo;
        OfficeFee = org.OfficeFee;
        UserFee = org.UserFee;
        Unit50Fee = org.Unit50Fee;
        Unit100Fee = org.Unit100Fee;
        Unit200Fee = org.Unit200Fee;
        Unit500Fee = org.Unit500Fee;
        SendGridName = org.SendGridName;
        IsActive = org.IsActive;
    }
}




