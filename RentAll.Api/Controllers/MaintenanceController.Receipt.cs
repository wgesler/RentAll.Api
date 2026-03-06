using RentAll.Api.Dtos.Maintenances.Receipts;
using RentAll.Domain.Enums;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpGet("receipt")]
    public async Task<IActionResult> GetAllReceipts()
    {
        try
        {
            var records = await _maintenanceRepository.GetReceiptsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new ReceiptResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting receipts");
            return ServerError("An error occurred while retrieving receipts");
        }
    }

    [HttpGet("receipt/property/{propertyId:guid}")]
    public async Task<IActionResult> GetReceiptsByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _maintenanceRepository.GetReceiptsByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new ReceiptResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting receipts for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving receipts");
        }
    }

    [HttpGet("receipt/{receiptId:int}")]
    public async Task<IActionResult> GetReceiptById(int receiptId)
    {
        if (receiptId <= 0)
            return BadRequest("ReceiptId is required");

        try
        {
            var record = await _maintenanceRepository.GetReceiptByIdAsync(receiptId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Receipt record not found");

            var response = new ReceiptResponseDto(record);
            if (!string.IsNullOrWhiteSpace(record.ReceiptPath))
                response.FileDetails = await _fileService.GetImageDetailsAsync(record.OrganizationId, null, record.ReceiptPath, ImageType.Receipts);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting receipt by ID: {ReceiptId}", receiptId);
            return ServerError("An error occurred while retrieving the receipt");
        }
    }
    #endregion

    #region Post
    [HttpPost("receipt")]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptDto dto)
    {
        if (dto == null)
            return BadRequest("Receipt data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var receipt = dto.ToModel(CurrentUser);
            var office = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, CurrentOrganizationId);
            if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
            {
                try
                {
                    var receiptPath = await _fileService.SaveImageAsync(dto.OrganizationId, office?.Name, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, ImageType.Receipts);
                    receipt.ReceiptPath = receiptPath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving receipt file");
                    return ServerError("An error occurred while saving the receipt file");
                }
            }

            var created = await _maintenanceRepository.CreateReceiptAsync(receipt);
            var response = new ReceiptResponseDto(created);
            if (!string.IsNullOrWhiteSpace(created.ReceiptPath))
                response.FileDetails = await _fileService.GetImageDetailsAsync(created.OrganizationId, office?.Name, created.ReceiptPath, ImageType.Receipts);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating receipt");
            return ServerError("An error occurred while creating the receipt");
        }
    }
    #endregion

    #region Put
    [HttpPut("receipt")]
    public async Task<IActionResult> UpdateReceipt([FromBody] UpdateReceiptDto dto)
    {
        if (dto == null)
            return BadRequest("Receipt data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _maintenanceRepository.GetReceiptByIdAsync(dto.ReceiptId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Receipt record not found");

            var receipt = dto.ToModel(CurrentUser);
            if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(existing.ReceiptPath))
                        await _fileService.DeleteImageAsync(existing.OrganizationId, null, existing.ReceiptPath, ImageType.Receipts);

                    var receiptPath = await _fileService.SaveImageAsync(existing.OrganizationId, null, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, ImageType.Receipts);
                    receipt.ReceiptPath = receiptPath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving receipt file");
                    return ServerError("An error occurred while saving the receipt file");
                }
            }
            else if (dto.ReceiptPath == null)
            {
                if (!string.IsNullOrWhiteSpace(existing.ReceiptPath))
                {
                    await _fileService.DeleteImageAsync(existing.OrganizationId, null, existing.ReceiptPath, ImageType.Receipts);
                    receipt.ReceiptPath = null;
                }
            }
            else
            {
                receipt.ReceiptPath = existing.ReceiptPath;
            }

            var updated = await _maintenanceRepository.UpdateReceiptAsync(receipt);

            var response = new ReceiptResponseDto(updated);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating receipt: {ReceiptId}", dto.ReceiptId);
            return ServerError("An error occurred while updating the receipt");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("receipt/{receiptId:int}")]
    public async Task<IActionResult> DeleteReceiptByIdAsync(int receiptId)
    {
        if (receiptId <= 0)
            return BadRequest("ReceiptId is required");

        try
        {
            await _maintenanceRepository.DeleteReceiptByIdAsync(receiptId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting receipt: {ReceiptId}", receiptId);
            return ServerError("An error occurred while deleting the receipt");
        }
    }

    #endregion
}
