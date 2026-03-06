using RentAll.Api.Dtos.Photos;

namespace RentAll.Api.Controllers
{
    public partial class DocumentController
    {
        #region Get
        [HttpGet("photo/{photoId:guid}")]
        public async Task<IActionResult> GetPhotoById(Guid photoId)
        {
            if (photoId == Guid.Empty)
                return BadRequest("Photo ID is required");

            var photo = await _photoRepository.GetByIdAsync(photoId, CurrentOrganizationId);
            if (photo == null)
                return NotFound();

            var response = new PhotoResponseDto(photo);

            if (!string.IsNullOrWhiteSpace(photo.PhotoPath))
                response.FileDetails = await _fileService.GetImageDetailsAsync(photo.OrganizationId, photo.OfficeName, photo.PhotoPath, ImageType.Photos);

            return Ok(response);
        }
        #endregion

        #region Post
        [HttpPost("photo")]
        public async Task<IActionResult> AddPhoto([FromBody] CreatePhotoDto dto)
        {
            if (dto == null)
                return BadRequest("Photo data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var model = dto.ToModel(CurrentOrganizationId, CurrentUser);
                var officeName = GetOfficeName(dto.OfficeId);

                // Handle photo file upload if provided
                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        var photoPath = await _fileService.SaveImageAsync(dto.OrganizationId, officeName, dto.FileDetails.File, dto.FileDetails.FileName, dto.FileDetails.ContentType, ImageType.Photos);
                        model.PhotoPath = photoPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving photo");
                        return ServerError("An error occurred while saving the photo file");
                    }
                }


                var created = await _photoRepository.CreateAsync(model);
                var response = new PhotoResponseDto(created);
                if (!string.IsNullOrWhiteSpace(created.PhotoPath))
                    response.FileDetails = await _fileService.GetImageDetailsAsync(created.OrganizationId, officeName, created.PhotoPath, ImageType.Photos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating photo");
                return ServerError("An error occurred while creating the photo");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("photo/{photoId:guid}")]
        public async Task<IActionResult> DeletePhotoById(Guid photoId)
        {
            if (photoId == Guid.Empty)
                return BadRequest("Photo ID is required");

            try
            {
                var photo = await _photoRepository.GetByIdAsync(photoId, CurrentOrganizationId);
                if (photo != null && photo.PhotoPath != null)
                    await _fileService.DeleteImageAsync(photo.OrganizationId, GetOfficeName(photo.OfficeId), photo.PhotoPath, ImageType.Photos);

                await _photoRepository.DeleteByIdAsync(photoId, CurrentOrganizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo: {PhotoId}", photoId);
                return ServerError("An error occurred while deleting the photo");
            }
        }
        #endregion
    }
}
