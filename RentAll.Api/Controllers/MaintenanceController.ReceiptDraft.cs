using RentAll.Api.Dtos.Maintenances.ReceiptDrafts;
using RentAll.Api.Dtos.Maintenances.Receipts;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpPost("receipt-draft/search")]
    public async Task<IActionResult> SearchReceiptDrafts([FromBody] GetReceiptDraftsDto dto)
    {
        if (dto == null)
            return BadRequest("Receipt draft search criteria is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var criteria = dto.ToCriteria(CurrentOrganizationId);
            var records = await _maintenanceRepository.GetReceiptDraftsByCriteriaAsync(criteria);
            var response = records.Select(record => new ReceiptDraftResponseDto(record));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching receipt drafts");
            return ServerError("An error occurred while retrieving receipt drafts");
        }
    }

    [HttpGet("receipt-draft/{receiptDraftId:guid}")]
    public async Task<IActionResult> GetReceiptDraftById(Guid receiptDraftId)
    {
        if (receiptDraftId == Guid.Empty)
            return BadRequest("ReceiptDraftId is required");

        try
        {
            var record = await _maintenanceRepository.GetReceiptDraftByIdAsync(receiptDraftId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Receipt draft record not found");

            var response = new ReceiptDraftResponseDto(record);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(record.OrganizationId, null, record.ReceiptPath, ImageType.Receipts);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting receipt draft by ID: {ReceiptDraftId}", receiptDraftId);
            return ServerError("An error occurred while retrieving the receipt draft");
        }
    }
    #endregion

    #region Post
    [HttpPost("receipt-draft")]
    public async Task<IActionResult> CreateReceiptDraft([FromBody] CreateReceiptDraftDto dto)
    {
        if (dto == null)
            return BadRequest("Receipt draft data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var draftCode = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.ReceiptDraft);
            if (string.IsNullOrWhiteSpace(draftCode))
                return ServerError("Unable to generate receipt draft code");

            var draft = dto.ToModel(draftCode, CurrentUser);
            var office = dto.OfficeId is > 0
                ? await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId.Value, CurrentOrganizationId)
                : null;
            draft.ReceiptPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, office?.Name, dto.FileDetails, ImageType.Receipts);

            var created = await _maintenanceRepository.CreateReceiptDraftAsync(draft);
            var response = new ReceiptDraftResponseDto(created);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(created.OrganizationId, office?.Name, created.ReceiptPath, ImageType.Receipts);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating receipt draft");
            return ServerError("An error occurred while creating the receipt draft");
        }
    }

    [HttpPost("receipt-draft/{receiptDraftId:guid}/link/{receiptId:guid}")]
    public async Task<IActionResult> LinkReceiptDraftToReceipt(Guid receiptDraftId, Guid receiptId)
    {
        if (receiptDraftId == Guid.Empty)
            return BadRequest("ReceiptDraftId is required");

        if (receiptId == Guid.Empty)
            return BadRequest("ReceiptId is required");

        try
        {
            var draft = await _maintenanceRepository.GetReceiptDraftByIdAsync(receiptDraftId, CurrentOrganizationId);
            if (draft == null)
                return NotFound("Receipt draft record not found");

            if (draft.IsPromoted)
                return BadRequest("Receipt draft has already been promoted");

            var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptId, CurrentOrganizationId);
            if (receipt == null)
                return NotFound("Receipt record not found");

            if (receipt.OrganizationId != CurrentOrganizationId)
                return Unauthorized("Invalid organization Id");

            var promotedDraft = await _maintenanceRepository.MarkReceiptDraftPromotedAsync(
                receiptDraftId,
                CurrentOrganizationId,
                receiptId,
                CurrentUser);

            var response = new ReceiptDraftResponseDto(promotedDraft);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                promotedDraft.OrganizationId,
                null,
                promotedDraft.ReceiptPath,
                ImageType.Receipts);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking receipt draft {ReceiptDraftId} to receipt {ReceiptId}", receiptDraftId, receiptId);
            return ServerError("An error occurred while linking the receipt draft");
        }
    }

    [HttpPost("receipt-draft/{receiptDraftId:guid}/promote")]
    public async Task<IActionResult> PromoteReceiptDraft(Guid receiptDraftId)
    {
        if (receiptDraftId == Guid.Empty)
            return BadRequest("ReceiptDraftId is required");

        try
        {
            var draft = await _maintenanceRepository.GetReceiptDraftByIdAsync(receiptDraftId, CurrentOrganizationId);
            if (draft == null)
                return NotFound("Receipt draft record not found");

            if (draft.IsPromoted)
                return BadRequest("Receipt draft has already been promoted");

            var (isValid, errorMessage, receiptDto) = ReceiptDraftPromotionMapper.ToCreateReceiptDto(draft);
            if (!isValid || receiptDto == null)
                return BadRequest(errorMessage ?? "Receipt draft is not ready for promotion");

            var periodCheck = await RefuseIfAccountingPeriodClosedAsync(
                _accountingRepository,
                CurrentOrganizationId,
                receiptDto.OfficeId,
                receiptDto.AccountingPeriod,
                "promote the receipt draft");
            if (periodCheck != null)
                return periodCheck;

            var receiptCode = await _organizationManager.GenerateEntityCodeAsync(receiptDto.OrganizationId, EntityType.Receipt);
            if (string.IsNullOrWhiteSpace(receiptCode))
                return ServerError("Unable to generate receipt code");

            var receipt = receiptDto.ToModel(receiptCode, CurrentUser);
            if (string.IsNullOrWhiteSpace(receipt.ReceiptPath) && !string.IsNullOrWhiteSpace(draft.ReceiptPath))
                receipt.ReceiptPath = draft.ReceiptPath;

            var createdReceipt = await _accountingManager.CreateReceiptAsync(receipt, CurrentUser);
            var promotedDraft = await _maintenanceRepository.MarkReceiptDraftPromotedAsync(
                receiptDraftId,
                CurrentOrganizationId,
                createdReceipt.ReceiptId,
                CurrentUser);

            var response = new PromoteReceiptDraftResponseDto
            {
                Draft = new ReceiptDraftResponseDto(promotedDraft),
                Receipt = new ReceiptResponseDto(createdReceipt)
            };
            response.Draft.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                promotedDraft.OrganizationId,
                null,
                promotedDraft.ReceiptPath,
                ImageType.Receipts);
            response.Receipt.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                createdReceipt.OrganizationId,
                null,
                createdReceipt.ReceiptPath,
                ImageType.Receipts);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting receipt draft: {ReceiptDraftId}", receiptDraftId);
            return ServerError("An error occurred while promoting the receipt draft");
        }
    }
    #endregion

    #region Put
    [HttpPut("receipt-draft")]
    public async Task<IActionResult> UpdateReceiptDraft([FromBody] UpdateReceiptDraftDto dto)
    {
        if (dto == null)
            return BadRequest("Receipt draft data is required");

        if (dto.OrganizationId != CurrentOrganizationId)
            return Unauthorized("Invalid organization Id");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _maintenanceRepository.GetReceiptDraftByIdAsync(dto.ReceiptDraftId, CurrentOrganizationId);
            if (existing == null)
                return NotFound("Receipt draft record not found");

            if (existing.IsPromoted)
                return BadRequest("Promoted receipt drafts cannot be edited");

            var draft = dto.ToModel(CurrentUser);
            draft.DraftCode = existing.DraftCode;
            var receiptPathForUpdate = string.IsNullOrWhiteSpace(dto.ReceiptPath)
                && dto.FileDetails == null
                && !string.IsNullOrWhiteSpace(existing.ReceiptPath)
                    ? existing.ReceiptPath
                    : dto.ReceiptPath;

            draft.ReceiptPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                existing.OrganizationId,
                null,
                dto.FileDetails,
                ImageType.Receipts,
                existing.ReceiptPath,
                receiptPathForUpdate);

            var updated = await _maintenanceRepository.UpdateReceiptDraftAsync(draft);
            var response = new ReceiptDraftResponseDto(updated);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(updated.OrganizationId, null, updated.ReceiptPath, ImageType.Receipts);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating receipt draft: {ReceiptDraftId}", dto.ReceiptDraftId);
            return ServerError("An error occurred while updating the receipt draft");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("receipt-draft/{receiptDraftId:guid}")]
    public async Task<IActionResult> DeleteReceiptDraftById(Guid receiptDraftId)
    {
        if (receiptDraftId == Guid.Empty)
            return BadRequest("ReceiptDraftId is required");

        try
        {
            var draft = await _maintenanceRepository.GetReceiptDraftByIdAsync(receiptDraftId, CurrentOrganizationId);
            if (draft == null)
                return NotFound("Receipt draft record not found");

            if (draft.IsPromoted)
                return BadRequest("Promoted receipt drafts cannot be deleted");

            await _maintenanceRepository.DeleteReceiptDraftByIdAsync(receiptDraftId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting receipt draft: {ReceiptDraftId}", receiptDraftId);
            return ServerError("An error occurred while deleting the receipt draft");
        }
    }
    #endregion
}
