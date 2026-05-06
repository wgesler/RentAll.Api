namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {

        #region Get

        [HttpGet("property-information/{propertyId}")]
        public async Task<IActionResult> GetPropertyInformationByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var propertyInformation = await _propertyRepository.GetPropertyInformationByPropertyIdAsync(propertyId, CurrentOrganizationId);
                if (propertyInformation == null)
                    return Ok(); // Not required

                return Ok(new PropertyInformationResponseDto(propertyInformation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property information by Property ID: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving the property information");
            }
        }

        #endregion

        #region Post

        [HttpPost("property-information")]
        public async Task<IActionResult> Create([FromBody] CreatePropertyInformationDto dto)
        {
            if (dto == null)
                return BadRequest("Property information data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetPropertyByIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var propertyInformation = dto.ToModel(CurrentUser);
                var createdPropertyInformation = await _propertyRepository.CreatePropertyInformationAsync(propertyInformation);

                var response = new PropertyInformationResponseDto(createdPropertyInformation);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property information");
                return ServerError("An error occurred while creating the property information");
            }
        }

        #endregion

        #region Put

        [HttpPut("property-information")]
        public async Task<IActionResult> Update([FromBody] UpdatePropertyInformationDto dto)
        {
            if (dto == null)
                return BadRequest("Property information data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var propertyInformation = dto.ToModel(CurrentUser);

                // Check if property information exists
                var existing = await _propertyRepository.GetPropertyInformationByPropertyIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (existing == null)
                {
                    var addPropertyInformation = await _propertyRepository.CreatePropertyInformationAsync(propertyInformation);
                    return Ok(new PropertyInformationResponseDto(addPropertyInformation));
                }
                else
                {
                    var updatedPropertyInformation = await _propertyRepository.UpdatePropertyInformationByIdAsync(propertyInformation);
                    return Ok(new PropertyInformationResponseDto(updatedPropertyInformation));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property information: {PropertyId}", dto.PropertyId);
                return ServerError("An error occurred while updating the property information");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("property-information/property/{propertyId}")]
        public async Task<IActionResult> DeletePropertyInformationByPropertyIdAsync(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Verify property belongs to organization
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                // Check if property information exists
                var propertyInformation = await _propertyRepository.GetPropertyInformationByPropertyIdAsync(propertyId, CurrentOrganizationId);
                if (propertyInformation == null)
                    return NotFound("Property information not found");

                await _propertyRepository.DeletePropertyInformationByPropertyIdAsync(propertyId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property information: {PropertyId}", propertyId);
                return ServerError("An error occurred while deleting the property information");
            }
        }

        #endregion

    }
}
