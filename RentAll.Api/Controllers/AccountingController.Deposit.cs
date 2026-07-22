using RentAll.Api.Dtos.Accounting.Deposits;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Get

    [HttpPost("deposit/search")]
    public async Task<IActionResult> SearchDeposits([FromBody] GetDepositsDto dto)
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
            _logger.LogError(ex, "Error searching deposits");
            return ServerError("An error occurred while retrieving deposits");
        }
    }

    [HttpGet("deposit")]
    public async Task<IActionResult> GetAllDeposits()
    {
        try
        {
            var records = await _accountingRepository.GetDepositsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new DepositResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deposits");
            return ServerError("An error occurred while retrieving deposits");
        }
    }

    [HttpGet("deposit/office/{officeId:int}")]
    public async Task<IActionResult> GetDepositsByOfficeId(int officeId)
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
            _logger.LogError(ex, "Error getting deposits");
            return ServerError("An error occurred while retrieving deposits");
        }
    }

    [HttpGet("deposit/property/{propertyId:guid}")]
    public async Task<IActionResult> GetDepositsByPropertyId(Guid propertyId)
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
            _logger.LogError(ex, "Error getting deposits for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving deposits");
        }
    }

    [HttpGet("deposit/{depositId:guid}")]
    public async Task<IActionResult> GetDepositById(Guid depositId)
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
            _logger.LogError(ex, "Error getting deposit by ID: {DepositId}", depositId);
            return ServerError("An error occurred while retrieving the deposit");
        }
    }

    #endregion

    #region Post

    [HttpPost("deposit")]
    public async Task<IActionResult> CreateDeposit([FromBody] CreateDepositDto dto)
    {
        if (dto == null)
            return BadRequest("Deposit data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var periodCheck = await RefuseIfAccountingPeriodClosedAsync(_accountingRepository, CurrentOrganizationId, dto.OfficeId, dto.AccountingPeriod, "create the deposit");
        if (periodCheck != null)
            return periodCheck;

        try
        {
            var depositCode = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Deposit);
            if (string.IsNullOrWhiteSpace(depositCode))
                return ServerError("Unable to generate deposit code");

            var deposit = dto.ToModel(depositCode, CurrentUser);
            deposit = await _accountingManager.PrepareDepositForSaveAsync(deposit);
            var created = await _accountingRepository.CreateDepositAsync(deposit);

            await _accountingManager.CreateJournalEntryFromDepositAsync(created, CurrentUser);

            var response = new DepositResponseDto(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deposit");
            return ServerError("An error occurred while creating the deposit");
        }
    }

    #endregion

    #region Put

    [HttpPut("deposit")]
    public async Task<IActionResult> UpdateDeposit([FromBody] UpdateDepositDto dto)
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

            var postingStatusCheck = RefuseIfDocumentUpdateNotAllowed(existing.PostingStatusId, "deposit");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            var deposit = dto.ToModel(CurrentUser);
            var updated = await _accountingManager.UpdateDepositAsync(deposit, CurrentUser);
            var response = new DepositResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating deposit: {DepositId}", dto.DepositId);
            return ServerError("An error occurred while updating the deposit");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("deposit/{depositId:guid}")]
    public async Task<IActionResult> DeleteDepositById(Guid depositId)
    {
        if (depositId == Guid.Empty)
            return BadRequest("DepositId is required");

        try
        {
            var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, CurrentOrganizationId);
            if (deposit == null)
                return NotFound("Deposit record not found");

            var postingStatusCheck = RefuseIfDocumentDeleteNotAllowed(deposit.PostingStatusId, "deposit");
            if (postingStatusCheck != null)
                return postingStatusCheck;

            await _accountingManager.DeleteJournalEntriesForDepositAsync(deposit);
            await _accountingRepository.DeleteDepositByIdAsync(depositId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting deposit: {DepositId}", depositId);
            return ServerError("An error occurred while deleting the deposit");
        }
    }

    #endregion
}
