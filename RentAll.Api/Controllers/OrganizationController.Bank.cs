using RentAll.Api.Dtos.Accounting.Banks;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        [HttpPost("accounting-office/{officeId:int}/bank")]
        public async Task<IActionResult> CreateBankAsync(int officeId, [FromBody] CreateBankDto dto)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (dto == null)
                return BadRequest("Bank data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid bank data");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var bank = dto.ToModel(CurrentOrganizationId, officeId, CurrentUser);
                var officeName = await GetOfficeNameAsync(officeId);
                if (dto.CheckStockFileDetails != null && !string.IsNullOrWhiteSpace(dto.CheckStockFileDetails.File))
                    bank.CheckStockPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(CurrentOrganizationId, officeName, dto.CheckStockFileDetails, ImageType.CheckStocks);

                var created = await _accountingRepository.CreateBankAsync(bank);
                return Ok(await ToBankResponseAsync(created, officeName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank for office: {OfficeId}", officeId);
                return ServerError("An error occurred while creating the bank");
            }
        }

        [HttpPut("accounting-office/{officeId:int}/bank/{bankId:int}")]
        public async Task<IActionResult> UpdateBankAsync(int officeId, int bankId, [FromBody] UpdateBankDto dto)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (bankId <= 0)
                return BadRequest("Bank ID is required");

            if (dto == null)
                return BadRequest("Bank data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid bank data");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var existing = await _accountingRepository.GetBankByIdAsync(bankId, CurrentOrganizationId, officeId);
                if (existing == null)
                    return NotFound("Bank not found");

                var bank = dto.ToModel(bankId, CurrentOrganizationId, officeId, CurrentUser);
                var officeName = await GetOfficeNameAsync(officeId);
                bank.CheckStockPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                    CurrentOrganizationId,
                    officeName,
                    dto.CheckStockFileDetails,
                    ImageType.CheckStocks,
                    existing.CheckStockPath,
                    dto.CheckStockPath);

                var updated = await _accountingRepository.UpdateBankByIdAsync(bank);
                return Ok(await ToBankResponseAsync(updated, officeName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank {BankId} for office: {OfficeId}", bankId, officeId);
                return ServerError("An error occurred while updating the bank");
            }
        }

        [HttpDelete("accounting-office/{officeId:int}/bank/{bankId:int}")]
        public async Task<IActionResult> DeleteBankAsync(int officeId, int bankId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (bankId <= 0)
                return BadRequest("Bank ID is required");

            try
            {
                var existing = await _accountingRepository.GetBankByIdAsync(bankId, CurrentOrganizationId, officeId);
                if (existing == null)
                    return NotFound("Bank not found");

                await _accountingRepository.DeleteBankByIdAsync(bankId, CurrentOrganizationId, officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bank {BankId} for office: {OfficeId}", bankId, officeId);
                return ServerError("An error occurred while deleting the bank");
            }
        }

        private async Task<BankResponseDto> ToBankResponseAsync(Bank bank, string? officeName)
        {
            var response = new BankResponseDto(bank);
            if (!string.IsNullOrWhiteSpace(bank.CheckStockPath))
                response.CheckStockFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(bank.OrganizationId, officeName, bank.CheckStockPath, ImageType.CheckStocks);
            return response;
        }

        private async Task HydrateBankCheckStockAsync(IEnumerable<BankResponseDto> banks, int officeId)
        {
            var officeName = await GetOfficeNameAsync(officeId);
            foreach (var bank in banks)
            {
                if (string.IsNullOrWhiteSpace(bank.CheckStockPath))
                    continue;
                bank.CheckStockFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(bank.OrganizationId, officeName, bank.CheckStockPath, ImageType.CheckStocks);
            }
        }
    }
}
