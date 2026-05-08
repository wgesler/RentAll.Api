namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {
        #region Get
        [HttpGet("tracker-response/property/{propertyId}")]
        public async Task<IActionResult> GetTrackerResponsesByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("PropertyId is required");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var responses = await _propertyRepository.GetTrackerResponsesByPropertyIdAsync(propertyId);
                var response = responses.Select(r => new PropertyTrackerResponseResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracker responses by PropertyId: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving tracker responses");
            }
        }

        [HttpGet("tracker-response/offices")]
        public async Task<IActionResult> GetTrackerResponsesByOfficeIds([FromQuery] bool includeInactive = false)
        {
            try
            {
                var responses = await _propertyRepository.GetTrackerResponsesByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess, includeInactive, true);
                var response = responses.Select(r => new PropertyTrackerResponseResponseDto(r));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracker responses by office access");
                return ServerError("An error occurred while retrieving tracker responses");
            }
        }

        [HttpGet("tracker-response-option/property/{propertyId}")]
        public async Task<IActionResult> GetTrackerResponseOptionsByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("PropertyId is required");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var options = await _propertyRepository.GetTrackerResponseOptionsByPropertyIdAsync(propertyId);
                var response = options.Select(o => new PropertyTrackerResponseOptionResponseDto(o));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracker response options by PropertyId: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving tracker response options");
            }
        }

        [HttpGet("tracker-response-option/offices")]
        public async Task<IActionResult> GetTrackerResponseOptionsByOfficeIds([FromQuery] bool includeInactive = false)
        {
            try
            {
                var options = await _propertyRepository.GetTrackerResponseOptionsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess, includeInactive, true);
                var response = options.Select(o => new PropertyTrackerResponseOptionResponseDto(o));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracker response options by office access");
                return ServerError("An error occurred while retrieving tracker response options");
            }
        }
        #endregion

        #region Post
        [HttpPost("tracker-response")]
        public async Task<IActionResult> CreateTrackerResponse([FromBody] PropertyTrackerResponseCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker response data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var trackerResponse = dto.ToModel(CurrentUser);
                var created = await _propertyRepository.CreateTrackerResponseAsync(trackerResponse);
                return Ok(new PropertyTrackerResponseResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tracker response for PropertyId: {PropertyId}", dto.PropertyId);
                return ServerError("An error occurred while creating tracker response");
            }
        }

        [HttpPost("tracker-response-option")]
        public async Task<IActionResult> CreateTrackerResponseOption([FromBody] PropertyTrackerResponseOptionCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker response option data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingResponse = await _propertyRepository.GetTrackerResponseByIdAsync(dto.TrackerResponseId);
                if (existingResponse == null)
                    return NotFound("Tracker response not found");

                var property = await _propertyRepository.GetPropertyByIdAsync(existingResponse.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var trackerResponseOption = dto.ToModel(CurrentUser);
                var created = await _propertyRepository.CreateTrackerResponseOptionAsync(trackerResponseOption);
                return Ok(new PropertyTrackerResponseOptionResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tracker response option for TrackerResponseId: {TrackerResponseId}", dto.TrackerResponseId);
                return ServerError("An error occurred while creating tracker response option");
            }
        }
        #endregion

        #region Put
        [HttpPut("tracker-response")]
        public async Task<IActionResult> UpdateTrackerResponse([FromBody] PropertyTrackerResponseUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker response data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var existing = await _propertyRepository.GetTrackerResponseByIdAsync(dto.TrackerResponseId);
                if (existing == null)
                    return NotFound("Tracker response not found");

                var trackerResponse = dto.ToModel(CurrentUser);
                var updated = await _propertyRepository.UpdateTrackerResponseByIdAsync(trackerResponse);
                return Ok(new PropertyTrackerResponseResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracker response: {TrackerResponseId}", dto.TrackerResponseId);
                return ServerError("An error occurred while updating tracker response");
            }
        }

        [HttpPut("tracker-response-option")]
        public async Task<IActionResult> UpdateTrackerResponseOption([FromBody] PropertyTrackerResponseOptionUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker response option data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingResponse = await _propertyRepository.GetTrackerResponseByIdAsync(dto.TrackerResponseId);
                if (existingResponse == null)
                    return NotFound("Tracker response not found");

                var property = await _propertyRepository.GetPropertyByIdAsync(existingResponse.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var updated = await _propertyRepository.UpdateTrackerResponseOptionByIdAsync(dto.TrackerResponseId, dto.TrackerDefinitionOptionId, dto.NewTrackerDefinitionOptionId);
                return Ok(new PropertyTrackerResponseOptionResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracker response option for TrackerResponseId: {TrackerResponseId}", dto.TrackerResponseId);
                return ServerError("An error occurred while updating tracker response option");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("tracker-response/property/{propertyId:guid}")]
        public async Task<IActionResult> DeleteTrackerResponsesByPropertyId(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("PropertyId is required");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                await _propertyRepository.DeleteTrackerResponsesByPropertyIdAsync(propertyId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker responses by PropertyId: {PropertyId}", propertyId);
                return ServerError("An error occurred while deleting tracker responses");
            }
        }

        [HttpDelete("tracker-response/{trackerResponseId:guid}")]
        public async Task<IActionResult> DeleteTrackerResponseById(Guid trackerResponseId)
        {
            if (trackerResponseId == Guid.Empty)
                return BadRequest("TrackerResponseId is required");

            try
            {
                var existing = await _propertyRepository.GetTrackerResponseByIdAsync(trackerResponseId);
                if (existing == null)
                    return NotFound("Tracker response not found");

                var property = await _propertyRepository.GetPropertyByIdAsync(existing.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                await _propertyRepository.DeleteTrackerResponseByIdAsync(trackerResponseId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker response: {TrackerResponseId}", trackerResponseId);
                return ServerError("An error occurred while deleting tracker response");
            }
        }

        [HttpDelete("tracker-response-option/{trackerResponseId:guid}/{trackerDefinitionOptionId:guid}")]
        public async Task<IActionResult> DeleteTrackerResponseOptionById(Guid trackerResponseId, Guid trackerDefinitionOptionId)
        {
            if (trackerResponseId == Guid.Empty)
                return BadRequest("TrackerResponseId is required");

            if (trackerDefinitionOptionId == Guid.Empty)
                return BadRequest("TrackerDefinitionOptionId is required");

            try
            {
                var existingResponse = await _propertyRepository.GetTrackerResponseByIdAsync(trackerResponseId);
                if (existingResponse == null)
                    return NotFound("Tracker response not found");

                var property = await _propertyRepository.GetPropertyByIdAsync(existingResponse.PropertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                await _propertyRepository.DeleteTrackerResponseOptionByIdAsync(trackerResponseId, trackerDefinitionOptionId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker response option for TrackerResponseId: {TrackerResponseId}", trackerResponseId);
                return ServerError("An error occurred while deleting tracker response option");
            }
        }
        #endregion
    }
}
