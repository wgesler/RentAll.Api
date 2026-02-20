using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.AccountingOffices;

public class AccountingOfficeResponseDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Fax { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankRouting { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string BankSwiftCode { get; set; } = string.Empty;
    public string BankAddress { get; set; } = string.Empty;
    public string BankPhone { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public AccountingOfficeResponseDto(AccountingOffice accountingOffice)
    {
        OrganizationId = accountingOffice.OrganizationId;
        OfficeId = accountingOffice.OfficeId;
        Name = accountingOffice.Name;
        Address1 = accountingOffice.Address1;
        Address2 = accountingOffice.Address2;
        Suite = accountingOffice.Suite;
        City = accountingOffice.City;
        State = accountingOffice.State;
        Zip = accountingOffice.Zip;
        Phone = accountingOffice.Phone;
        Fax = accountingOffice.Fax;
        Email = accountingOffice.Email;
        Website = accountingOffice.Website;
        BankName = accountingOffice.BankName;
        BankRouting = accountingOffice.BankRouting;
        BankAccount = accountingOffice.BankAccount;
        BankSwiftCode = accountingOffice.BankSwiftCode;
        BankAddress = accountingOffice.BankAddress;
        BankPhone = accountingOffice.BankPhone;
        LogoPath = accountingOffice.LogoPath;
        IsActive = accountingOffice.IsActive;
        CreatedOn = accountingOffice.CreatedOn;
        CreatedBy = accountingOffice.CreatedBy;
        ModifiedOn = accountingOffice.ModifiedOn;
        ModifiedBy = accountingOffice.ModifiedBy;
    }
}
