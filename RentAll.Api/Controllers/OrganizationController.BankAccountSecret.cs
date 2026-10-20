namespace RentAll.Api.Controllers;

public partial class OrganizationController
{
    [HttpGet("accounting-office/{officeId:int}/bank-account")]
    public async Task<IActionResult> GetAccountingOfficeBankAccountAsync(int officeId)
    {
        if (officeId <= 0)
            return BadRequest("Office ID is required");

        try
        {
            var office = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
            if (office == null)
                return NotFound("Accounting office not found");

            if (IsClearBankAccountNumber(office.AccountNo))
                return Ok(new BankAccountRevealResponseDto { AccountNo = DigitsOnly(office.AccountNo!) });

            var encrypted = await _organizationRepository.GetEncryptedAccountingOfficeBankAccountAsync(CurrentOrganizationId, officeId);
            if (encrypted == null || encrypted.Length == 0)
                return Ok(new BankAccountRevealResponseDto { AccountNo = string.Empty });

            var accountNumber = await _encryptionService.DecryptAsync(encrypted);
            return Ok(new BankAccountRevealResponseDto { AccountNo = accountNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting accounting office bank account for office {OfficeId}", officeId);
            return ServerError("An error occurred while retrieving the bank account");
        }
    }

    [HttpGet("accounting-office/{officeId:int}/bank/{bankId:int}/account-number")]
    public async Task<IActionResult> GetBankAccountNumberAsync(int officeId, int bankId)
    {
        if (officeId <= 0)
            return BadRequest("Office ID is required");
        if (bankId <= 0)
            return BadRequest("Bank ID is required");

        try
        {
            var bank = await _accountingRepository.GetBankAccountByIdAsync(bankId, CurrentOrganizationId, officeId);
            if (bank == null)
                return NotFound("Bank not found");

            if (IsClearBankAccountNumber(bank.AccountNo))
                return Ok(new BankAccountRevealResponseDto { AccountNo = DigitsOnly(bank.AccountNo!) });

            var encrypted = await _accountingRepository.GetEncryptedBankAccountAsync(bankId, CurrentOrganizationId, officeId);
            if (encrypted == null || encrypted.Length == 0)
                return Ok(new BankAccountRevealResponseDto { AccountNo = string.Empty });

            var accountNumber = await _encryptionService.DecryptAsync(encrypted);
            return Ok(new BankAccountRevealResponseDto { AccountNo = accountNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting bank account {BankId} for office {OfficeId}", bankId, officeId);
            return ServerError("An error occurred while retrieving the bank account");
        }
    }

    private async Task ProtectStoredBankAccountsAsync(AccountingOffice office)
    {
        if (IsClearBankAccountNumber(office.AccountNo))
            await ProtectAccountingOfficeBankAccountAsync(office, office.AccountNo!);

        foreach (var bank in office.Banks)
        {
            if (IsClearBankAccountNumber(bank.AccountNo))
                await ProtectBankAccountNumberAsync(bank, bank.AccountNo!);
        }
    }

    private async Task ProtectAccountingOfficeBankAccountAsync(AccountingOffice office, string clearValue)
    {
        var digits = DigitsOnly(clearValue);
        var lastFour = digits.Length <= 4 ? digits : digits[^4..];
        var encrypted = await _encryptionService.EncryptAsync(digits);
        await _organizationRepository.SaveEncryptedAccountingOfficeBankAccountAsync(office.OrganizationId, office.OfficeId, encrypted, lastFour, CurrentUser);
        office.AccountNo = null;
        office.AccountNoLastFour = lastFour;
    }

    private async Task ProtectBankAccountNumberAsync(BankAccount bank, string clearValue)
    {
        var digits = DigitsOnly(clearValue);
        var lastFour = digits.Length <= 4 ? digits : digits[^4..];
        var encrypted = await _encryptionService.EncryptAsync(digits);
        await _accountingRepository.SaveEncryptedBankAccountAsync(bank.BankAccountId, bank.OrganizationId, bank.OfficeId, encrypted, lastFour, CurrentUser);
        bank.AccountNo = null;
        bank.AccountNoLastFour = lastFour;
    }

    private static void HideBankAccountNumbers(AccountingOfficeResponseDto response)
    {
        response.AccountNo = null;
        foreach (var bank in response.Banks)
            bank.AccountNo = null;
    }

    private static string? ClearBankAccountOrNull(string? value)
        => IsClearBankAccountNumber(value) ? DigitsOnly(value!) : null;

    private static bool IsClearBankAccountNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('*', StringComparison.Ordinal))
            return false;

        var compact = value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        return compact.Length >= 4 && compact.All(char.IsDigit);
    }

    private static string DigitsOnly(string value)
        => new string(value.Where(char.IsDigit).ToArray());
}

public sealed class BankAccountRevealResponseDto
{
    public string AccountNo { get; set; } = string.Empty;
}
