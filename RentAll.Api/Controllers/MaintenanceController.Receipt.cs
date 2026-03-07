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
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(record.OrganizationId, null, record.ReceiptPath, ImageType.Receipts);

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
            receipt.ReceiptPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, office?.Name, dto.FileDetails, ImageType.Receipts);

            var created = await _maintenanceRepository.CreateReceiptAsync(receipt);
            var response = new ReceiptResponseDto(created);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(created.OrganizationId, office?.Name, created.ReceiptPath, ImageType.Receipts);

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
            receipt.ReceiptPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                existing.OrganizationId, null, dto.FileDetails, ImageType.Receipts, existing.ReceiptPath, dto.ReceiptPath);

            var updated = await _maintenanceRepository.UpdateReceiptAsync(receipt);

            var response = new ReceiptResponseDto(updated);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(updated.OrganizationId, null, updated.ReceiptPath, ImageType.Receipts);
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
