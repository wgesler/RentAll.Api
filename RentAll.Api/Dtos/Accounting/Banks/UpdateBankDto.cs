using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Accounting.Banks;

public class UpdateBankDto
{
    public string BankName { get; set; } = string.Empty;
    public string? BankPhone { get; set; }
    public string? BankRouting { get; set; }
    public string? BankAccount { get; set; }
    public string? BankSwiftCode { get; set; }
    public string? BankAddress { get; set; }
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

    public Bank ToModel(int bankId, Guid organizationId, int officeId, Guid currentUser)
    {
        return new Bank
        {
            BankId = bankId,
            OrganizationId = organizationId,
            OfficeId = officeId,
            BankName = BankName.Trim(),
            BankPhone = Normalize(BankPhone),
            BankRouting = Normalize(BankRouting),
            BankAccount = Normalize(BankAccount),
            BankSwiftCode = Normalize(BankSwiftCode),
            BankAddress = Normalize(BankAddress),
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
