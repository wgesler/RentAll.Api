using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Accounting.BankAccounts;

public class UpdateBankAccountDto
{
    public string BankName { get; set; } = string.Empty;
    public string? BankPhone { get; set; }
    public string? BankRouting { get; set; }
    public string? BankSwiftCode { get; set; }
    public string? BankAddress { get; set; }
    public string? AccountNo { get; set; }
    public string? AccountNoLastFour { get; set; }
    public int? AccountChartOfAccountId { get; set; }
    public int CurrentCheckNumber { get; set; } = 1;
    public string? CheckStockPath { get; set; }
    public FileDetails? CheckStockFileDetails { get; set; }
    public string? CheckHtml { get; set; }
    public bool UseOnInvoices { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(BankName))
            return (false, "BankName is required");

        if (CurrentCheckNumber < 1)
            return (false, "CurrentCheckNumber must be at least 1");

        return (true, null);
    }

    public BankAccount ToModel(int bankId, Guid organizationId, int officeId, Guid currentUser)
    {
        return new BankAccount
        {
            BankAccountId = bankId,
            OrganizationId = organizationId,
            OfficeId = officeId,
            BankName = BankName.Trim(),
            BankPhone = Normalize(BankPhone),
            BankRouting = Normalize(BankRouting),
            BankSwiftCode = Normalize(BankSwiftCode),
            BankAddress = Normalize(BankAddress),
            AccountNo = Normalize(AccountNo),
            AccountNoLastFour = Normalize(AccountNoLastFour),
            AccountChartOfAccountId = AccountChartOfAccountId,
            CurrentCheckNumber = CurrentCheckNumber,
            CheckStockPath = Normalize(CheckStockPath),
            CheckHtml = Normalize(CheckHtml),
            UseOnInvoices = UseOnInvoices,
            ModifiedBy = currentUser
        };
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
