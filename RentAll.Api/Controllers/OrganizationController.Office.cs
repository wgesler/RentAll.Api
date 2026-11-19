using RentAll.Domain.Models.Common;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get
        [HttpGet("office/{organizationId:guid}")]
        public async Task<IActionResult> GetAllOffices(Guid organizationId)
        {
            try
            {
                IEnumerable<Office> offices;
                if (IsSuperAdmin())
                    offices = await _organizationRepository.GetOfficesByOrganizationIdAsync(organizationId);
                else if (IsAdmin())
                    offices = await _organizationRepository.GetOfficesByOrganizationIdAsync(CurrentOrganizationId);
                else
                    offices = await _organizationRepository.GetOfficesByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);

                var response = new List<OfficeResponseDto>();
                foreach (var office in offices)
                {
                    var dto = new OfficeResponseDto(office);
                    dto.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(office.OrganizationId, null, office.LogoPath, ImageType.Logos);
                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all offices");
                return ServerError("An error occurred while retrieving offices");
            }
        }

        [HttpGet("office/{officeId}")]
        public async Task<IActionResult> GetOfficeById(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                var office = await _organizationRepository.GetOfficeByIdAsync(officeId, CurrentOrganizationId);
                if (office == null)
                    return NotFound("Office not found");

                var response = new OfficeResponseDto(office);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(office.OrganizationId, null, office.LogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting office by ID: {OfficeId}", officeId);
                return ServerError("An error occurred while retrieving the office");
            }
        }
        #endregion

        #region Post
        [HttpPost("office")]
        public async Task<IActionResult> CreateOffice([FromBody] OfficeCreateDto dto)
        {
            if (dto == null)
                return BadRequest("Office data is required");

            var organizationId = IsSuperAdmin() ? dto.OrganizationId : CurrentOrganizationId;
            var (isValid, errorMessage) = dto.IsValid(await RequireOfficeManagementContactAsync(organizationId));
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                if (await _organizationRepository.ExistsByOfficeCodeAsync(dto.OfficeCode, organizationId))
                    return Conflict("Office Code already exists");

                // Create requested office
                var office = dto.ToModel();
                office.OrganizationId = organizationId;
                office.LogoPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(organizationId, office.Name, dto.FileDetails, ImageType.Logos);
                var createdOffice = await _organizationRepository.CreateAsync(office);

                // Create default cost codes for the office
                await _accountingManager.CreateDefaultCostCodeAsync(createdOffice.OrganizationId, createdOffice.OfficeId);
                await SyncPublicOfficeLogoAsync(createdOffice.OrganizationId, createdOffice.Name, createdOffice.LogoPath, dto.FileDetails);

                var response = new OfficeResponseDto(createdOffice);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(createdOffice.OrganizationId, null, createdOffice.LogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating office");
                return ServerError("An error occurred while creating the office");
            }
        }
        #endregion

        #region Put
        [HttpPut("office")]
        public async Task<IActionResult> UpdateOffice([FromBody] OfficeUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Office data is required");

            var organizationId = IsSuperAdmin() ? dto.OrganizationId : CurrentOrganizationId;
            var (isValid, errorMessage) = dto.IsValid(await RequireOfficeManagementContactAsync(organizationId));
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existingOffice = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, organizationId);
                if (existingOffice == null)
                    return NotFound("Office not found");

                if (existingOffice.OfficeCode != dto.OfficeCode)
                {
                    if (await _organizationRepository.ExistsByOfficeCodeAsync(dto.OfficeCode, organizationId))
                        return Conflict("Office Code already exists");
                }

                var office = dto.ToModel();
                office.OrganizationId = organizationId;

                office.LogoPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                    existingOffice.OrganizationId, existingOffice.Name, dto.FileDetails, ImageType.Logos, existingOffice.LogoPath, dto.LogoPath);

                var updatedOffice = await _organizationRepository.UpdateByIdAsync(office);
                if (!string.Equals(existingOffice.Name?.Trim(), updatedOffice.Name?.Trim(), StringComparison.OrdinalIgnoreCase))
                    await _fileService.DeletePublicOfficeLogoAsync(existingOffice.OrganizationId, existingOffice.Name);
                await SyncPublicOfficeLogoAsync(updatedOffice.OrganizationId, updatedOffice.Name, updatedOffice.LogoPath, dto.FileDetails);
                var response = new OfficeResponseDto(updatedOffice);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(updatedOffice.OrganizationId, null, updatedOffice.LogoPath, ImageType.Logos);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating office: {OfficeId}", dto.OfficeId);
                return ServerError("An error occurred while updating the office");
            }
        }
        #endregion

        #region Delete
        [HttpDelete("office/{officeId}")]
        public async Task<IActionResult> DeleteOfficeByIdAsync(int officeId)
        {
            if (officeId <= 0)
                return BadRequest("Office ID is required");

            try
            {
                // Check if office exists check/delete logo before deleting office
                var existingOffice = await _organizationRepository.GetOfficeByIdAsync(officeId, CurrentOrganizationId);
                if (existingOffice != null && !string.IsNullOrWhiteSpace(existingOffice.LogoPath))
                    await _fileService.DeleteImageAsync(existingOffice.OrganizationId, await GetOfficeNameAsync(officeId), existingOffice.LogoPath, ImageType.Logos);
                if (existingOffice != null)
                    await _fileService.DeletePublicOfficeLogoAsync(existingOffice.OrganizationId, existingOffice.Name);

                await _organizationRepository.DeleteOfficeByIdAsync(officeId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting office: {OfficeId}", officeId);
                return ServerError("An error occurred while deleting the office");
            }
        }
        #endregion

        private async Task SyncPublicOfficeLogoAsync(
            Guid organizationId,
            string? officeName,
            string? logoPath,
            FileDetails? uploadedFile)
        {
            var content = TryGetUploadedLogoBytes(uploadedFile, out var contentType);
            if (content == null && !string.IsNullOrWhiteSpace(logoPath))
            {
                var stored = await _fileService.GetImageDetailsAsync(organizationId, officeName, logoPath, ImageType.Logos);
                content = TryGetUploadedLogoBytes(stored, out contentType);
            }

            if (content == null || content.Length == 0)
            {
                await _fileService.DeletePublicOfficeLogoAsync(organizationId, officeName);
                return;
            }

            await using var stream = new MemoryStream(content);
            await _fileService.SavePublicOfficeLogoAsync(organizationId, officeName, stream, contentType);
        }

        private static byte[]? TryGetUploadedLogoBytes(FileDetails? fileDetails, out string contentType)
        {
            contentType = "image/png";
            if (fileDetails == null)
                return null;

            if (!string.IsNullOrWhiteSpace(fileDetails.ContentType))
                contentType = fileDetails.ContentType.Trim();

            var raw = !string.IsNullOrWhiteSpace(fileDetails.File)
                ? fileDetails.File
                : fileDetails.DataUrl;
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            try
            {
                if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var comma = raw.IndexOf(',');
                    if (comma < 0 || comma == raw.Length - 1)
                        return null;
                    return Convert.FromBase64String(raw[(comma + 1)..]);
                }

                return Convert.FromBase64String(raw);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
