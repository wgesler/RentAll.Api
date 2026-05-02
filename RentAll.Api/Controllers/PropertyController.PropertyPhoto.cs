using RentAll.Api.Dtos.Properties.PropertyPhotos;

namespace RentAll.Api.Controllers
{
    public partial class PropertyController
    {
        #region Get

        [HttpGet("photo/{photoId:int}")]
        public async Task<IActionResult> GetPropertyPhotoByIdAsync(int photoId)
        {
            if (photoId <= 0)
                return BadRequest("Photo ID is required");

            try
            {
                var photo = await _propertyRepository.GetPropertyPhotoByIdAsync(photoId, CurrentOrganizationId);
                if (photo == null)
                    return NotFound("Photo not found");

                var property = await _propertyRepository.GetPropertyByIdAsync(photo.PropertyId, CurrentOrganizationId);
                var listingScope = BuildListingPhotoScope(property?.OfficeName, property?.PropertyCode);

                var response = new PropertyPhotoResponseDto(photo);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, listingScope, photo.PhotoPath, ImageType.Photos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property photo by ID: {PhotoId}", photoId);
                return ServerError("An error occurred while retrieving the property photo");
            }
        }

        [HttpGet("{propertyId:guid}/photos")]
        public async Task<IActionResult> GetPropertyPhotosByPropertyIdAsync(Guid propertyId)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var propertyCode = property.PropertyCode;
                var officeName = property.OfficeName;
                var photos = await _propertyRepository.GetPropertyPhotosByPropertyIdAsync(propertyId);

                var response = new List<PropertyPhotoResponseDto>();
                foreach (var photo in photos)
                {
                    var listingScope = BuildListingPhotoScope(officeName, propertyCode);
                    var photoResponse = new PropertyPhotoResponseDto(photo);
                    photoResponse.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, listingScope, photo.PhotoPath, ImageType.Photos);
                    response.Add(photoResponse);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property photos by PropertyId: {PropertyId}", propertyId);
                return ServerError("An error occurred while retrieving property photos");
            }
        }

        #endregion

        #region Post

        [HttpPost("{propertyId:guid}/photo")]
        public async Task<IActionResult> AddPropertyPhotoAsync(Guid propertyId, [FromBody] CreatePropertyPhotoDto dto)
        {
            if (propertyId == Guid.Empty)
                return BadRequest("Property ID is required");

            if (dto == null)
                return BadRequest("Photo data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");

                var listingScope = BuildListingPhotoScope(property.OfficeName, property.PropertyCode);
                var photo = dto.ToModel(propertyId);
                photo.PhotoPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(CurrentOrganizationId, listingScope, dto.FileDetails, ImageType.Photos) ?? string.Empty;

                var created = await _propertyRepository.CreatePropertyPhotoAsync(photo);
                var response = new PropertyPhotoResponseDto(created);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, listingScope, created.PhotoPath, ImageType.Photos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding property photo: {PropertyId}", propertyId);
                return ServerError("An error occurred while adding the property photo");
            }
        }

        #endregion

        #region Put

        [HttpPut("photo/order")]
        public async Task<IActionResult> UpdatePropertyPhotoOrderAsync([FromBody] UpdatePropertyPhotoOrderDto dto)
        {
            if (dto == null)
                return BadRequest("Photo update data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _propertyRepository.GetPropertyPhotoByIdAsync(dto.PhotoId, CurrentOrganizationId);
                if (existing == null)
                    return NotFound("Photo not found");

                await _propertyRepository.UpdatePropertyPhotoOrderAsync(dto.PhotoId, dto.Order);
                existing.Order = dto.Order;

                return Ok(new PropertyPhotoResponseDto(existing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property photo order: {PhotoId}", dto.PhotoId);
                return ServerError("An error occurred while updating the property photo order");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("photo/{photoId:int}")]
        public async Task<IActionResult> DeletePropertyPhotoByIdAsync(int photoId)
        {
            if (photoId <= 0)
                return BadRequest("Photo ID is required");

            try
            {
                var photo = await _propertyRepository.GetPropertyPhotoByIdAsync(photoId, CurrentOrganizationId);
                if (photo == null)
                    return NoContent();

                var property = await _propertyRepository.GetPropertyByIdAsync(photo.PropertyId, CurrentOrganizationId);
                var listingScope = BuildListingPhotoScope(property?.OfficeName, property?.PropertyCode);

                if (!string.IsNullOrWhiteSpace(photo.PhotoPath))
                    await _fileService.DeleteImageAsync(CurrentOrganizationId, listingScope, photo.PhotoPath, ImageType.Photos);

                await _propertyRepository.DeletePropertyPhotoByIdAsync(photoId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property photo: {PhotoId}", photoId);
                return ServerError("An error occurred while deleting the property photo");
            }
        }

        #endregion

        private static string BuildListingPhotoScope(string? officeName, string? propertyCode)
        {
            var normalizedOffice = string.IsNullOrWhiteSpace(officeName) ? "global" : officeName.Trim();
            var normalizedCode = string.IsNullOrWhiteSpace(propertyCode) ? "unknown-property" : propertyCode.Trim();
            return $"{normalizedOffice}/listings/{normalizedCode}";
        }
    }
}
