using RentAll.Api.Dtos.Photos;

namespace RentAll.Api.Controllers;

public partial class MaintenanceController
{
    #region Get
    [HttpGet("inspection/photo/{photoId:guid}")]
    public async Task<IActionResult> GetInspectionPhotoById(Guid photoId)
    {
        if (photoId == Guid.Empty)
            return BadRequest("Photo ID is required");

        var photo = await _photoRepository.GetByIdAsync(photoId, CurrentOrganizationId);
        if (photo == null)
            return NotFound();

        var officeName = await GetInspectionPhotoOfficeNameAsync(photo.OfficeId);
        var propertyCode = await GetInspectionPhotoPropertyCodeAsync(photo.MaintenanceId);
        var photoScope = ResolveInspectionPhotoScopeForExistingPath(officeName, propertyCode, photo.PhotoPath);
        var response = new PhotoResponseDto(photo);
        response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(photo.OrganizationId, photoScope, photo.PhotoPath, ImageType.Photos);

        return Ok(response);
    }
    #endregion

    #region Post
    [HttpPost("inspection/photo")]
    public async Task<IActionResult> AddInspectionPhoto([FromBody] CreatePhotoDto dto)
    {
        if (dto == null)
            return BadRequest("Photo data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var model = dto.ToModel(CurrentOrganizationId, CurrentUser);
            var officeName = await GetInspectionPhotoOfficeNameAsync(dto.OfficeId);
            var propertyCode = await GetInspectionPhotoPropertyCodeAsync(dto.MaintenanceId);
            var photoScope = BuildInspectionPhotoScope(officeName, propertyCode);

            model.PhotoPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(dto.OrganizationId, photoScope, dto.FileDetails, ImageType.Photos);

            var created = await _photoRepository.CreateAsync(model);
            var response = new PhotoResponseDto(created);
            response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(created.OrganizationId, photoScope, created.PhotoPath, ImageType.Photos);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inspection photo");
            return ServerError("An error occurred while creating the inspection photo");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("inspection/photo/{photoId:guid}")]
    public async Task<IActionResult> DeleteInspectionPhotoById(Guid photoId)
    {
        if (photoId == Guid.Empty)
            return BadRequest("Photo ID is required");

        try
        {
            var photo = await _photoRepository.GetByIdAsync(photoId, CurrentOrganizationId);
            if (photo != null && photo.PhotoPath != null)
            {
                var officeName = await GetInspectionPhotoOfficeNameAsync(photo.OfficeId);
                var propertyCode = await GetInspectionPhotoPropertyCodeAsync(photo.MaintenanceId);
                var photoScope = ResolveInspectionPhotoScopeForExistingPath(officeName, propertyCode, photo.PhotoPath);
                await _fileService.DeleteImageAsync(photo.OrganizationId, photoScope, photo.PhotoPath, ImageType.Photos);
            }

            await _photoRepository.DeleteByIdAsync(photoId, CurrentOrganizationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inspection photo: {PhotoId}", photoId);
            return ServerError("An error occurred while deleting the inspection photo");
        }
    }
    #endregion

    private async Task<string?> GetInspectionPhotoOfficeNameAsync(int? officeId)
    {
        if (!officeId.HasValue)
            return null;

        var office = await _organizationRepository.GetOfficeByIdAsync(officeId.Value, CurrentOrganizationId);
        return office?.Name;
    }

    private async Task<string?> GetInspectionPhotoPropertyCodeAsync(Guid maintenanceId)
    {
        if (maintenanceId == Guid.Empty)
            return null;

        var maintenance = await _maintenanceRepository.GetMaintenanceByIdAsync(maintenanceId, CurrentOrganizationId);
        return maintenance?.PropertyCode;
    }

    private static string BuildInspectionPhotoScope(string? officeName, string? propertyCode)
    {
        var officeSegment = string.IsNullOrWhiteSpace(officeName) ? "global" : officeName.Trim();
        var propertySegment = string.IsNullOrWhiteSpace(propertyCode) ? "unknown-property" : propertyCode.Trim();
        return $"{officeSegment}/inspection/{propertySegment}";
    }

    private static string ResolveInspectionPhotoScopeForExistingPath(string? officeName, string? propertyCode, string? photoPath)
    {
        if (!string.IsNullOrWhiteSpace(photoPath)
            && photoPath.Contains("/inspection/", StringComparison.OrdinalIgnoreCase))
        {
            return BuildInspectionPhotoScope(officeName, propertyCode);
        }

        return string.IsNullOrWhiteSpace(officeName) ? "global" : officeName.Trim();
    }
}
