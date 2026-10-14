namespace RentAll.Api.Controllers;

public partial class ContactController
{
    [HttpPost("account-numbers/encrypt-stored")]
    public async Task<IActionResult> EncryptStoredAccountNumbersAsync([FromBody] EncryptStoredAccountNumbersRequestDto? dto)
    {
        if (dto == null || dto.OrganizationId == Guid.Empty)
            return BadRequest("Organization ID is required");

        if (!CanViewStoredAccountNumber())
            return Forbid();

        if (!IsSuperAdmin() && dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("You are not authorized to encrypt account numbers for this organization.");

        try
        {
            var rows = await _contactRepository.GetClearAccountNumbersAsync(dto.OrganizationId);
            var converted = 0;
            foreach (var row in rows)
            {
                if (!IsClearBankAccountNumber(row.AccountNumber))
                    continue;

                await ProtectAccountNumberAsync(new Contact
                {
                    ContactId = row.ContactId,
                    AccountNumber = row.AccountNumber
                });
                converted++;
            }

            var agreementRows = await _propertyRepository.GetClearAgreementAccountNumbersAsync(dto.OrganizationId);
            foreach (var row in agreementRows)
            {
                if (!IsClearBankAccountNumber(row.AccountNumber))
                    continue;

                var digits = new string(row.AccountNumber.Where(char.IsDigit).ToArray());
                var lastFour = digits.Length <= 4 ? digits : digits[^4..];
                var encrypted = await _encryptionService.EncryptAsync(digits);
                await _propertyRepository.SaveEncryptedAgreementAccountNumberAsync(row.PropertyId, encrypted, lastFour);
                converted++;
            }

            return Ok(new EncryptStoredAccountNumbersResponseDto { Converted = converted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting stored account numbers for organization {OrganizationId}", dto.OrganizationId);
            return ServerError("An error occurred while encrypting stored account numbers");
        }
    }

    [HttpPost("{contactId:guid}/account-number/encrypt")]
    public async Task<IActionResult> EncryptAccountNumberAsync(Guid contactId, [FromBody] EncryptAccountNumberRequestDto? dto)
    {
        if (contactId == Guid.Empty)
            return BadRequest("Contact ID is required");

        if (!CanViewStoredAccountNumber())
            return Forbid();

        try
        {
            var contact = await GetAuthorizedContactAsync(contactId);
            if (contact == null)
                return NotFound("Contact not found");

            if (IsClearBankAccountNumber(dto?.AccountNumber))
                contact.AccountNumber = dto!.AccountNumber;

            if (!IsClearBankAccountNumber(contact.AccountNumber))
                return Ok(new EncryptAccountNumberResponseDto { Converted = false, AccountNumberLastFour = contact.AccountNumberLastFour });

            await ProtectAccountNumberAsync(contact);
            return Ok(new EncryptAccountNumberResponseDto { Converted = true, AccountNumberLastFour = contact.AccountNumberLastFour });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting account number for contact {ContactId}", contactId);
            return ServerError("An error occurred while encrypting the account number");
        }
    }

    [HttpDelete("{contactId:guid}/bank-information")]
    public async Task<IActionResult> ClearBankInformationAsync(Guid contactId)
    {
        if (contactId == Guid.Empty)
            return BadRequest("Contact ID is required");

        if (!CanViewStoredAccountNumber())
            return Forbid();

        try
        {
            var contact = await GetAuthorizedContactAsync(contactId);
            if (contact == null)
                return NotFound("Contact not found");

            await _contactRepository.ClearBankInformationAsync(contactId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing bank information for contact {ContactId}", contactId);
            return ServerError("An error occurred while clearing the bank information");
        }
    }

    [HttpGet("{contactId:guid}/account-number")]
    public async Task<IActionResult> GetAccountNumberAsync(Guid contactId)
    {
        if (contactId == Guid.Empty)
            return BadRequest("Contact ID is required");

        if (!CanViewStoredAccountNumber())
            return Forbid();

        try
        {
            var contact = await GetAuthorizedContactAsync(contactId);
            if (contact == null)
                return NotFound("Contact not found");

            if (IsClearBankAccountNumber(contact.AccountNumber))
                return Ok(new AccountNumberRevealResponseDto { AccountNumber = contact.AccountNumber! });

            var encrypted = await _contactRepository.GetEncryptedAccountNumberAsync(contactId);
            if (encrypted == null || encrypted.Length == 0)
                return Ok(new AccountNumberRevealResponseDto { AccountNumber = string.Empty });

            var accountNumber = await _encryptionService.DecryptAsync(encrypted);
            return Ok(new AccountNumberRevealResponseDto { AccountNumber = accountNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting account number for contact {ContactId}", contactId);
            return ServerError("An error occurred while retrieving the account number");
        }
    }

    private bool CanViewStoredAccountNumber()
        => HasAnyUserGroup(
            RoleType.SuperAdmin,
            RoleType.Admin,
            RoleType.OfficeAdmin,
            RoleType.Accounting,
            RoleType.AccountingAdmin);

    private async Task ProtectAccountNumberAsync(Contact contact)
    {
        var raw = contact.AccountNumber;
        if (!IsClearBankAccountNumber(raw))
        {
            if (!string.IsNullOrWhiteSpace(raw) && raw.Contains('*', StringComparison.Ordinal))
                contact.AccountNumber = null;
            return;
        }

        var digits = new string(raw!.Where(char.IsDigit).ToArray());
        var lastFour = digits.Length <= 4 ? digits : digits[^4..];
        var encrypted = await _encryptionService.EncryptAsync(digits);
        await _contactRepository.SaveEncryptedAccountNumberAsync(contact.ContactId, encrypted, lastFour, CurrentUser);
        contact.AccountNumber = null;
        contact.AccountNumberLastFour = lastFour;
    }

    private static bool IsClearBankAccountNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('*', StringComparison.Ordinal))
            return false;

        var compact = value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        return compact.Length >= 4 && compact.All(char.IsDigit);
    }
}

public sealed class EncryptStoredAccountNumbersRequestDto
{
    public Guid OrganizationId { get; set; }
}

public sealed class EncryptStoredAccountNumbersResponseDto
{
    public int Converted { get; set; }
}

public sealed class EncryptAccountNumberRequestDto
{
    public string? AccountNumber { get; set; }
}

public sealed class EncryptAccountNumberResponseDto
{
    public bool Converted { get; set; }
    public string? AccountNumberLastFour { get; set; }
}

public sealed class AccountNumberRevealResponseDto
{
    public string AccountNumber { get; set; } = string.Empty;
}
