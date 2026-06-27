using RentAll.Domain.Models.Common;
using RentAll.Api.Dtos.Accounting.BankCards;

namespace RentAll.Api.Dtos.Organizations.Accounting;

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
    public int WorkOrderNo { get; set; }
    public int? DefaultActRecvAccountId { get; set; }
    public int? DefaultEscrowAccountId { get; set; }
    public int? DefaultUndepFundsAccountId { get; set; }
    public int? DefaultBankAccountId { get; set; }
    public int? DefaultActPayableAccountId { get; set; }
    public int? DefaultOwnActPayableAccountId { get; set; }
    public int? DefaultPrePayAccountId { get; set; }
    public int? DefaultTenantExpAccountId { get; set; }
    public int? DefaultTenantIncAccountId { get; set; }
    public int? DefaultPmUtilityIncAccountId { get; set; }
    public int? DefaultOwnerExpAccountId { get; set; }
    public int? DefaultOwnerIncAccountId { get; set; }
    public int? DefaultCompanyExpAccountId { get; set; }
    public int? DefaultDepartureIncAccountId { get; set; }
    public int? DefaultDepartureExpAccountId { get; set; }
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public List<BankCardResponseDto> BankCards { get; set; } = new();

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
        WorkOrderNo = accountingOffice.WorkOrderNo;
        DefaultActRecvAccountId = accountingOffice.DefaultActRecvAccountId;
        DefaultEscrowAccountId = accountingOffice.DefaultEscrowAccountId;
        DefaultUndepFundsAccountId = accountingOffice.DefaultUndepFundsAccountId;
        DefaultBankAccountId = accountingOffice.DefaultBankAccountId;
        DefaultActPayableAccountId = accountingOffice.DefaultActPayableAccountId;
        DefaultOwnActPayableAccountId = accountingOffice.DefaultOwnActPayableAccountId;
        DefaultPrePayAccountId = accountingOffice.DefaultPrePayAccountId;
        DefaultTenantExpAccountId = accountingOffice.DefaultTenantExpAccountId;
        DefaultTenantIncAccountId = accountingOffice.DefaultTenantIncAccountId;
        DefaultPmUtilityIncAccountId = accountingOffice.DefaultPmUtilityIncAccountId;
        DefaultOwnerExpAccountId = accountingOffice.DefaultOwnerExpAccountId;
        DefaultOwnerIncAccountId = accountingOffice.DefaultOwnerIncAccountId;
        DefaultCompanyExpAccountId = accountingOffice.DefaultCompanyExpAccountId;
        DefaultDepartureIncAccountId = accountingOffice.DefaultDepartureIncAccountId;
        DefaultDepartureExpAccountId = accountingOffice.DefaultDepartureExpAccountId;
        LogoPath = accountingOffice.LogoPath;
        IsActive = accountingOffice.IsActive;
        CreatedOn = accountingOffice.CreatedOn;
        CreatedBy = accountingOffice.CreatedBy;
        ModifiedOn = accountingOffice.ModifiedOn;
        ModifiedBy = accountingOffice.ModifiedBy;
        BankCards = accountingOffice.BankCards.Select(b => new BankCardResponseDto(b)).ToList();
    }
}
