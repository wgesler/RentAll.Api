using RentAll.Api.Dtos.Accounting.BankAccounts;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        [HttpPost("accounting-office/{officeId:int}/bank")]
        public async Task<IActionResult> CreateBankAccountAsync(int officeId, [FromBody] CreateBankAccountDto dto)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (dto == null)
                return BadRequest("Bank account data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid bank account data");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var bankAccount = dto.ToModel(CurrentOrganizationId, officeId, CurrentUser);
                var officeName = await GetOfficeNameAsync(officeId);
                if (dto.CheckStockFileDetails != null && !string.IsNullOrWhiteSpace(dto.CheckStockFileDetails.File))
                    bankAccount.CheckStockPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(CurrentOrganizationId, officeName, dto.CheckStockFileDetails, ImageType.CheckStocks);

                var clearBankAccount = ClearBankAccountOrNull(bankAccount.AccountNo);
                var created = await _accountingRepository.CreateBankAccountAsync(bankAccount);
                if (clearBankAccount != null)
                    await ProtectBankAccountNumberAsync(created, clearBankAccount);
                return Ok(await ToBankAccountResponseAsync(created, officeName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank account for office: {OfficeId}", officeId);
                return ServerError("An error occurred while creating the bank account");
            }
        }

        [HttpPut("accounting-office/{officeId:int}/bank/{bankId:int}")]
        public async Task<IActionResult> UpdateBankAccountAsync(int officeId, int bankId, [FromBody] UpdateBankAccountDto dto)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (bankId <= 0)
                return BadRequest("Bank account ID is required");

            if (dto == null)
                return BadRequest("Bank account data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid bank account data");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var existing = await _accountingRepository.GetBankAccountByIdAsync(bankId, CurrentOrganizationId, officeId);
                if (existing == null)
                    return NotFound("Bank account not found");

                var bankAccount = dto.ToModel(bankId, CurrentOrganizationId, officeId, CurrentUser);
                var officeName = await GetOfficeNameAsync(officeId);
                bankAccount.CheckStockPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                    CurrentOrganizationId,
                    officeName,
                    dto.CheckStockFileDetails,
                    ImageType.CheckStocks,
                    existing.CheckStockPath,
                    dto.CheckStockPath);

                var clearBankAccount = ClearBankAccountOrNull(dto.AccountNo) ?? ClearBankAccountOrNull(existing.AccountNo);
                bankAccount.AccountNo = clearBankAccount;
                var updated = await _accountingRepository.UpdateBankAccountByIdAsync(bankAccount);
                if (clearBankAccount != null)
                    await ProtectBankAccountNumberAsync(updated, clearBankAccount);
                return Ok(await ToBankAccountResponseAsync(updated, officeName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank account {BankId} for office: {OfficeId}", bankId, officeId);
                return ServerError("An error occurred while updating the bank account");
            }
        }

        [HttpDelete("accounting-office/{officeId:int}/bank/{bankId:int}")]
        public async Task<IActionResult> DeleteBankAccountAsync(int officeId, int bankId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (bankId <= 0)
                return BadRequest("Bank account ID is required");

            try
            {
                var existing = await _accountingRepository.GetBankAccountByIdAsync(bankId, CurrentOrganizationId, officeId);
                if (existing == null)
                    return NotFound("Bank account not found");

                await _accountingRepository.DeleteBankAccountByIdAsync(bankId, CurrentOrganizationId, officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bank account {BankId} for office: {OfficeId}", bankId, officeId);
                return ServerError("An error occurred while deleting the bank account");
            }
        }

        private async Task<BankAccountResponseDto> ToBankAccountResponseAsync(BankAccount bankAccount, string? officeName)
        {
            var response = new BankAccountResponseDto(bankAccount);
            response.AccountNo = null;
            if (!string.IsNullOrWhiteSpace(bankAccount.CheckStockPath))
                response.CheckStockFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(bankAccount.OrganizationId, officeName, bankAccount.CheckStockPath, ImageType.CheckStocks);
            return response;
        }

        private async Task HydrateBankAccountCheckStockAsync(IEnumerable<BankAccountResponseDto> bankAccounts, int officeId)
        {
            var officeName = await GetOfficeNameAsync(officeId);
            foreach (var bankAccount in bankAccounts)
            {
                if (string.IsNullOrWhiteSpace(bankAccount.CheckStockPath))
                    continue;
                bankAccount.CheckStockFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(bankAccount.OrganizationId, officeName, bankAccount.CheckStockPath, ImageType.CheckStocks);
            }
        }
    }
}
