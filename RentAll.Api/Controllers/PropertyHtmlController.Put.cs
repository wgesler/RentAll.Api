using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.PropertyHtmls;

namespace RentAll.Api.Controllers
{
    public partial class PropertyHtmlController
    {
        /// <summary>
        /// Update an existing property HTML
        /// </summary>
        /// <param name="dto">Property HTML data</param>
        /// <returns>Updated property HTML</returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdatePropertyHtmlDto dto)
        {
            if (dto == null)
                return BadRequest("Property HTML data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if HTML exists
                var existing = await _propertyRepository.GetPropertyHtmlByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (existing == null)
                    return NotFound("Property HTML not found");

                var propertyHtml = dto.ToModel(CurrentUser, CurrentOrganizationId);
                var updatedPropertyHtml = await _propertyRepository.UpdatePropertyHtmlByIdAsync(propertyHtml);
                return Ok(new PropertyHtmlResponseDto(updatedPropertyHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property HTML: {PropertyId}", dto.PropertyId);
                return ServerError("An error occurred while updating the property HTML");
            }
        }
    }
}

