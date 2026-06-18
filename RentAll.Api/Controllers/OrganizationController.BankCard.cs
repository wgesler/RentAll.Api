using RentAll.Api.Dtos.Accounting.BankCards;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        #region Bank Card Post
        [HttpPost("accounting-office/{officeId:int}/bank-card")]
        public async Task<IActionResult> CreateBankCardAsync(int officeId, [FromBody] CreateBankCardDto dto)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (dto == null)
                return BadRequest("Bank card data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid bank card data");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var created = await CreateBankCardInternalAsync(officeId, dto);
                return Ok(new BankCardResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank card for office: {OfficeId}", officeId);
                return ServerError("An error occurred while creating the bank card");
            }
        }
        #endregion

        #region Bank Card Put
        [HttpPut("accounting-office/{officeId:int}/bank-card/{bankCardId:int}")]
        public async Task<IActionResult> UpdateBankCardAsync(int officeId, int bankCardId, [FromBody] UpdateBankCardDto dto)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (bankCardId <= 0)
                return BadRequest("Bank card ID is required");

            if (dto == null)
                return BadRequest("Bank card data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid bank card data");

            if (dto.BankCardId != bankCardId)
                return BadRequest("BankCardId does not match route");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var existing = await _accountingRepository.GetBankCardByIdAsync(bankCardId, CurrentOrganizationId, officeId);
                if (existing == null)
                    return NotFound("Bank card not found");

                var updated = await UpdateBankCardInternalAsync(officeId, dto, existing);
                return Ok(new BankCardResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank card {BankCardId} for office: {OfficeId}", bankCardId, officeId);
                return ServerError("An error occurred while updating the bank card");
            }
        }
        #endregion

        #region Bank Card Delete
        [HttpDelete("accounting-office/{officeId:int}/bank-card/{bankCardId:int}")]
        public async Task<IActionResult> DeleteBankCardAsync(int officeId, int bankCardId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (bankCardId <= 0)
                return BadRequest("Bank card ID is required");

            try
            {
                var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var existing = await _accountingRepository.GetBankCardByIdAsync(bankCardId, CurrentOrganizationId, officeId);
                if (existing == null)
                    return NotFound("Bank card not found");

                await _accountingRepository.DeleteBankCardByIdAsync(bankCardId, CurrentOrganizationId, officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bank card {BankCardId} for office: {OfficeId}", bankCardId, officeId);
                return ServerError("An error occurred while deleting the bank card");
            }
        }
        #endregion

        #region Bank Card Private Methods
        private async Task<BankCard> CreateBankCardInternalAsync(int officeId, CreateBankCardDto dto)
        {
            var model = dto.ToModel(CurrentOrganizationId, officeId);
            model.LastFour = ExtractLastFour(model.CardNumber);
            var encrypted = await _encryptionService.EncryptAsync(model.CardNumber);
            return await _accountingRepository.CreateAsync(model, encrypted);
        }

        private async Task<BankCard> UpdateBankCardInternalAsync(int officeId, UpdateBankCardDto dto, BankCard existing)
        {
            var model = dto.ToModel(CurrentOrganizationId, officeId);
            byte[] encrypted;

            if (string.IsNullOrWhiteSpace(dto.CardNumber))
            {
                encrypted = Convert.FromBase64String(existing.CardNumber);
                model.LastFour = existing.LastFour;
            }
            else
            {
                model.LastFour = ExtractLastFour(model.CardNumber);
                encrypted = await _encryptionService.EncryptAsync(model.CardNumber);
            }

            return await _accountingRepository.UpdateByIdAsync(model, encrypted);
        }
        #endregion
    }
}
