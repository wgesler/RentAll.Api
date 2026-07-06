namespace RentAll.Infrastructure.Entities.Organizations;

public class AccountingOfficeEntity
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
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}
