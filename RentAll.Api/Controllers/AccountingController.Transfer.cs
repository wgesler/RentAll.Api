using RentAll.Api.Dtos.Accounting.Transfers;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Get

    [HttpPost("transfer/search")]
    public async Task<IActionResult> SearchTransfers([FromBody] GetTransfersDto dto)
    {
        if (dto == null)
            return BadRequest("Transfer search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var records = await _accountingRepository.GetTransfersByCriteriaAsync(criteria);
            var response = records.Select(o => new TransferResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching transfers");
            return ServerError("An error occurred while retrieving transfers");
        }
    }

    [HttpGet("transfer")]
    public async Task<IActionResult> GetAllTransfers()
    {
        try
        {
            var records = await _accountingRepository.GetTransfersByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new TransferResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfers");
            return ServerError("An error occurred while retrieving transfers");
        }
    }

    [HttpGet("transfer/office/{officeId:int}")]
    public async Task<IActionResult> GetTransfersByOfficeId(int officeId)
    {
        if (officeId <= 0)
            return BadRequest("OfficeId is required");

        try
        {
            var officeAccess = officeId.ToString();
            var records = await _accountingRepository.GetTransfersByOfficeIdsAsync(CurrentOrganizationId, officeAccess);
            var response = records.Select(o => new TransferResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfers");
            return ServerError("An error occurred while retrieving transfers");
        }
    }

    [HttpGet("transfer/property/{propertyId:guid}")]
    public async Task<IActionResult> GetTransfersByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _accountingRepository.GetTransfersByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new TransferResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfers for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving transfers");
        }
    }

    [HttpGet("transfer/{transferId:guid}")]
    public async Task<IActionResult> GetTransferById(Guid transferId)
    {
        if (transferId == Guid.Empty)
            return BadRequest("TransferId is required");

        try
        {
            var record = await _accountingRepository.GetTransferByIdAsync(transferId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Transfer record not found");

            var response = await MapTransferResponseAsync(record);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer by ID: {TransferId}", transferId);
            return ServerError("An error occurred while retrieving the transfer");
        }
    }

    #endregion

    #region Post

    [HttpPost("transfer")]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferDto dto)
    {
        if (dto == null)
            return BadRequest("Transfer data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var periodCheck = await RefuseIfAccountingPeriodClosedAsync(_accountingRepository, CurrentOrganizationId, dto.OfficeId, dto.AccountingPeriod, "create the transfer");
        if (periodCheck != null)
            return periodCheck;

        try
        {
            var transferCode = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Transfer);
            if (string.IsNullOrWhiteSpace(transferCode))
                return ServerError("Unable to generate transfer code");

            var transfer = dto.ToModel(transferCode, CurrentUser);
            transfer = await _accountingManager.PrepareTransferForSaveAsync(transfer);
            var created = await _accountingRepository.CreateTransferAsync(transfer);

            var journalEntry = await _accountingManager.CreateJournalEntryFromTransferAsync(created, CurrentUser);
            if (journalEntry != null)
            {
                created.JournalEntryId = journalEntry.JournalEntryId;
                created.ModifiedBy = CurrentUser;
                created = await _accountingRepository.UpdateTransferAsync(created);
            }

            var response = await MapTransferResponseAsync(created);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transfer");
            return ServerError("An error occurred while creating the transfer");
        }
    }

    [HttpPost("transfer/{transferId:guid}/post-report")]
    public async Task<IActionResult> PostTransferReport(Guid transferId)
    {
        if (transferId == Guid.Empty)
            return BadRequest("TransferId is required");

        try
        {
            var existing = await _accountingRepository.GetTransferByIdAsync(transferId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Transfer record not found");

            var hardClosedResult = RefuseIfJournalEntryHardClosed(existing.PostingStatusId, "transfer");
            if (hardClosedResult != null)
                return hardClosedResult;

            var periodCheck = await RefuseIfAccountingPeriodClosedAsync(
                _accountingRepository,
                CurrentOrganizationId,
                existing.OfficeId,
                existing.AccountingPeriod,
                "post the transfer report");
            if (periodCheck != null)
                return periodCheck;

            var updated = await _accountingManager.PostTransferReportAsync(transferId, CurrentOrganizationId, CurrentUser);
            return Ok(await MapTransferResponseAsync(updated));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting transfer report: {TransferId}", transferId);
            return ServerError(ex.Message);
        }
    }

    #endregion

    #region Put

    [HttpPut("transfer")]
    public async Task<IActionResult> UpdateTransfer([FromBody] UpdateTransferDto dto)
    {
        if (dto == null)
            return BadRequest("Transfer data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _accountingRepository.GetTransferByIdAsync(dto.TransferId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Transfer record not found");

            var hardClosedResult = RefuseIfJournalEntryHardClosed(existing.PostingStatusId, "transfer");
            if (hardClosedResult != null)
                return hardClosedResult;

            var transfer = dto.ToModel(CurrentUser);
            var updated = await _accountingManager.UpdateTransferAsync(transfer, CurrentUser);
            var response = await MapTransferResponseAsync(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transfer: {TransferId}", dto.TransferId);
            return ServerError("An error occurred while updating the transfer");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("transfer/{transferId:guid}")]
    public async Task<IActionResult> DeleteTransferById(Guid transferId)
    {
        if (transferId == Guid.Empty)
            return BadRequest("TransferId is required");

        try
        {
            var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, CurrentOrganizationId);
            if (transfer == null)
                return NotFound("Transfer record not found");

            await _accountingManager.DeleteJournalEntriesForTransferAsync(transfer);
            await _accountingRepository.DeleteTransferByIdAsync(transferId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transfer: {TransferId}", transferId);
            return ServerError("An error occurred while deleting the transfer");
        }
    }

    #endregion

    private async Task<TransferResponseDto> MapTransferResponseAsync(Transfer transfer)
    {
        await _accountingManager.EnrichTransferSplitsForDisplayAsync(transfer);
        return new TransferResponseDto(transfer);
    }
}
