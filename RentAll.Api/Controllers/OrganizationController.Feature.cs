
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get

        [HttpGet("feature")]
        public async Task<IActionResult> GetFeaturesByOfficeIdsAsync()
        {
            try
            {
                var features = await _organizationRepository.GetFeaturesByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
                var response = features.Select(f => new FeatureResponseDto(f));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all features");
                return ServerError("An error occurred while retrieving features");
            }
        }

        [HttpGet("feature/{featureId}")]
        public async Task<IActionResult> GetFeatureById(int featureId)
        {
            if (featureId <= 0)
                return BadRequest("Feature ID is required");

            try
            {
                var feature = await _organizationRepository.GetFeatureByIdAsync(featureId, CurrentOrganizationId);
                if (feature == null)
                    return NotFound("Feature not found");

                return Ok(new FeatureResponseDto(feature));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature by ID: {FeatureId}", featureId);
                return ServerError("An error occurred while retrieving the feature");
            }
        }

        #endregion

        #region Post

        [HttpPost("feature")]
        public async Task<IActionResult> CreateFeature([FromBody] FeatureCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Feature data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                if (await _organizationRepository.ExistsFeatureByOfficeAndFeatureTypeAsync(CurrentOrganizationId, dto.OfficeId, dto.FeatureTypeId))
                    return Conflict("Feature already exists for this office and feature type");

                var feature = dto.ToModel();
                var createdFeature = await _organizationRepository.CreateFeatureAsync(feature);

                var response = new FeatureResponseDto(createdFeature);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feature");
                return ServerError("An error occurred while creating the feature");
            }
        }

        #endregion

        #region Put

        [HttpPut("feature")]
        public async Task<IActionResult> UpdateFeature([FromBody] FeatureUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Feature data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingFeature = await _organizationRepository.GetFeatureByIdAsync(dto.FeatureId, CurrentOrganizationId);
                if (existingFeature == null)
                    return NotFound("Feature not found");

                if (existingFeature.OfficeId != dto.OfficeId || (int)existingFeature.FeatureTypeId != dto.FeatureTypeId)
                {
                    if (await _organizationRepository.ExistsFeatureByOfficeAndFeatureTypeAsync(CurrentOrganizationId, dto.OfficeId, dto.FeatureTypeId, dto.FeatureId))
                        return Conflict("Feature already exists for this office and feature type");
                }

                var feature = dto.ToModel();
                var updatedFeature = await _organizationRepository.UpdateFeatureByIdAsync(feature);
                return Ok(new FeatureResponseDto(updatedFeature));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feature: {FeatureId}", dto.FeatureId);
                return ServerError("An error occurred while updating the feature");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("feature/{featureId}")]
        public async Task<IActionResult> DeleteFeatureByIdAsync(int featureId)
        {
            if (featureId <= 0)
                return BadRequest("Feature ID is required");

            try
            {
                var feature = await _organizationRepository.GetFeatureByIdAsync(featureId, CurrentOrganizationId);
                if (feature == null)
                    return NotFound("Feature not found");

                await _organizationRepository.DeleteFeatureByIdAsync(featureId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feature: {FeatureId}", featureId);
                return ServerError("An error occurred while deleting the feature");
            }
        }

        #endregion

    }
}
