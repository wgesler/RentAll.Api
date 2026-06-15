using RentAll.Domain.Models.Common;
using RentAll.Api.Dtos.Accounting.BankCards;

namespace RentAll.Api.Dtos.Organizations.Accounting;

public class UpdateAccountingOfficeDto
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
    public int? DefaultTenantExpAccountId { get; set; }
    public int? DefaultOwnerExpAccountId { get; set; }
    public int? DefaultCompanyExpAccountId { get; set; }
    public string? LogoPath { get; set; }
    public FileDetails? FileDetails { get; set; }
    public bool IsActive { get; set; }
    public List<UpdateBankCardDto>? BankCards { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

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

        if (string.IsNullOrWhiteSpace(BankName))
            return (false, "BankName is required");

        if (string.IsNullOrWhiteSpace(BankRouting))
            return (false, "BankRouting is required");

        if (string.IsNullOrWhiteSpace(BankAccount))
            return (false, "BankAccount is required");

        if (string.IsNullOrWhiteSpace(BankSwiftCode))
            return (false, "BankSwiftCode is required");

        if (string.IsNullOrWhiteSpace(BankAddress))
            return (false, "BankAddress is required");

        if (string.IsNullOrWhiteSpace(BankPhone))
            return (false, "BankPhone is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        if (BankCards == null)
            return (true, null);

        foreach (var bankCard in BankCards)
        {
            var (bankCardValid, bankCardError) = bankCard.IsValid();
            if (!bankCardValid)
                return (false, bankCardError ?? "Invalid bank card data");
        }

        return (true, null);
    }

    public AccountingOffice ToModel(Guid currentUser)
    {
        return new AccountingOffice
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            Name = Name,
            Address1 = Address1,
            Address2 = Address2,
            Suite = Suite,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Fax = Fax,
            Email = Email,
            Website = Website,
            BankName = BankName,
            BankRouting = BankRouting,
            BankAccount = BankAccount,
            BankSwiftCode = BankSwiftCode,
            BankAddress = BankAddress,
            BankPhone = BankPhone,
            WorkOrderNo = WorkOrderNo,
            DefaultActRecvAccountId = DefaultActRecvAccountId,
            DefaultEscrowAccountId = DefaultEscrowAccountId,
            DefaultUndepFundsAccountId = DefaultUndepFundsAccountId,
            DefaultBankAccountId = DefaultBankAccountId,
            DefaultActPayableAccountId = DefaultActPayableAccountId,
            DefaultTenantExpAccountId = DefaultTenantExpAccountId,
            DefaultOwnerExpAccountId = DefaultOwnerExpAccountId,
            DefaultCompanyExpAccountId = DefaultCompanyExpAccountId,
            LogoPath = LogoPath, // Will be updated by controller if FileDetails provided
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
