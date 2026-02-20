using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Properties;

namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {
        /// <summary>
        /// Create a new property
        /// </summary>
        /// <param name="dto">Property data</param>
        /// <returns>Created property</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePropertyDto dto)
        {
            if (dto == null)
                return BadRequest("Property data is required");

            if (!ModelState.IsValid)
                return BadRequest("Invalid request data");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if PropertyCode already exists
                if (await _propertyRepository.ExistsByPropertyCodeAsync(dto.PropertyCode, CurrentOrganizationId))
                    return Conflict("Property Code already exists");

                var property = dto.ToModel(CurrentUser);
                var createdProperty = await _propertyRepository.CreateAsync(property);
                return CreatedAtAction(nameof(GetById), new { id = createdProperty.PropertyId }, new PropertyResponseDto(createdProperty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                return ServerError("An error occurred while creating the property");
            }
        }
    }
}
