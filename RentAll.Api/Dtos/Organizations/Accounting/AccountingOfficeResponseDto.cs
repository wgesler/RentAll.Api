using RentAll.Api.Dtos.Accounting.BankCards;
using RentAll.Domain.Models.Common;

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
    public int? DefaultTenantIncAccountId { get; set; }
    public int? DefaultTenantExpAccountId { get; set; }
    public int? DefaultOwnerIncAccountId { get; set; }
    public int? DefaultOwnerExpAccountId { get; set; }
    public int? DefaultCompanyExpAccountId { get; set; }
    public int? DefaultPmUtilityIncAccountId { get; set; }
    public int? DefaultLaborIncAccountId { get; set; }
    public int? DefaultLinenTowelIncAccountId { get; set; }
    public int? DefaultDepartureIncAccountId { get; set; }
    public int? DefaultDepartureExpAccountId { get; set; }
    public int? DefaultBankAccountId { get; set; }
    public int? DefaultActRcvableAccountId { get; set; }
    public int? DefaultActPayableAccountId { get; set; }
    public int? DefaultUndepFundsAccountId { get; set; }
    public int? DefaultEscrowDepositAccountId { get; set; }
    public int? DefaultEscrowOwnersAccountId { get; set; }
    public int? DefaultEscrowSecDepAccountId { get; set; }
    public int? DefaultEscrowSdwAccountId { get; set; }
    public int? DefaultOwnActPayableAccountId { get; set; }
    public int? DefaultPrePayAccountId { get; set; }
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public string? CheckStockPath { get; set; }
    public FileDetails? CheckStockFileDetails { get; set; }
    public int CurrentCheckNumber { get; set; }
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
        DefaultTenantIncAccountId = accountingOffice.DefaultTenantIncAccountId;
        DefaultTenantExpAccountId = accountingOffice.DefaultTenantExpAccountId;
        DefaultOwnerIncAccountId = accountingOffice.DefaultOwnerIncAccountId;
        DefaultOwnerExpAccountId = accountingOffice.DefaultOwnerExpAccountId;
        DefaultCompanyExpAccountId = accountingOffice.DefaultCompanyExpAccountId;
        DefaultPmUtilityIncAccountId = accountingOffice.DefaultPmUtilityIncAccountId;
        DefaultLaborIncAccountId = accountingOffice.DefaultLaborIncAccountId;
        DefaultLinenTowelIncAccountId = accountingOffice.DefaultLinenTowelIncAccountId;
        DefaultDepartureIncAccountId = accountingOffice.DefaultDepartureIncAccountId;
        DefaultDepartureExpAccountId = accountingOffice.DefaultDepartureExpAccountId;
        DefaultBankAccountId = accountingOffice.DefaultBankAccountId;
        DefaultActRcvableAccountId = accountingOffice.DefaultActRcvableAccountId;
        DefaultActPayableAccountId = accountingOffice.DefaultActPayableAccountId;
        DefaultUndepFundsAccountId = accountingOffice.DefaultUndepFundsAccountId;
        DefaultEscrowDepositAccountId = accountingOffice.DefaultEscrowDepositAccountId;
        DefaultEscrowOwnersAccountId = accountingOffice.DefaultEscrowOwnersAccountId;
        DefaultEscrowSecDepAccountId = accountingOffice.DefaultEscrowSecDepAccountId;
        DefaultEscrowSdwAccountId = accountingOffice.DefaultEscrowSdwAccountId;
        DefaultOwnActPayableAccountId = accountingOffice.DefaultOwnActPayableAccountId;
        DefaultPrePayAccountId = accountingOffice.DefaultPrePayAccountId;
        LogoPath = accountingOffice.LogoPath;
        CheckStockPath = accountingOffice.CheckStockPath;
        CurrentCheckNumber = accountingOffice.CurrentCheckNumber;
        IsActive = accountingOffice.IsActive;
        CreatedOn = accountingOffice.CreatedOn;
        CreatedBy = accountingOffice.CreatedBy;
        ModifiedOn = accountingOffice.ModifiedOn;
        ModifiedBy = accountingOffice.ModifiedBy;
        BankCards = accountingOffice.BankCards.Select(b => new BankCardResponseDto(b)).ToList();
    }
}
