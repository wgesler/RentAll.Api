
namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {

        #region Get

        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            try
            {
                // Get the property summary for the list of properties
                var list = await _propertyRepository.GetPropertyListByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = list.Select(p => new PropertyListResponseDto(p));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties list");
                return ServerError("An error occurred while retrieving properties list");
            }
        }

        [HttpGet("active-list")]
        public async Task<IActionResult> GetActiveList()
        {
            try
            {
                // Get the property summary for the list of properties
                var list = await _propertyRepository.GetPropertyActiveListByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = list.Select(p => new PropertyListResponseDto(p));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties list");
                return ServerError("An error occurred while retrieving properties list");
            }
        }


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPropertiesByUserSelection(Guid userId)
        {
            if (CurrentUser == Guid.Empty || CurrentUser != userId)
                return Unauthorized();

            try
            {
                var properties = await _propertyRepository.GetPropertyListBySelectionCriteriaAsync(CurrentUser, CurrentOrganizationId, CurrentOfficeAccess);
                var response = properties.Select(p => new PropertyListResponseDto(p));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties by selection criteria for user: {UserId}", CurrentUser);
                return ServerError("An error occurred while retrieving properties");
            }
        }

        [HttpGet("user/{userId}/active")]
        public async Task<IActionResult> GetActivePropertiesByUserSelection(Guid userId)
        {
            if (CurrentUser == Guid.Empty || CurrentUser != userId)
                return Unauthorized();

            try
            {
                var properties = await _propertyRepository.GetActivePropertyListBySelectionCriteriaAsync(CurrentUser, CurrentOrganizationId, CurrentOfficeAccess);
                var response = properties.Select(p => new PropertyListResponseDto(p));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties by selection criteria for user: {UserId}", CurrentUser);
                return ServerError("An error occurred while retrieving properties");
            }
        }

        [HttpGet("owner/{ownerId}")]
        public async Task<IActionResult> GetPropertiesByOwnerId(Guid ownerId)
        {
            if (CurrentUser == Guid.Empty || CurrentUser != ownerId)
                return Unauthorized();

            try
            {
                var user = await _userRepository.GetUserByIdAsync(CurrentUser);
                if (user == null)
                    return NotFound("User not found");

                var contact = await _contactRepository.GetContactByEmailAsync(user.Email, CurrentOrganizationId);
                if (contact == null)
                    return NotFound("Owner not found");

                var properties = await _propertyRepository.GetPropertyListByOwnerIdAsync(contact.ContactId, CurrentOrganizationId, CurrentOfficeAccess);
                var response = properties.Select(p => new PropertyListResponseDto(p));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties by selection criteria for user: {UserId}", CurrentUser);
                return ServerError("An error occurred while retrieving properties");
            }
        }

        [HttpGet("{propertyId}")]
        public async Task<IActionResult> GetById(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
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

        [HttpGet("{propertyId}/calendar/subscription-url")]
        public IActionResult GetCalendarSubscriptionUrl(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var configuredBaseUrl = _appSettings.PublicApiBaseUrl;
                var fallbackBaseUrl = $"{Request.Scheme}://{Request.Host}";

                var baseUrl = !string.IsNullOrWhiteSpace(configuredBaseUrl)
                    ? configuredBaseUrl.Trim().TrimEnd('/') : fallbackBaseUrl;

                var subscriptionUrl = _calendarManager.GeneratePropertyCalendarSubscriptionUrl(propertyId, CurrentOrganizationId, baseUrl);
                return Ok(new { configuredBaseUrl, fallbackBaseUrl, finalBaseUrl = baseUrl, subscriptionUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar subscription URL for property: {PropertyId}", propertyId);
                return ServerError("An error occurred while creating calendar subscription URL");
            }
        }
        #endregion

        #region Post

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

                var response = new PropertyResponseDto(createdProperty);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                return ServerError("An error occurred while creating the property");
            }
        }

        #endregion

        #region Put

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
                var existingProperty = await _propertyRepository.GetPropertyByIdAsync(dto.PropertyId, CurrentOrganizationId);
                if (existingProperty == null)
                    return NotFound("Property not found");

                // Check if PropertyCode is being changed and if the new code already exists
                if (existingProperty.PropertyCode != dto.PropertyCode)
                {
                    if (await _propertyRepository.ExistsByPropertyCodeAsync(dto.PropertyCode, CurrentOrganizationId))
                        return Conflict("Property Code already exists");
                }

                // Check if PropertyOffice is being changed and update associated owners if necessary
                var property = dto.ToModel(CurrentUser);
                if (existingProperty.OfficeId != dto.OfficeId)
                    await _propertyManager.UpdatePropertyOfficeAsync(property, CurrentUser);

                var updatedProperty = await _propertyRepository.UpdateByIdAsync(property);
                return Ok(new PropertyResponseDto(updatedProperty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property: {PropertyId}", dto.PropertyId);
                return ServerError("An error occurred while updating the property");
            }
        }

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

        [HttpDelete("{propertyId}")]
        public async Task<IActionResult> DeletePropertyByIdAsync(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                await _propertyRepository.DeletePropertyByIdAsync(propertyId);
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
