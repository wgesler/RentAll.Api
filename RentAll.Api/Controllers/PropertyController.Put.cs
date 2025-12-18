using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Properties;

namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {
        /// <summary>
        /// Update an existing property
        /// </summary>
        /// <param name="id">Property ID</param>
        /// <param name="dto">Property data</param>
        /// <returns>Updated property</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PropertyUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Property data is required" });

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
                // Check if property exists
                var existingProperty = await _propertyRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingProperty == null)
                    return NotFound(new { message = "Property not found" });

                // Check if PropertyCode is being changed and if the new code already exists
                if (existingProperty.PropertyCode != dto.PropertyCode)
                {
                    if (await _propertyRepository.ExistsByPropertyCodeAsync(dto.PropertyCode, CurrentOrganizationId))
                        return Conflict(new { message = "Property Code already exists" });
                }

                var property = dto.ToModel(CurrentUser);
                var updatedProperty = await _propertyRepository.UpdateByIdAsync(property);
                return Ok(new PropertyResponseDto(updatedProperty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property: {PropertyId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the property" });
            }
        }
    }
}