
namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {

        #region Get

        /// <summary>
        /// Get all properties list
        /// </summary>
        /// <returns>List of properties</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            try
            {
                // Get the property summary for the list of properties
                var list = await _propertyRepository.GetListByOfficeIdAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = list.Select(p => new PropertyListResponseDto(p));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties list");
                return ServerError("An error occurred while retrieving properties list");
            }
        }


        /// <summary>
        /// Get properties by the current user's selection criteria
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <returns>List of properties by user selection</returns>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPropertiesByUserSelection(Guid userId)
        {
            if (CurrentUser == Guid.Empty || CurrentUser != userId)
                return Unauthorized();

            try
            {
                var properties = await _propertyRepository.GetListBySelectionCriteriaAsync(CurrentUser, CurrentOrganizationId, CurrentOfficeAccess);
                var response = properties.Select(p => new PropertyListResponseDto(p));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties by selection criteria for user: {UserId}", CurrentUser);
                return ServerError("An error occurred while retrieving properties");
            }
        }

        /// <summary>
        /// Get iCal subscription URL for a property.
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>Tokenized iCal subscription URL</returns>
        [HttpGet("{propertyId}/calendar/subscription-url")]
        public IActionResult GetCalendarSubscriptionUrl(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var subscriptionUrl = _calendarManager.GeneratePropertyCalendarSubscriptionUrl(propertyId, CurrentOrganizationId, baseUrl);

                return Ok(new CalendarUrlResponseDto { PropertyId = propertyId, OrganizationId = CurrentOrganizationId, SubscriptionUrl = subscriptionUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar subscription URL for property: {PropertyId}", propertyId);
                return ServerError("An error occurred while creating calendar subscription URL");
            }
        }

        /// <summary>
        /// Get property by ID
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>Property</returns>
        [HttpGet("{propertyId}")]
        public async Task<IActionResult> GetById(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                return Ok(new PropertyResponseDto(property));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property by ID: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving the property");
            }
        }

        /// <summary>
        /// Get property by PropertyCode
        /// </summary>
        /// <param name="propertyCode">Property Code</param>
        /// <returns>Property</returns>
        [HttpGet("code/{propertyCode}")]
        public async Task<IActionResult> GetByPropertyCode(string propertyCode)
        {
            if (string.IsNullOrWhiteSpace(propertyCode))
                return BadRequest("Property Code is required");

            try
            {
                var property = await _propertyRepository.GetByPropertyCodeAsync(propertyCode, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                return Ok(new PropertyResponseDto(property));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property by PropertyCode: {PropertyCode}", propertyCode);
                return ServerError("An error occurred while retrieving the property");
            }
        }

        /// <summary>
        /// Get the current user's property selection
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <returns>Property selection</returns>
        [HttpGet("selection/{userId}")]
        public async Task<IActionResult> GetPropertySelection(Guid userId)
        {
            if (CurrentUser == Guid.Empty || CurrentUser != userId)
                return Unauthorized();

            try
            {
                var selection = await _propertyRepository.GetPropertySelectionByUserIdAsync(CurrentUser);
                if (selection == null)
                    return Ok();

                return Ok(new PropertySelectionResponseDto(selection));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property selection for user: {UserId}", CurrentUser);
                return ServerError("An error occurred while retrieving the property selection");
            }
        }

        #endregion

        #region Post

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
                return CreatedAtAction(nameof(GetById), new { propertyId = createdProperty.PropertyId }, new PropertyResponseDto(createdProperty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                return ServerError("An error occurred while creating the property");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing property
        /// </summary>
        /// <param name="dto">Property data</param>
        /// <returns>Updated property</returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdatePropertyDto dto)
        {
            if (dto == null)
                return BadRequest("Property data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if property exists
                var existingProperty = await _propertyRepository.GetByIdAsync(dto.PropertyId, CurrentOrganizationId);
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
                _logger.LogError(ex, "Error updating property: {PropertyId}", dto.PropertyId);
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
                var updatedSelection = await _propertyRepository.UpsertPropertySelectionAsync(selection);
                return Ok(new PropertySelectionResponseDto(updatedSelection));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting property selection for user: {UserId}", CurrentUser);
                return ServerError("An error occurred while saving the property selection");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a property
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{propertyId}")]
        public async Task<IActionResult> Delete(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                // Check if property exists
                var property = await _propertyRepository.GetByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                await _propertyRepository.DeleteByIdAsync(propertyId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property: {PropertyId}", propertyId);
                return ServerError("An error occurred while deleting the property");
            }
        }

        #endregion

    }
}
