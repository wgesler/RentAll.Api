namespace RentAll.Api.Controllers;

public partial class LeadController
{
    [HttpGet("owners/html/{propertyId:guid}")]
    public async Task<IActionResult> GetOwnerHtmlByPropertyIdAsync(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("Property ID is required");

        try
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
            if (property == null)
                return NotFound("Property not found");

            var ownerHtml = await _leadRepository.GetOwnerHtmlByPropertyIdAsync(propertyId, CurrentOrganizationId);
            if (ownerHtml == null)
                return NotFound("Owner HTML not found");

            return Ok(new OwnerHtmlResponseDto(ownerHtml));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner HTML by Property ID: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving owner HTML");
        }
    }
}
