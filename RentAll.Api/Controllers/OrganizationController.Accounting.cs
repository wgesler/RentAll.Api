
using RentAll.Api.Dtos.Accounting.BankCards;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        #region Get

        [HttpGet("accounting-office")]
        public async Task<IActionResult> GetAccountingOfficesByOfficeIdAsync()
        {
            try
            {
                var accountingOffices = await _organizationRepository.GetAccountingOfficesByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var accountingOfficeList = accountingOffices.ToList();
                foreach (var accountingOffice in accountingOfficeList)
                    accountingOffice.BankCards = await LoadBankCardsNoDecryptionAsync(accountingOffice.OfficeId);

                var response = new List<AccountingOfficeResponseDto>();
                foreach (var accountingOffice in accountingOfficeList)
                {
                    var dto = new AccountingOfficeResponseDto(accountingOffice);
                    if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
                        dto.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(accountingOffice.OrganizationId, await GetOfficeNameAsync(accountingOffice.OfficeId), accountingOffice.LogoPath, ImageType.Logos);
                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all accounting offices");
                return ServerError("An error occurred while retrieving accounting offices");
            }
        }

        [HttpGet("accounting-office/{officeId}")]
        public async Task<IActionResult> GetAccountingOfficeByIdAsync(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                var accountingOffice = await LoadAccountingOfficeWithBankCardsAsync(officeId);
                if (accountingOffice == null)
                    return NotFound("Accounting office not found");

                var response = new AccountingOfficeResponseDto(accountingOffice);
                if (!string.IsNullOrWhiteSpace(accountingOffice.LogoPath))
                    response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(accountingOffice.OrganizationId, await GetOfficeNameAsync(officeId), accountingOffice.LogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounting office by ID: {OfficeId}", officeId);
                return ServerError("An error occurred while retrieving the accounting office");
            }
        }
        #endregion

        #region Post
        [HttpPost("accounting-office")]
        public async Task<IActionResult> CreateAccountingOffice([FromBody] CreateAccountingOfficeDto dto)
        {
            if (dto == null)
                return BadRequest("Accounting office data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid accounting office data");

            try
            {
                // Check if accounting office already exists
                var existing = await _organizationRepository.GetAccountingOfficeByIdAsync(dto.OrganizationId, dto.OfficeId);
                if (existing != null)
                    return Conflict("Accounting office code already exists");

                var accountingOffice = dto.ToModel(CurrentUser);
                accountingOffice.OrganizationId = CurrentOrganizationId;

                accountingOffice.LogoPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(CurrentOrganizationId, await GetOfficeNameAsync(dto.OfficeId), dto.FileDetails, ImageType.Logos);

                var created = await _organizationRepository.CreateAccountingAsync(accountingOffice);
                await ReplaceBankCardsForOfficeAsync(created.OfficeId, dto.BankCards);

                var refreshedAccountingOffice = await LoadAccountingOfficeWithBankCardsAsync(created.OfficeId);
                if (refreshedAccountingOffice == null)
                    return NotFound("Accounting office not found");

                var response = new AccountingOfficeResponseDto(refreshedAccountingOffice);
                if (!string.IsNullOrWhiteSpace(refreshedAccountingOffice.LogoPath))
                    response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(refreshedAccountingOffice.OrganizationId, await GetOfficeNameAsync(refreshedAccountingOffice.OfficeId), refreshedAccountingOffice.LogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accounting office");
                return ServerError("An error occurred while creating the accounting office");
            }
        }

        #endregion

        #region Put
        [HttpPut("accounting-office/{officeId}/work-order-no")]
        public async Task<IActionResult> UpdateAccountingOfficeWorkOrderNoAsync(int officeId, [FromBody] UpdateAccountingOfficeWorkOrderNoDto dto)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            if (dto == null)
                return BadRequest("Work order number data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid work order number data");

            try
            {
                var existing = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (existing == null)
                    return NotFound("Accounting office not found");

                var updated = await _organizationRepository.UpdateAccountingOfficeWorkOrderNoByIdAsync(
                    CurrentOrganizationId,
                    officeId,
                    dto.WorkOrderNo,
                    CurrentUser);

                return Ok(new AccountingOfficeWorkOrderNoResponseDto(updated.OfficeId, updated.WorkOrderNo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accounting office work order number: {OfficeId}", officeId);
                return ServerError("An error occurred while updating the accounting office work order number");
            }
        }

        [HttpPut("accounting-office")]
        public async Task<IActionResult> UpdateAccountingOffice([FromBody] UpdateAccountingOfficeDto dto)
        {
            if (dto == null)
                return BadRequest("Accounting office data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid accounting office data");

            try
            {
                // Check if accounting office exists
                var existing = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, dto.OfficeId);
                if (existing == null)
                    return NotFound("Accounting office not found");

                var accountingOffice = dto.ToModel(CurrentUser);
                var officeName = await GetOfficeNameAsync(dto.OfficeId);
                accountingOffice.LogoPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(dto.OrganizationId, officeName, dto.FileDetails,
                    ImageType.Logos, existing.LogoPath, dto.LogoPath);

                var updated = await _organizationRepository.UpdateAccountingAsync(accountingOffice);
                if (dto.BankCards != null)
                    await ReplaceBankCardsForOfficeAsync(updated.OfficeId, dto.BankCards);

                var refreshedAccountingOffice = await LoadAccountingOfficeWithBankCardsAsync(updated.OfficeId);
                if (refreshedAccountingOffice == null)
                    return NotFound("Accounting office not found");

                var response = new AccountingOfficeResponseDto(refreshedAccountingOffice);
                if (!string.IsNullOrWhiteSpace(refreshedAccountingOffice.LogoPath))
                    response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(refreshedAccountingOffice.OrganizationId, await GetOfficeNameAsync(refreshedAccountingOffice.OfficeId), refreshedAccountingOffice.LogoPath, ImageType.Logos);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accounting office: {OfficeId}", dto.OfficeId);
                return ServerError("An error occurred while updating the accounting office");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("accounting-office/{officeId}")]
        public async Task<IActionResult> DeleteAccountingOfficeByIdAsync(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                // Check if accounting office exists and be sure to delete the logo file
                var office = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                if (office != null && !string.IsNullOrWhiteSpace(office.LogoPath))
                    await _fileService.DeleteImageAsync(office.OrganizationId, await GetOfficeNameAsync(officeId), office.LogoPath, ImageType.Logos);

                await DeleteBankCardsForOfficeAsync(officeId);

                await _organizationRepository.DeleteAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting accounting office: {OfficeId}", officeId);
                return ServerError("An error occurred while deleting the accounting office");
            }
        }

        #endregion

        #region Private Methods
        private async Task<List<BankCard>> LoadBankCardsNoDecryptionAsync(int officeId)
        {
            var bankCards = await _accountingRepository.GetBankCardsByOfficeIdAsync(CurrentOrganizationId, officeId);
            return bankCards;
        }

        private async Task<List<BankCard>> LoadBankCardsAsync(int officeId)
        {
            var bankCards = await _accountingRepository.GetBankCardsByOfficeIdAsync(CurrentOrganizationId, officeId);
            await DecryptAndMaskBankCardsAsync(bankCards);
            return bankCards;
        }

        private async Task<AccountingOffice?> LoadAccountingOfficeWithBankCardsAsync(int officeId)
        {
            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, officeId);
            if (accountingOffice == null)
                return null;

            accountingOffice.BankCards = await LoadBankCardsNoDecryptionAsync(officeId);
            return accountingOffice;
        }

        private async Task DeleteBankCardsForOfficeAsync(int officeId)
        {
            var existingCards = await _accountingRepository.GetBankCardsByOfficeIdAsync(CurrentOrganizationId, officeId);
            foreach (var existingCard in existingCards)
                await _accountingRepository.DeleteBankCardByIdAsync(existingCard.BankCardId, CurrentOrganizationId, officeId);
        }

        private async Task ReplaceBankCardsForOfficeAsync(int officeId, IEnumerable<CreateBankCardDto> bankCards)
        {
            await DeleteBankCardsForOfficeAsync(officeId);
            foreach (var bankCardDto in bankCards)
            {
                var model = bankCardDto.ToModel(CurrentOrganizationId, officeId);
                model.LastFour = ExtractLastFour(model.CardNumber);
                var encrypted = await _encryptionService.EncryptAsync(model.CardNumber);
                await _accountingRepository.CreateAsync(model, encrypted);
            }
        }

        private async Task ReplaceBankCardsForOfficeAsync(int officeId, IEnumerable<UpdateBankCardDto> bankCards)
        {
            var incomingCards = (bankCards ?? Enumerable.Empty<UpdateBankCardDto>()).ToList();
            var existingCards = await _accountingRepository.GetBankCardsByOfficeIdAsync(CurrentOrganizationId, officeId);

            var incomingIds = incomingCards
                .Where(card => card.BankCardId > 0)
                .Select(card => card.BankCardId)
                .ToList();

            if (incomingIds.Count != incomingIds.Distinct().Count())
                throw new InvalidOperationException("Duplicate BankCardId values are not allowed in update payload.");

            var existingById = existingCards.ToDictionary(card => card.BankCardId);

            foreach (var incomingId in incomingIds)
            {
                if (!existingById.ContainsKey(incomingId))
                    throw new InvalidOperationException($"Bank card {incomingId} does not exist for this office.");
            }

            foreach (var bankCardDto in incomingCards.Where(card => card.BankCardId > 0))
            {
                var model = bankCardDto.ToModel(CurrentOrganizationId, officeId);
                model.LastFour = ExtractLastFour(model.CardNumber);
                var encrypted = await _encryptionService.EncryptAsync(model.CardNumber);
                await _accountingRepository.UpdateByIdAsync(model, encrypted);
            }

            foreach (var bankCardDto in incomingCards.Where(card => card.BankCardId <= 0))
            {
                var model = bankCardDto.ToModel(CurrentOrganizationId, officeId);
                model.LastFour = ExtractLastFour(model.CardNumber);
                var encrypted = await _encryptionService.EncryptAsync(model.CardNumber);
                await _accountingRepository.CreateAsync(model, encrypted);
            }

            var idsToDelete = existingCards
                .Where(existing => !incomingIds.Contains(existing.BankCardId))
                .Select(existing => existing.BankCardId)
                .ToList();

            foreach (var idToDelete in idsToDelete)
            {
                await _accountingRepository.DeleteBankCardByIdAsync(idToDelete, CurrentOrganizationId, officeId);
            }
        }

        private async Task DecryptAndMaskBankCardsAsync(List<BankCard> bankCards)
        {
            foreach (var bankCard in bankCards)
            {
                if (string.IsNullOrWhiteSpace(bankCard.CardNumber))
                    continue;

                var cipherBytes = Convert.FromBase64String(bankCard.CardNumber);
                bankCard.CardNumber = await _encryptionService.DecryptAsync(cipherBytes);
                bankCard.LastFour = ExtractLastFour(bankCard.CardNumber);
            }
        }

        private static string ExtractLastFour(string cardNumber)
            => string.IsNullOrWhiteSpace(cardNumber)
                ? string.Empty
                : (cardNumber.Length <= 4 ? cardNumber : cardNumber[^4..]);
        #endregion

    }
}
