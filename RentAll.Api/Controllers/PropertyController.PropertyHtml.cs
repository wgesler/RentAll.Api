
namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {

        #region Get

        /// <summary>
        /// Get property HTML by Property ID
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>Property HTML</returns>
        [HttpGet("property-html/{propertyId}")]
        public async Task<IActionResult> GetPropertyHtmlByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var propertyHtml = await _propertyRepository.GetPropertyHtmlByPropertyIdAsync(propertyId, CurrentOrganizationId);
                if (propertyHtml == null)
                    return NotFound("Property HTML not found");

                return Ok(new PropertyHtmlResponseDto(propertyHtml));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property HTML by Property ID: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving the property HTML");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new property HTML
        /// </summary>
        /// <param name="dto">Property HTML data</param>
        /// <returns>Created property HTML</returns>
        [HttpPost("property-html")]
        public async Task<IActionResult> Create([FromBody] CreatePropertyHtmlDto dto)
        {
            if (dto == null)
                return BadRequest("Property HTML data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetByIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var propertyHtml = dto.ToModel(CurrentUser);
                var createdPropertyHtml = await _propertyRepository.CreatePropertyHtmlAsync(propertyHtml);

                var response = new PropertyHtmlResponseDto(createdPropertyHtml);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property HTML");
                return ServerError("An error occurred while creating the property HTML");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing property HTML
        /// </summary>
        /// <param name="dto">Property HTML data</param>
        /// <returns>Updated property HTML</returns>
        [HttpPut("property-html")]
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

        #endregion

        #region Delete

        /// <summary>
        /// Delete a property HTML
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>No content</returns>
        [HttpDelete("property-html/property/{propertyId}")]
        public async Task<IActionResult> DeletePropertyHtml(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                // Check if HTML exists
                var propertyHtml = await _propertyRepository.GetPropertyHtmlByPropertyIdAsync(propertyId, CurrentOrganizationId);
                if (propertyHtml == null)
                    return NotFound("Property HTML not found");

                await _propertyRepository.DeletePropertyHtmlByPropertyIdAsync(propertyId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property HTML: {PropertyId}", propertyId);
                return ServerError("An error occurred while deleting the property HTML");
            }
        }

        #endregion

    }
}
