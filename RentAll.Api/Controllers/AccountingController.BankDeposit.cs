using RentAll.Api.Dtos.Accounting.BankDeposits;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region BankDeposits
    [HttpPost("bank-deposit/search")]
    public async Task<IActionResult> SearchBankDeposits([FromBody] GetDepositsDto dto)
    {
        if (dto == null)
            return BadRequest("Deposit search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var records = await _accountingRepository.GetDepositsByCriteriaAsync(criteria);
            var response = records.Select(o => new DepositResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching bank deposits");
            return ServerError("An error occurred while retrieving deposits");
        }
    }

    [HttpGet("bank-deposit")]
    public async Task<IActionResult> GetAllBankDeposits()
    {
        try
        {
            var records = await _accountingRepository.GetDepositsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new DepositResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank deposits");
            return ServerError("An error occurred while retrieving deposits");
        }
    }

    [HttpGet("bank-deposit/office/{officeId:int}")]
    public async Task<IActionResult> GetBankDepositsByOfficeId(int officeId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        try
        {
            var officeAccess = officeId.ToString();
            var records = await _accountingRepository.GetDepositsByOfficeIdsAsync(CurrentOrganizationId, officeAccess);
            var response = records.Select(o => new DepositResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank deposits");
            return ServerError("An error occurred while retrieving deposits");
        }
    }

    [HttpGet("bank-deposit/property/{propertyId:guid}")]
    public async Task<IActionResult> GetBankDepositsByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _accountingRepository.GetDepositsByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new DepositResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank deposits for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving deposits");
        }
    }

    [HttpGet("bank-deposit/{depositId:guid}")]
    public async Task<IActionResult> GetBankDepositById(Guid depositId)
    {
        if (depositId == Guid.Empty)
            return BadRequest("DepositId is required");

        try
        {
            var record = await _accountingRepository.GetDepositByIdAsync(depositId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Deposit record not found");

            var response = new DepositResponseDto(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank deposit by ID: {DepositId}", depositId);
            return ServerError("An error occurred while retrieving the deposit");
        }
    }

    [HttpPost("bank-deposit")]
    public async Task<IActionResult> CreateBankDeposit([FromBody] CreateDepositDto dto)
    {
        if (dto == null)
            return BadRequest("Deposit data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var depositCode = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Deposit);
            if (string.IsNullOrWhiteSpace(depositCode))
                return ServerError("Unable to generate deposit code");

            var deposit = dto.ToModel(depositCode, CurrentUser);
            var created = await _accountingRepository.CreateDepositAsync(deposit);
            var response = new DepositResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bank deposit");
            return ServerError("An error occurred while creating the deposit");
        }
    }

    [HttpPut("bank-deposit")]
    public async Task<IActionResult> UpdateBankDeposit([FromBody] UpdateDepositDto dto)
    {
        if (dto == null)
            return BadRequest("Deposit data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _accountingRepository.GetDepositByIdAsync(dto.DepositId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Deposit record not found");

            var deposit = dto.ToModel(CurrentUser);
            deposit.JournalEntryId = existing.JournalEntryId;
            var updated = await _accountingRepository.UpdateDepositAsync(deposit);
            var response = new DepositResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bank deposit: {DepositId}", dto.DepositId);
            return ServerError("An error occurred while updating the deposit");
        }
    }

    [HttpDelete("bank-deposit/{depositId:guid}")]
    public async Task<IActionResult> DeleteBankDepositById(Guid depositId)
    {
        if (depositId == Guid.Empty)
            return BadRequest("DepositId is required");

        try
        {
            var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, CurrentOrganizationId);
            if (deposit == null)
                return NotFound("Deposit record not found");

            await _accountingRepository.DeleteDepositByIdAsync(depositId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bank deposit: {DepositId}", depositId);
            return ServerError("An error occurred while deleting the deposit");
        }
    }
    #endregion
}
