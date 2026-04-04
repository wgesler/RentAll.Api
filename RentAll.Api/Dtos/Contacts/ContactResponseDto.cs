using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Contacts;

public class ContactResponseDto
{
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string ContactCode { get; set; } = string.Empty;
    public int EntityTypeId { get; set; }
    public int? OwnerTypeId { get; set; }
    public List<string> Properties { get; set; } = new List<string>();
    public string? CompanyName { get; set; }
    public string? CompanyEmail { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public bool IsInternational { get; set; }
    public string? W9Path { get; set; }
    public FileDetails? W9FileDetails { get; set; }
    public string? InsurancePath { get; set; }
    public DateTimeOffset? InsuranceExpiration { get; set; }
    public FileDetails? InsuranceFileDetails { get; set; }
    public string? AgreementPath { get; set; }
    public FileDetails? AgreementFileDetails { get; set; }
    public int? Markup { get; set; }
    public decimal? RevenueSplitOwner { get; set; }
    public decimal? RevenueSplitOffice { get; set; }
    public decimal? WorkingCapitalBalance { get; set; }
    public decimal? LinenAndTowelFee { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }


    public ContactResponseDto(Contact contact)
    {
        ContactId = contact.ContactId;
        OrganizationId = contact.OrganizationId;
        OfficeId = contact.OfficeId;
        OfficeName = contact.OfficeName;
        ContactCode = contact.ContactCode;
        EntityTypeId = (int)contact.EntityType;
        OwnerTypeId = (int?)contact.OwnerType;
        CompanyName = contact.CompanyName;
        CompanyEmail = contact.CompanyEmail;
        Properties = contact.Properties;
        DisplayName = contact.DisplayName;
        FirstName = contact.FirstName;
        LastName = contact.LastName;
        FullName = contact.FullName;
        Address1 = contact.Address1;
        Address2 = contact.Address2;
        City = contact.City;
        State = contact.State;
        Zip = contact.Zip;
        Phone = contact.Phone;
        Email = contact.Email;
        Rating = contact.Rating;
        Notes = contact.Notes;
        IsInternational = contact.IsInternational;
        W9Path = contact.W9Path;
        W9FileDetails = contact.W9FileDetails;
        InsurancePath = contact.InsurancePath;
        InsuranceExpiration = contact.InsuranceExpiration;
        InsuranceFileDetails = contact.InsuranceFileDetails;
        AgreementPath = contact.AgreementPath;
        Markup = contact.Markup;
        RevenueSplitOwner = contact.RevenueSplitOwner;
        RevenueSplitOffice = contact.RevenueSplitOffice;
        WorkingCapitalBalance = contact.WorkingCapitalBalance;
        LinenAndTowelFee = contact.LinenAndTowelFee;
        BankName = contact.BankName;
        RoutingNumber = contact.RoutingNumber;
        AccountNumber = contact.AccountNumber;
        IsActive = contact.IsActive;
        CreatedOn = contact.CreatedOn;
        CreatedBy = contact.CreatedBy;
        ModifiedOn = contact.ModifiedOn;
        ModifiedBy = contact.ModifiedBy;
    }
}
