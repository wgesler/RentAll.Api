namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {
        private static readonly Guid SystemUserGuideOrganizationId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        #region Get

        [HttpGet("user-guide")]
        public async Task<IActionResult> GetUserGuide()
        {
            try
            {
                var userGuide = await _organizationRepository.GetUserGuideAsync();
                if (userGuide == null)
                    return NotFound("UserGuide not found");

                var hydrated = await _userGuideHtmlImageService.HydrateForResponseAsync(userGuide);
                return Ok(new UserGuideResponseDto(hydrated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user guide");
                return ServerError("An error occurred while retrieving the user guide");
            }
        }

        #endregion

        #region Post

        [HttpGet("user-guide/image")]
        public async Task<IActionResult> GetUserGuideImage([FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Image path is required");

            if (!path.Contains("/userguide/", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid user guide image path");

            try
            {
                var fileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    SystemUserGuideOrganizationId,
                    null,
                    path,
                    ImageType.UserGuide);

                if (fileDetails == null || string.IsNullOrWhiteSpace(fileDetails.File))
                    return NotFound("User guide image not found");

                var bytes = Convert.FromBase64String(fileDetails.File);
                return File(bytes, string.IsNullOrWhiteSpace(fileDetails.ContentType) ? "image/jpeg" : fileDetails.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user guide image");
                return ServerError("An error occurred while loading the user guide image");
            }
        }

        [HttpPost("user-guide/image")]
        public async Task<IActionResult> UploadUserGuideImage([FromBody] UploadUserGuideImageDto dto)
        {
            if (!IsSuperAdmin())
                return Unauthorized("NoAccess");

            if (dto == null)
                return BadRequest("Image data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var imagePath = await _fileAttachmentHelper.SaveImageIfPresentAsync(
                    SystemUserGuideOrganizationId,
                    null,
                    dto.FileDetails,
                    ImageType.UserGuide);

                if (string.IsNullOrWhiteSpace(imagePath))
                    return BadRequest("Unable to save user guide image");

                return Ok(new UserGuideImageUploadResponseDto { ImagePath = imagePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading user guide image");
                return ServerError("An error occurred while uploading the user guide image");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("user-guide/image")]
        public async Task<IActionResult> DeleteUserGuideImage([FromQuery] string path)
        {
            if (!IsSuperAdmin())
                return Unauthorized("NoAccess");

            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("Image path is required");

            if (!path.Contains("/userguide/", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid user guide image path");

            try
            {
                await _fileService.DeleteImageAsync(
                    SystemUserGuideOrganizationId,
                    null,
                    path,
                    ImageType.UserGuide);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user guide image");
                return ServerError("An error occurred while deleting the user guide image");
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
                var hydrated = await _userGuideHtmlImageService.HydrateForResponseAsync(updated);
                return Ok(new UserGuideResponseDto(hydrated));
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
