using RentAll.Domain.Models.Common;

namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get
        [HttpGet]
        public async Task<IActionResult> GetOrganizationsAsync()
        {
            try
            {
                var orgs = await _organizationRepository.GetOrganizationsAsync();
                var response = new List<OrganizationResponseDto>();
                foreach (var org in orgs)
                {
                    var dto = new OrganizationResponseDto(org);
                    dto.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(org.OrganizationId, null, org.LogoPath, ImageType.Logos);
                    response.Add(dto);
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all organizations");
                return ServerError("An error occurred while retrieving organizations");
            }
        }

        [HttpGet("{organizationId}")]
        public async Task<IActionResult> GetOrganizationByIdAsync(Guid organizationId)
        {
            try
            {
                var org = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
                if (org == null)
                    return NotFound("Organization not found");

                var response = new OrganizationResponseDto(org);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(org.OrganizationId, null, org.LogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organization by ID: {OrganizationId}", organizationId);
                return ServerError("An error occurred while retrieving the organization");
            }
        }

        #endregion

        #region Post

        [HttpPost]
        public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationDto dto)
        {
            if (!IsSuperAdmin())
                return Unauthorized("NoAccess");

            if (dto == null)
                return BadRequest("Organization data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            var organizationId = Guid.NewGuid();
            string? savedLogoPath = null;

            try
            {
                var code = await _organizationManager.GenerateEntityCodeAsync();
                var model = dto.ToModel(code, CurrentUser);
                model.OrganizationId = organizationId;

                if (dto.FileDetails != null && !string.IsNullOrWhiteSpace(dto.FileDetails.File))
                {
                    try
                    {
                        savedLogoPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(
                            organizationId, null, dto.FileDetails, ImageType.Logos);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving organization logo during create for organization {OrganizationId}", organizationId);
                        return ServerError("The organization logo could not be saved. Try again without a logo or verify blob storage access.");
                    }
                }

                model.LogoPath = savedLogoPath;

                var created = await _organizationRepository.CreateAsync(model);
                await _organizationRepository.CreateFeatureAsync(new Feature { OrganizationId = created.OrganizationId, FeatureTypeId = FeatureType.MainProgram, HasAccess = true });
                await CreateDefaultMainOfficeAsync(created, dto.FileDetails);
                var response = new OrganizationResponseDto(created);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(
                    created.OrganizationId, null, created.LogoPath, ImageType.Logos);

                return Ok(response);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(savedLogoPath))
                {
                    try
                    {
                        await _fileService.DeleteImageAsync(organizationId, null, savedLogoPath, ImageType.Logos);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to clean up logo after organization create failure: {LogoPath}", savedLogoPath);
                    }
                }

                _logger.LogError(ex, "Error creating organization");
                return ServerError("An error occurred while creating the organization");
            }
        }

        #endregion

        #region Put

        [HttpPut]
        public async Task<IActionResult> UpdateOrganization([FromBody] UpdateOrganizationDto dto)
        {
            if (dto == null)
                return BadRequest("Organization data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                var existing = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
                if (existing == null)
                    return NotFound("Organization not found");

                // If OrganizationCode changed, ensure new one is unique
                if (!string.Equals(existing.OrganizationCode, dto.OrganizationCode, StringComparison.OrdinalIgnoreCase))
                    return Conflict("OrganizationCode cannot change");

                var model = dto.ToModel(CurrentUser);

                model.LogoPath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(
                    existing.OrganizationId, null, dto.FileDetails, ImageType.Logos, existing.LogoPath, dto.LogoPath);

                var updated = await _organizationRepository.UpdateByIdAsync(model);
                var response = new OrganizationResponseDto(updated);
                response.FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(updated.OrganizationId, null, updated.LogoPath, ImageType.Logos);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization: {OrganizationId}", dto.OrganizationId);
                return ServerError("An error occurred while updating the organization");
            }
        }

        #endregion

        #region Delete

        [HttpDelete("{organizationId}")]
        public async Task<IActionResult> DeleteOrganizationByIdAsync(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                return BadRequest("OrganizationId is required");

            try
            {
                var users = await _userRepository.GetUsersByOrganizationIdAsync(organizationId);
                if (users != null)
                    return BadRequest("Unable to delete an organization that still has users");

                // Check if organization exists then check/delete logo
                var existing = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
                if (existing != null && !string.IsNullOrWhiteSpace(existing.LogoPath))
                    await _fileService.DeleteImageAsync(existing.OrganizationId, null, existing.LogoPath, ImageType.Logos);

                // Delete all documents/receipts as well (TBD)

                await _organizationRepository.DeleteOrganizationByIdAsync(organizationId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization: {OrganizationId}", organizationId);
                return ServerError("An error occurred while deleting the organization");
            }
        }

        #endregion

        #region Auto Create Methods
        private async Task CreateDefaultMainOfficeAsync(Organization organization, FileDetails? fileDetails)
        {
            var cityName = string.IsNullOrWhiteSpace(organization.City) ? "Main" : organization.City.Trim();
            var office = new Office
            {
                OrganizationId = organization.OrganizationId,
                OfficeCode = cityName.ToUpperInvariant(),
                Name = cityName,
                Address1 = organization.Address1,
                Address2 = organization.Address2,
                Suite = organization.Suite,
                City = organization.City,
                State = organization.State,
                Zip = organization.Zip,
                Phone = organization.Phone,
                Fax = organization.Fax,
                Website = organization.Website,
                IsInternational = organization.IsInternational,
                IsActive = organization.IsActive
            };

            office.LogoPath = await _fileAttachmentHelper.SaveImageIfPresentAsync(organization.OrganizationId, null, fileDetails, ImageType.Logos);

            // For every new organization, create a default office
            var createdOffice = await _organizationRepository.CreateAsync(office);

            // If this is a Partner organization, create a default vendor
            if (organization.OrganizationType == OrganizationType.Partner)
                await CreateDefaultVendorContactAsync(organization, createdOffice);

            // Create cost codes for this fofice
            await _accountingManager.CreateDefaultCostCodeAsync(createdOffice.OrganizationId, createdOffice.OfficeId);

            // Create a new admin/user for this organizaiton
            var adminType = organization.OrganizationType == OrganizationType.Partner ? RoleType.PartnerAdmin : RoleType.Admin;
            await _userRepository.AddDefaultAdminAsync(createdOffice.OrganizationId, createdOffice.OfficeId, organization.Phone, adminType, CurrentUser);
        }

        private async Task CreateDefaultVendorContactAsync(Organization organization, Office office)
        {
            var companyName = string.IsNullOrWhiteSpace(organization.Name) ? "Main" : organization.Name.Trim();
            var displayName = companyName.Length <= 10 ? companyName : companyName[..10];
            var email = !string.IsNullOrWhiteSpace(organization.ContactEmail) ? organization.ContactEmail.Trim() : $"admin@{(organization.Domain ?? string.Empty).Trim()}";
            var address2 = string.Join(" ", new[] { organization.Address2, organization.Suite }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
            var firstName = (organization.ContactName ?? string.Empty).Trim();
            var code = await _contactManager.GenerateContactCodeAsync(organization.OrganizationId, (int)EntityType.Vendor);
            await _contactRepository.CreateAsync(new Contact
            {
                OrganizationId = organization.OrganizationId,
                OfficeId = office.OfficeId,
                OfficeAccess = new List<int> { office.OfficeId },
                ContactCode = code,
                EntityType = EntityType.Vendor,
                VendorType = VendorType.Company,
                CompanyName = companyName,
                CompanyEmail = email,
                DisplayName = displayName,
                FirstName = firstName.Length <= 100 ? firstName : firstName[..100],
                Address1 = organization.Address1,
                Address2 = string.IsNullOrWhiteSpace(address2) ? null : (address2.Length <= 100 ? address2 : address2[..100]),
                City = organization.City,
                State = organization.State,
                Zip = organization.Zip,
                Phone = organization.Phone,
                Email = email,
                Markup = 25,
                RevenueSplitOwner = 75,
                RevenueSplitOffice = 25,
                WorkingCapitalBalance = 0,
                LinenAndTowelFee = 0,
                IsInternational = organization.IsInternational,
                IsActive = true,
                CreatedBy = CurrentUser
            });
        }
        #endregion
    }
}
