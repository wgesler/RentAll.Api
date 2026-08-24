using RentAll.Api.Dtos.Properties.PropertyAgreements;
using RentAll.Domain.Constants;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    #region Get
    [HttpGet("property-agreement/rent-roll")]
    public async Task<IActionResult> GetRentRollByOfficeIdsAsync()
    {
        try
        {
            var rentRoll = await _propertyRepository.GetPropertyAgreementRentRollByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = rentRoll.Select(item => new PropertyAgreementRentRollResponseDto(item))
                .ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rent roll by offices");
            return ServerError("An error occurred while retrieving rent roll");
        }
    }

    [HttpGet("property-agreement/{propertyId:guid}")]
    public async Task<IActionResult> GetPropertyAgreementByPropertyIdAsync(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("Property ID is required");

        try
        {
            var agreement = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(propertyId);
            if (agreement == null)
                return Ok();

            // Get the office name for file storage path
            var office = await _organizationRepository.GetOfficeByIdAsync(agreement.OfficeId, CurrentOrganizationId);
            var officeName = office != null ? office.Name : null;

            // Get W9, Insurance, and Agreement file details if paths are available
            var response = new PropertyAgreementResponseDto(agreement);
            response.W9FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, officeName, agreement.W9Path, ImageType.W9Forms);
            response.InsuranceFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, officeName, agreement.InsurancePath, ImageType.Insurances);
            response.AgreementFileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(CurrentOrganizationId, officeName, agreement.AgreementPath);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property agreement: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving the property agreement");
        }
    }
    #endregion

    #region Post
    [HttpPost("property-agreement/{propertyId:guid}")]
    public async Task<IActionResult> CreatePropertyAgreementAsync(Guid propertyId, [FromBody] CreatePropertyAgreementDto dto)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("Property ID is required");
        if (dto == null)
            return BadRequest("Agreement data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, CurrentOrganizationId);
            if (property == null)
                return NotFound("Property not found");

            var existing = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(propertyId);
            if (existing != null)
                return Conflict("Property agreement already exists for this property");

            var office = await _organizationRepository.GetOfficeByIdAsync(property.OfficeId, CurrentOrganizationId);
            var officeName = office?.Name;

            var model = dto.ToModel(propertyId, property.OfficeId, property.OrganizationId);
            model.W9Path = await _fileAttachmentHelper.SaveImageIfPresentAsync(CurrentOrganizationId, officeName, dto.W9FileDetails, ImageType.W9Forms);
            model.InsurancePath = await _fileAttachmentHelper.SaveImageIfPresentAsync(CurrentOrganizationId, officeName, dto.InsuranceFileDetails, ImageType.Insurances);
            model.AgreementPath = await _fileAttachmentHelper.SaveDocumentIfPresentAsync(CurrentOrganizationId, officeName, dto.AgreementFileDetails, DocumentType.Agreements);
            var saved = await _propertyRepository.CreatePropertyAgreementAsync(model);

            var response = new PropertyAgreementResponseDto(saved);
            response.W9FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, officeName, saved.W9Path, ImageType.W9Forms);
            response.InsuranceFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, officeName, saved.InsurancePath, ImageType.Insurances);
            response.AgreementFileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(CurrentOrganizationId, officeName, saved.AgreementPath);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property agreement: {PropertyId}", propertyId);
            return ServerError("An error occurred while creating the property agreement");
        }
    }

    [HttpPost("property-agreement/rent-roll/agreement-line")]
    public async Task<IActionResult> CreateRentRollAgreementLineAsync([FromBody] CreatePropertyAgreementLineDto dto)
    {
        if (dto == null)
            return BadRequest("Agreement line data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var isCompanyLine = !dto.PropertyId.HasValue || ReceiptPropertyConstants.IsCompanyPropertyId(dto.PropertyId.Value);
            if (!isCompanyLine)
            {
                var property = await _propertyRepository.GetPropertyByIdAsync(dto.PropertyId!.Value, CurrentOrganizationId);
                if (property == null)
                    return NotFound("Property not found");
            }

            var model = dto.ToModel(dto.PropertyId, CurrentOrganizationId);
            var saved = await _propertyRepository.CreateRentRollAgreementLineAsync(model);
            return Ok(new PropertyAgreementLineResponseDto(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rent roll agreement line");
            return ServerError("An error occurred while creating the agreement line");
        }
    }
    #endregion

    #region Put
    [HttpPut("property-agreement")]
    public async Task<IActionResult> UpdatePropertyAgreementAsync([FromBody] UpdatePropertyAgreementDto dto)
    {
        if (dto == null)
            return BadRequest("Agreement data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(dto.PropertyId);
            if (existing == null)
                return NotFound("Property agreement not found");

            var office = await _organizationRepository.GetOfficeByIdAsync(existing.OfficeId, CurrentOrganizationId);
            var officeName = office?.Name;

            var model = dto.ToModel(existing, CurrentOrganizationId);
            model.W9Path = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(CurrentOrganizationId, officeName, dto.W9FileDetails, ImageType.W9Forms, existing.W9Path, dto.W9Path);
            model.InsurancePath = await _fileAttachmentHelper.ResolveImagePathForUpdateAsync(CurrentOrganizationId, officeName, dto.InsuranceFileDetails, ImageType.Insurances, existing.InsurancePath, dto.InsurancePath);
            model.AgreementPath = await _fileAttachmentHelper.ResolveDocumentPathForUpdateAsync(CurrentOrganizationId, officeName, dto.AgreementFileDetails, DocumentType.Agreements, existing.AgreementPath, dto.AgreementPath);

            var updatedContact = await _propertyRepository.UpdatePropertyAgreementByPropertyIdAsync(model);
            var response = new PropertyAgreementResponseDto(updatedContact);

            response.W9FileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, officeName, updatedContact.W9Path, ImageType.W9Forms);
            response.InsuranceFileDetails = await _fileAttachmentHelper.GetImageDetailsForResponseAsync(CurrentOrganizationId, officeName, updatedContact.InsurancePath, ImageType.Insurances);
            response.AgreementFileDetails = await _fileAttachmentHelper.GetDocumentDetailsForResponseAsync(CurrentOrganizationId, officeName, updatedContact.AgreementPath);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property agreement: {PropertyId}", dto.PropertyId);
            return ServerError("An error occurred while updating the property agreement");
        }
    }

    [HttpPut("property-agreement/rent-roll/agreement-line")]
    public async Task<IActionResult> UpdateRentRollAgreementLineAsync([FromBody] UpdatePropertyAgreementLineDto dto)
    {
        if (dto == null)
            return BadRequest("Agreement line data is required");

        if (!dto.AgreementLineId.HasValue || dto.AgreementLineId.Value <= 0)
            return BadRequest("AgreementLineId is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var existing = await _propertyRepository.GetAgreementLineByIdAsync(dto.AgreementLineId.Value);
            if (existing == null || existing.OrganizationId != CurrentOrganizationId)
                return NotFound("Agreement line not found");

            var isCompanyLine = !existing.AgreementId.HasValue;
            if (!isCompanyLine)
                return BadRequest("Use property agreement update for property-linked agreement lines");

            var model = dto.ToModel(null, CurrentOrganizationId, existing);
            var saved = await _propertyRepository.UpdateRentRollAgreementLineAsync(model);
            return Ok(new PropertyAgreementLineResponseDto(saved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rent roll agreement line: {AgreementLineId}", dto.AgreementLineId);
            return ServerError("An error occurred while updating the agreement line");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("property-agreement/{propertyId:guid}")]
    public async Task<IActionResult> DeletePropertyAgreementByPropertyIdAsync(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("Property ID is required");

        try
        {
            var existing = await _propertyRepository.GetPropertyAgreementByPropertyIdAsync(propertyId);
            if (existing == null)
                return NoContent();

            var office = await _organizationRepository.GetOfficeByIdAsync(existing.OfficeId, CurrentOrganizationId);
            var officeName = office?.Name;

            if (!string.IsNullOrWhiteSpace(existing.W9Path))
                await _fileService.DeleteImageAsync(CurrentOrganizationId, officeName, existing.W9Path, ImageType.W9Forms);
            if (!string.IsNullOrWhiteSpace(existing.InsurancePath))
                await _fileService.DeleteImageAsync(CurrentOrganizationId, officeName, existing.InsurancePath, ImageType.Insurances);
            if (!string.IsNullOrWhiteSpace(existing.AgreementPath))
                await _fileService.DeleteDocumentAsync(CurrentOrganizationId, officeName, existing.AgreementPath);

            await _propertyRepository.DeletePropertyAgreementByPropertyIdAsync(propertyId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting property agreement: {PropertyId}", propertyId);
            return ServerError("An error occurred while deleting the property agreement");
        }
    }

    [HttpDelete("property-agreement/rent-roll/agreement-line/{agreementLineId:int}")]
    public async Task<IActionResult> DeleteRentRollAgreementLineAsync(int agreementLineId)
    {
        if (agreementLineId <= 0)
            return BadRequest("AgreementLineId is required");

        try
        {
            var existing = await _propertyRepository.GetAgreementLineByIdAsync(agreementLineId);
            if (existing == null || existing.OrganizationId != CurrentOrganizationId)
                return NoContent();

            if (existing.AgreementId.HasValue)
                return BadRequest("Use property agreement update for property-linked agreement lines");

            await _propertyRepository.DeleteRentRollAgreementLineByIdAsync(agreementLineId, CurrentOrganizationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rent roll agreement line: {AgreementLineId}", agreementLineId);
            return ServerError("An error occurred while deleting the agreement line");
        }
    }

    #endregion
}
