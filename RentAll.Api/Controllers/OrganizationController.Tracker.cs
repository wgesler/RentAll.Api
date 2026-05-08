namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        #region Get
        [HttpGet("tracker-configuration")]
        public async Task<IActionResult> GetTrackerConfiguration([FromQuery] bool includeInactive = false)
        {
            try
            {
                var contexts = await _organizationRepository.GetTrackerContextsAsync();
                var definitions = await _organizationRepository.GetTrackerDefinitionsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess, null, includeInactive);
                var options = await _organizationRepository.GetTrackerDefinitionOptionsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess, null, includeInactive);

                var optionLookup = options
                    .GroupBy(option => option.TrackerDefinitionId)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .OrderBy(option => option.OptionSortOrder)
                            .ThenBy(option => option.Label)
                            .Select(option => new TrackerDefinitionOptionResponseDto(option))
                            .ToList());

                var definitionLookup = definitions
                    .GroupBy(definition => definition.TrackerContextId)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .OrderBy(definition => definition.OfficeName)
                            .ThenBy(definition => definition.SortOrder)
                            .ThenBy(definition => definition.DisplayName)
                            .Select(definition => new TrackerConfigurationDefinitionResponseDto
                            {
                                TrackerDefinitionId = definition.TrackerDefinitionId,
                                OrganizationId = definition.OrganizationId,
                                OfficeId = definition.OfficeId,
                                OfficeName = definition.OfficeName,
                                TrackerContextId = (int)definition.TrackerContextId,
                                TrackerContextCode = definition.TrackerContextCode,
                                DisplayName = definition.DisplayName,
                                Description = definition.Description,
                                SortOrder = definition.SortOrder,
                                IsActive = definition.IsActive,
                                CreatedOn = definition.CreatedOn,
                                CreatedBy = definition.CreatedBy,
                                ModifiedOn = definition.ModifiedOn,
                                ModifiedBy = definition.ModifiedBy,
                                Options = optionLookup.TryGetValue(definition.TrackerDefinitionId, out var definitionOptions)
                                    ? definitionOptions
                                    : Enumerable.Empty<TrackerDefinitionOptionResponseDto>()
                            })
                            .ToList());

                var response = new TrackerConfigurationResponseDto
                {
                    Contexts = contexts
                        .OrderBy(context => (int)context.TrackerContextId)
                        .Select(context => new TrackerConfigurationContextResponseDto
                        {
                            TrackerContextId = (int)context.TrackerContextId,
                            Code = context.Code,
                            Description = context.Description,
                            IsActive = context.IsActive,
                            Definitions = definitionLookup.TryGetValue(context.TrackerContextId, out var contextDefinitions)
                                ? contextDefinitions
                                : Enumerable.Empty<TrackerConfigurationDefinitionResponseDto>()
                        })
                        .ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracker configuration");
                return ServerError("An error occurred while retrieving tracker configuration");
            }
        }
        #endregion

        #region Post
        [HttpPost("tracker-context")]
        public async Task<IActionResult> CreateTrackerContext([FromBody] TrackerContextCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker context data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var trackerContext = dto.ToModel();
                var created = await _organizationRepository.CreateTrackerContextAsync(trackerContext);
                return Ok(new TrackerContextResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tracker context");
                return ServerError("An error occurred while creating tracker context");
            }
        }

        [HttpPost("tracker-definition")]
        public async Task<IActionResult> CreateTrackerDefinition([FromBody] TrackerDefinitionCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker definition data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var tracker = dto.ToModel(CurrentUser);
                var created = await _organizationRepository.CreateTrackerDefinitionAsync(tracker);
                return Ok(new TrackerDefinitionResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tracker definition");
                return ServerError("An error occurred while creating tracker definition");
            }
        }

        [HttpPost("tracker-definition-option")]
        public async Task<IActionResult> CreateTrackerDefinitionOption([FromBody] TrackerDefinitionOptionCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker definition option data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var option = dto.ToModel();
                var created = await _organizationRepository.CreateTrackerDefinitionOptionAsync(option);
                return Ok(new TrackerDefinitionOptionResponseDto(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tracker definition option");
                return ServerError("An error occurred while creating tracker definition option");
            }
        }

        [HttpPost("tracker-definition/office/{targetOfficeId:int}/copy/{sourceOfficeId:int}")]
        public async Task<IActionResult> CopyTrackerDefinitionsByOfficeId(int targetOfficeId, int sourceOfficeId)
        {
            if (targetOfficeId <= 0 || sourceOfficeId <= 0)
                return BadRequest("SourceOfficeId and TargetOfficeId are required");

            if (targetOfficeId == sourceOfficeId)
                return BadRequest("SourceOfficeId and TargetOfficeId must be different");

            try
            {
                await _organizationRepository.CopyTrackerDefinitionsByOfficeIdAsync(CurrentOrganizationId, sourceOfficeId, targetOfficeId, CurrentUser);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying tracker definitions from office {SourceOfficeId} to office {TargetOfficeId}", sourceOfficeId, targetOfficeId);
                return ServerError("An error occurred while copying tracker definitions");
            }
        }
        #endregion

        #region Put
        [HttpPut("tracker-context")]
        public async Task<IActionResult> UpdateTrackerContext([FromBody] TrackerContextUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker context data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _organizationRepository.GetTrackerContextByIdAsync(dto.TrackerContextId);
                if (existing == null)
                    return NotFound("Tracker context not found");

                var trackerContext = dto.ToModel();
                var updated = await _organizationRepository.UpdateTrackerContextByIdAsync(trackerContext);
                return Ok(new TrackerContextResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracker context: {TrackerContextId}", dto.TrackerContextId);
                return ServerError("An error occurred while updating tracker context");
            }
        }

        [HttpPut("tracker-definition")]
        public async Task<IActionResult> UpdateTrackerDefinition([FromBody] TrackerDefinitionUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker definition data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _organizationRepository.GetTrackerDefinitionByIdAsync(dto.TrackerDefinitionId, CurrentOrganizationId);
                if (existing == null)
                    return NotFound("Tracker definition not found");

                var tracker = dto.ToModel(CurrentUser);
                var updated = await _organizationRepository.UpdateTrackerDefinitionByIdAsync(tracker);
                return Ok(new TrackerDefinitionResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracker definition: {TrackerDefinitionId}", dto.TrackerDefinitionId);
                return ServerError("An error occurred while updating tracker definition");
            }
        }

        [HttpPut("tracker-definition-option")]
        public async Task<IActionResult> UpdateTrackerDefinitionOption([FromBody] TrackerDefinitionOptionUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Tracker definition option data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _organizationRepository.GetTrackerDefinitionOptionByIdAsync(dto.TrackerDefinitionOptionId);
                if (existing == null)
                    return NotFound("Tracker definition option not found");

                var option = dto.ToModel();
                var updated = await _organizationRepository.UpdateTrackerDefinitionOptionByIdAsync(option);
                return Ok(new TrackerDefinitionOptionResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tracker definition option: {TrackerDefinitionOptionId}", dto.TrackerDefinitionOptionId);
                return ServerError("An error occurred while updating tracker definition option");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("tracker-context/{trackerContextId}")]
        public async Task<IActionResult> DeleteTrackerContextById(int trackerContextId)
        {
            if (trackerContextId <= 0)
                return BadRequest("TrackerContextId is required");

            try
            {
                await _organizationRepository.DeleteTrackerContextByIdAsync(trackerContextId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker context: {TrackerContextId}", trackerContextId);
                return ServerError("An error occurred while deleting tracker context");
            }
        }

        [HttpDelete("tracker-definition/{trackerDefinitionId:guid}")]
        public async Task<IActionResult> DeleteTrackerDefinitionById(Guid trackerDefinitionId)
        {
            if (trackerDefinitionId == Guid.Empty)
                return BadRequest("TrackerDefinitionId is required");

            try
            {
                await _organizationRepository.DeleteTrackerDefinitionByIdAsync(trackerDefinitionId, CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker definition: {TrackerDefinitionId}", trackerDefinitionId);
                return ServerError("An error occurred while deleting tracker definition");
            }
        }

        [HttpDelete("tracker-definition/office/{officeId:int}")]
        public async Task<IActionResult> DeleteTrackerDefinitionsByOfficeId(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("OfficeId is required");

            try
            {
                await _organizationRepository.DeleteTrackerDefinitionsByOfficeIdAsync(CurrentOrganizationId, officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker definitions for office: {OfficeId}", officeId);
                return ServerError("An error occurred while deleting tracker definitions for office");
            }
        }

        [HttpDelete("tracker-definition-option/{trackerDefinitionOptionId:guid}")]
        public async Task<IActionResult> DeleteTrackerDefinitionOptionById(Guid trackerDefinitionOptionId)
        {
            if (trackerDefinitionOptionId == Guid.Empty)
                return BadRequest("TrackerDefinitionOptionId is required");

            try
            {
                await _organizationRepository.DeleteTrackerDefinitionOptionByIdAsync(trackerDefinitionOptionId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tracker definition option: {TrackerDefinitionOptionId}", trackerDefinitionOptionId);
                return ServerError("An error occurred while deleting tracker definition option");
            }
        }
        #endregion
    }
}
