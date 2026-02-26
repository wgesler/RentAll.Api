
namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {

        #region Get

        /// <summary>
        /// Get property letter by Property ID
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>Property letter</returns>
        [HttpGet("property-letter/{propertyId}")]
        public async Task<IActionResult> GetPropertyLetterByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var propertyLetter = await _propertyRepository.GetPropertyLetterByPropertyIdAsync(propertyId, CurrentOrganizationId);
                if (propertyLetter == null)
                    return Ok(); // Not required

                return Ok(new PropertyLetterResponseDto(propertyLetter));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property letter by Property ID: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving the property letter");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new property letter
        /// </summary>
        /// <param name="dto">Property letter data</param>
        /// <returns>Created property letter</returns>
        [HttpPost("property-letter")]
        public async Task<IActionResult> Create([FromBody] CreatePropertyLetterDto dto)
        {
            if (dto == null)
                return BadRequest("Property letter data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetByIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var propertyLetter = dto.ToModel(CurrentUser);
                var createdPropertyLetter = await _propertyRepository.CreatePropertyLetterAsync(propertyLetter);

                var response = new PropertyLetterResponseDto(createdPropertyLetter);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property letter");
                return ServerError("An error occurred while creating the property letter");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing property letter
        /// </summary>
        /// <param name="dto">Property letter data</param>
        /// <returns>Updated property letter</returns>
        [HttpPut("property-letter")]
        public async Task<IActionResult> Update([FromBody] UpdatePropertyLetterDto dto)
        {
            if (dto == null)
                return BadRequest("Property letter data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var propertyLetter = dto.ToModel(CurrentUser);

                // Check if property letter exists
                var existing = await _propertyRepository.GetPropertyLetterByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (existing == null)
                {
                    var addPropertyLetter = await _propertyRepository.CreatePropertyLetterAsync(propertyLetter);
                    return Ok(new PropertyLetterResponseDto(addPropertyLetter));
                }
                else
                {
                    var updatedPropertyLetter = await _propertyRepository.UpdatePropertyLetterByIdAsync(propertyLetter);
                    return Ok(new PropertyLetterResponseDto(updatedPropertyLetter));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property letter: {PropertyId}", dto.PropertyId);
                return ServerError("An error occurred while updating the property letter");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a property letter
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>No content</returns>
        [HttpDelete("property-letter/property/{propertyId}")]
        public async Task<IActionResult> DeletePropertyLetter(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                // Check if property letter exists
                var propertyLetter = await _propertyRepository.GetPropertyLetterByPropertyIdAsync(propertyId, CurrentOrganizationId);
                if (propertyLetter == null)
                    return NotFound("Property letter not found");

                await _propertyRepository.DeletePropertyLetterByPropertyIdAsync(propertyId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property letter: {PropertyId}", propertyId);
                return ServerError("An error occurred while deleting the property letter");
            }
        }

        #endregion

    }
}
