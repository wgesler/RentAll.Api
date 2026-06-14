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

            return Ok(new CheckHtmlResponseDto(checkHtml));
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

            return Ok(new CheckHtmlResponseDto(checkHtml));
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
            return Ok(rows.Select(row => new CheckHtmlResponseDto(row)));
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
            var created = await _accountingRepository.CreateCheckHtmlAsync(dto.ToModel(CurrentUser));
            return Ok(new CheckHtmlResponseDto(created));
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

            var updated = await _accountingRepository.UpdateCheckHtmlByIdAsync(dto.ToModel(CurrentUser));
            return Ok(new CheckHtmlResponseDto(updated));
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
}
