using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    private static readonly Guid ExternalPropertySystemUserId = new("99999999-9999-9999-9999-999999999999");

    [AllowAnonymous]
    [HttpPost("external")]
    public async Task<IActionResult> UpsertExternalProperty(
        [FromBody] CreateExternalPropertyDto dto,
        [FromServices] IOptions<ExternalPropertyIntakeSettings> settings)
    {
        if (dto == null)
            return BadRequest("Property data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (!IsExternalPropertyApiKeyValid(settings.Value.ApiKey))
            return Unauthorized("Invalid API key");

        SetApplicationLogContext(dto.OrganizationId, dto.OfficeId);

        try
        {
            var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
            if (organization == null)
                return BadRequest("Invalid OrganizationId");

            var office = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, dto.OrganizationId);
            if (office == null)
                return BadRequest("Invalid OfficeId for OrganizationId");

            var propertyCode = dto.PropertyCode.Trim();
            var propertyDto = dto.ToCreatePropertyDto(propertyCode);
            var (propertyDtoIsValid, propertyDtoErrorMessage) = propertyDto.IsValid();
            if (!propertyDtoIsValid)
                return BadRequest(propertyDtoErrorMessage ?? "Invalid request data");

            var existingProperty = await _propertyRepository.GetPropertyByCodeAsync(propertyCode, dto.OrganizationId);
            if (existingProperty != null)
            {
                var (updateResult, updateError) = await TryUpdateExternalPropertyAsync(existingProperty, propertyDto);
                if (updateResult == null)
                    return BadRequest(updateError ?? "Invalid request data");

                return Ok(new PropertyResponseDto(updateResult));
            }

            var createdProperty = await _propertyRepository.CreateAsync(propertyDto.ToModel(ExternalPropertySystemUserId));
            return Ok(new PropertyResponseDto(createdProperty));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error upserting external property intake request. OrganizationId={OrganizationId}, OfficeId={OfficeId}, PropertyCode={PropertyCode}, VendorId={VendorId}",
                dto.OrganizationId,
                dto.OfficeId,
                dto.PropertyCode,
                dto.VendorId);
            return ServerError("An error occurred while saving the property");
        }
    }

    private async Task<(Property? Property, string? ErrorMessage)> TryUpdateExternalPropertyAsync(
        Property existingProperty,
        CreatePropertyDto propertyDto)
    {
        var updateDto = propertyDto.ToUpdateDto(existingProperty.PropertyId);
        var (updateIsValid, updateErrorMessage) = updateDto.IsValid();
        if (!updateIsValid)
            return (null, updateErrorMessage ?? "Invalid request data");

        var property = updateDto.ToModel(ExternalPropertySystemUserId);
        if (existingProperty.OfficeId != propertyDto.OfficeId)
            await _propertyManager.UpdatePropertyOfficeAsync(property, ExternalPropertySystemUserId);

        var updatedProperty = await _propertyRepository.UpdateByIdAsync(property);
        return (updatedProperty, null);
    }

    private bool IsExternalPropertyApiKeyValid(string configuredApiKey)
    {
        if (string.IsNullOrWhiteSpace(configuredApiKey))
            return false;

        var inboundApiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(inboundApiKey))
            return false;

        return string.Equals(inboundApiKey.Trim(), configuredApiKey.Trim(), StringComparison.Ordinal);
    }
}
