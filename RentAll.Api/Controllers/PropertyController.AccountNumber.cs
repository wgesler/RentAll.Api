namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    [HttpGet("property-agreement/{propertyId:guid}/account-number")]
    public async Task<IActionResult> GetAgreementAccountNumberAsync(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("Property ID is required");

        try
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
            if (property == null)
                return NotFound("Property not found");

            var agreement = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(propertyId);
            if (agreement == null)
                return NotFound("Property agreement not found");

            if (IsClearAgreementAccountNumber(agreement.AccountNumber))
                return Ok(new AccountNumberRevealResponseDto { AccountNumber = agreement.AccountNumber! });

            var encrypted = await _propertyRepository.GetEncryptedAgreementAccountNumberAsync(propertyId);
            if (encrypted == null || encrypted.Length == 0)
                return Ok(new AccountNumberRevealResponseDto { AccountNumber = string.Empty });

            var accountNumber = await _encryptionService.DecryptAsync(encrypted);
            return Ok(new AccountNumberRevealResponseDto { AccountNumber = accountNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting account number for property agreement {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving the account number");
        }
    }

    private async Task ProtectAgreementAccountNumberAsync(PropertyAgreement agreement)
    {
        var raw = agreement.AccountNumber;
        if (!IsClearAgreementAccountNumber(raw))
        {
            if (!string.IsNullOrWhiteSpace(raw) && raw.Contains('*'))
                agreement.AccountNumber = null;
            return;
        }

        var digits = new string(raw!.Where(char.IsDigit).ToArray());
        var lastFour = digits.Length <= 4 ? digits : digits[^4..];
        var encrypted = await _encryptionService.EncryptAsync(digits);
        await _propertyRepository.SaveEncryptedAgreementAccountNumberAsync(agreement.PropertyId, encrypted, lastFour);
        agreement.AccountNumber = null;
        agreement.AccountNumberLastFour = lastFour;
    }

    private static bool IsClearAgreementAccountNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('*'))
            return false;

        var compact = value.Replace(" ", string.Empty).Replace("-", string.Empty);
        return compact.Length >= 4 && compact.All(char.IsDigit);
    }
}
