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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyDto dto)
        {
            if (dto == null)
                return BadRequest("Property data is required");

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if property exists
                var existingProperty = await _propertyRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingProperty == null)
                    return NotFound("Property not found");

                // Check if PropertyCode is being changed and if the new code already exists
                if (existingProperty.PropertyCode != dto.PropertyCode)
                {
                    if (await _propertyRepository.ExistsByPropertyCodeAsync(dto.PropertyCode, CurrentOrganizationId))
                        return Conflict("Property Code already exists");
                }

                var property = dto.ToModel(CurrentUser);
                var updatedProperty = await _propertyRepository.UpdateByIdAsync(property);
                return Ok(new PropertyResponseDto(updatedProperty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property: {PropertyId}", id);
                return ServerError("An error occurred while updating the property");
            }
        }

		/// <summary>
		/// Upsert the current user's property selection
		/// </summary>
		/// <param name="dto">Property selection data</param>
		/// <returns>Updated property selection</returns>
		[HttpPut("selection")]
		public async Task<IActionResult> PutPropertySelection([FromBody] UpsertPropertySelectionDto dto)
		{
			if (dto == null)
				return BadRequest("Property selection data is required");

			var (isValid, errorMessage) = dto.IsValid(CurrentUser);
			if (!isValid)
				return BadRequest(errorMessage ?? "Invalid request data");

			try
			{
				var selection = dto.ToModel();
				var updatedSelection = await _propertySelectionRepository.UpsertAsync(selection);
				return Ok(new PropertySelectionResponseDto(updatedSelection));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error upserting property selection for user: {UserId}", CurrentUser);
				return ServerError("An error occurred while saving the property selection");
			}
		}
    }
}