using RentAll.Api.Dtos.Accounting.CheckHtmls;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Get

    [HttpGet("check-html")]
    public async Task<IActionResult> GetCheckHtmlByScope([FromQuery] int? officeId)
    {
        try
        {
            var checkHtml = await _accountingRepository.GetCheckHtmlByScopeAsync(CurrentOrganizationId, officeId);
            if (checkHtml == null)
                return NotFound("CheckHtml not found");

            return Ok(await ToCheckHtmlResponseDtoAsync(checkHtml));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CheckHtml for organization: {OrganizationId}, office: {OfficeId}", CurrentOrganizationId, officeId);
            return ServerError("An error occurred while retrieving CheckHtml");
        }
    }

    [HttpGet("check-html/{checkHtmlId:guid}")]
    public async Task<IActionResult> GetCheckHtmlById(Guid checkHtmlId)
    {
        try
        {
            var checkHtml = await _accountingRepository.GetCheckHtmlByIdAsync(checkHtmlId);
            if (checkHtml == null || checkHtml.OrganizationId != CurrentOrganizationId)
                return NotFound("CheckHtml not found");

            return Ok(await ToCheckHtmlResponseDtoAsync(checkHtml));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CheckHtml: {CheckHtmlId}", checkHtmlId);
            return ServerError("An error occurred while retrieving CheckHtml");
        }
    }

    [HttpGet("check-html/all")]
    public async Task<IActionResult> GetCheckHtmlAll()
    {
        try
        {
            var rows = await _accountingRepository.GetCheckHtmlAllAsync(CurrentOrganizationId);
            var response = new List<CheckHtmlResponseDto>();
            foreach (var row in rows)
                response.Add(await ToCheckHtmlResponseDtoAsync(row));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CheckHtml list for organization: {OrganizationId}", CurrentOrganizationId);
            return ServerError("An error occurred while retrieving CheckHtml");
        }
    }

    #endregion

    #region Post

    [HttpPost("check-html")]
    public async Task<IActionResult> CreateCheckHtml([FromBody] CreateCheckHtmlDto dto)
    {
        if (dto == null)
            return BadRequest("CheckHtml data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var checkHtml = dto.ToModel(CurrentUser);
            var savedPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(
                CurrentOrganizationId,
                await GetCheckStockStorageScopeAsync(dto.OfficeId),
                dto.CheckStockFileDetails,
                ImageType.CheckStocks);
            checkHtml.CheckStockPath = savedPath;

            var created = await _accountingRepository.CreateCheckHtmlAsync(checkHtml);
            if (!string.IsNullOrWhiteSpace(savedPath) && string.IsNullOrWhiteSpace(created.CheckStockPath))
            {
                created.CheckStockPath = savedPath;
                created.ModifiedBy = CurrentUser;
                created = await _accountingRepository.UpdateCheckHtmlByIdAsync(created);
            }

            return Ok(await ToCheckHtmlResponseDtoAsync(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating CheckHtml for organization: {OrganizationId}", CurrentOrganizationId);
            return ServerError("An error occurred while creating CheckHtml");
        }
    }

    #endregion

    #region Put

    [HttpPut("check-html")]
    public async Task<IActionResult> UpdateCheckHtml([FromBody] UpdateCheckHtmlDto dto)
    {
        if (dto == null)
            return BadRequest("CheckHtml data is required");

        var (isValid, errorMessage) = dto.IsValid(CurrentOrganizationId);
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _accountingRepository.GetCheckHtmlByIdAsync(dto.CheckHtmlId);
            if (existing == null || existing.OrganizationId != CurrentOrganizationId)
                return NotFound("CheckHtml not found");

            var checkHtml = dto.ToModel(CurrentUser);
            checkHtml.CheckStockPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                CurrentOrganizationId,
                await GetCheckStockStorageScopeAsync(dto.OfficeId),
                dto.CheckStockFileDetails,
                ImageType.CheckStocks,
                existing.CheckStockPath,
                dto.CheckStockPath);

            var updated = await _accountingRepository.UpdateCheckHtmlByIdAsync(checkHtml);
            return Ok(await ToCheckHtmlResponseDtoAsync(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CheckHtml: {CheckHtmlId}", dto.CheckHtmlId);
            return ServerError("An error occurred while updating CheckHtml");
        }
    }

    #endregion

    #region Delete

    [HttpDelete("check-html/{checkHtmlId:guid}")]
    public async Task<IActionResult> DeleteCheckHtml(Guid checkHtmlId)
    {
        try
        {
            var existing = await _accountingRepository.GetCheckHtmlByIdAsync(checkHtmlId);
            if (existing == null || existing.OrganizationId != CurrentOrganizationId)
                return NotFound("CheckHtml not found");

            if (!string.IsNullOrWhiteSpace(existing.CheckStockPath))
            {
                await _fileService.DeleteImageAsync(
                    existing.OrganizationId,
                    await GetCheckStockStorageScopeAsync(existing.OfficeId),
                    existing.CheckStockPath,
                    ImageType.CheckStocks);
            }

            await _accountingRepository.DeleteCheckHtmlByIdAsync(checkHtmlId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting CheckHtml: {CheckHtmlId}", checkHtmlId);
            return ServerError("An error occurred while deleting CheckHtml");
        }
    }

    #endregion

    #region CheckHtml Helpers

    private async Task<CheckHtmlResponseDto> ToCheckHtmlResponseDtoAsync(CheckHtml checkHtml)
    {
        var response = new CheckHtmlResponseDto(checkHtml);
        if (string.IsNullOrWhiteSpace(checkHtml.CheckStockPath))
            return response;

        // Match receipt retrieve: try office scope first, then null (Azure parses path; local FileService needs matching prefix).
        var officeScope = await GetCheckStockStorageScopeAsync(checkHtml.OfficeId);
        response.CheckStockFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
            checkHtml.OrganizationId,
            officeScope,
            checkHtml.CheckStockPath,
            ImageType.CheckStocks);

        if (response.CheckStockFileDetails == null && !string.IsNullOrWhiteSpace(officeScope))
        {
            response.CheckStockFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                checkHtml.OrganizationId,
                null,
                checkHtml.CheckStockPath,
                ImageType.CheckStocks);
        }

        return response;
    }

    private async Task<string?> GetCheckStockStorageScopeAsync(int? officeId)
    {
        if (!officeId.HasValue || officeId.Value <= 0)
            return null;

        var office = await _organizationRepository.GetOfficeByIdAsync(officeId.Value, CurrentOrganizationId);
        return office?.Name;
    }

    #endregion
}
