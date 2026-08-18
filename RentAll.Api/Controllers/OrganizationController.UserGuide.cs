namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        #region Get

        [HttpGet("user-guide")]
        public async Task<IActionResult> GetUserGuide()
        {
            try
            {
                var userGuide = await _organizationRepository.GetUserGuideAsync();
                if (userGuide == null)
                    return NotFound("UserGuide not found");

                return Ok(new UserGuideResponseDto(userGuide));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user guide");
                return ServerError("An error occurred while retrieving the user guide");
            }
        }

        #endregion

        #region Put

        [HttpPut("user-guide")]
        public async Task<IActionResult> UpdateUserGuide([FromBody] UpdateUserGuideDto dto)
        {
            if (!IsSuperAdmin())
                return Unauthorized("NoAccess");

            if (dto == null)
                return BadRequest("UserGuide data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var updated = await _organizationRepository.UpsertUserGuideAsync(dto.ToModel(), CurrentUser);
                return Ok(new UserGuideResponseDto(updated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user guide");
                return ServerError("An error occurred while updating the user guide");
            }
        }

        #endregion
    }
}
