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
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(photo.OrganizationId, photo.OfficeName, photo.PhotoPath, ImageType.Photos);

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
                var officeName = await GetOfficeNameAsync(dto.OfficeId);

                model.PhotoPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, officeName, dto.FileDetails, ImageType.Photos);

                var created = await _photoRepository.CreateAsync(model);
                var response = new PhotoResponseDto(created);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(created.OrganizationId, officeName, created.PhotoPath, ImageType.Photos);

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
                    await _fileService.DeleteImageAsync(photo.OrganizationId, await GetOfficeNameAsync(photo.OfficeId), photo.PhotoPath, ImageType.Photos);

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
