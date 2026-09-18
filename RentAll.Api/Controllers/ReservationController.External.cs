using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class ReservationController
{
    [AllowAnonymous]
    [HttpPost("external")]
    public async Task<IActionResult> CreateExternalReservation([FromBody] JsonElement body)
    {
        var (parsed, context, reservations, parseError) = CreateExternalReservationRequestDto.TryParseFromBody(body);
        if (!parsed || context == null || reservations == null)
            return BadRequest(parseError ?? "Invalid request data");

        var organizationAccessError = await ValidateExternalReservationOrganizationAccessAsync(context.OrganizationId);
        if (organizationAccessError != null)
            return organizationAccessError;

        var accessError = await ValidateExternalReservationAccessAsync(context.OrganizationId, context.OfficeId);
        if (accessError != null)
            return accessError;

        var partnerError = await ValidateExternalReservationPartnerAsync(context.OrganizationId, context.PartnerVendorId);
        if (partnerError != null)
            return partnerError;

        var response = new ExternalReservationBatchResponseDto();
        for (var index = 0; index < reservations.Count; index++)
        {
            var dto = reservations[index];
            var item = new ExternalReservationBatchItemResultDto
            {
                Index = index,
                PropertyCode = (dto.PropertyCode ?? string.Empty).Trim(),
                ReferenceNo = string.IsNullOrWhiteSpace(dto.ReferenceNo) ? null : dto.ReferenceNo.Trim()
            };

            try
            {
                var (success, updated, reservation, errorMessage) = await UpsertExternalReservationAsync(dto, context);
                item.Success = success;
                item.Updated = updated;
                item.ErrorMessage = errorMessage;
                item.Reservation = reservation;
                item.ReservationCode = reservation?.ReservationCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting external reservation {PropertyCode} {ReferenceNo}", item.PropertyCode, item.ReferenceNo);
                item.Success = false;
                item.ErrorMessage = "An error occurred while saving the reservation";
            }

            response.Results.Add(item);
        }

        response.SuccessCount = response.Results.Count(result => result.Success);
        response.FailureCount = response.Results.Count - response.SuccessCount;
        return Ok(response);
    }

    private async Task<(bool Success, bool Updated, ReservationResponseDto? Reservation, string? ErrorMessage)> UpsertExternalReservationAsync(
        CreateExternalReservationDto dto,
        ExternalPropertyIntakeContext context)
    {
        var property = await _propertyRepository.GetPropertyByCodeAsync(dto.PropertyCode.Trim(), context.OrganizationId);
        if (property == null)
            return (false, false, null, $"Property '{dto.PropertyCode.Trim()}' was not found.");

        var contactIds = new List<Guid>();
        var contactEntityType = dto.ExpectedContactEntityType;
        var (contactOk, contactId, contactError) = await ResolveExternalContactAsync(dto.Contact!, contactEntityType, "contact", context, SystemUserId);
        if (!contactOk || contactId == null || contactId == Guid.Empty)
            return (false, false, null, contactError ?? "contact is required.");
        contactIds.Add(contactId.Value);

        Guid? companyId = null;
        if (dto.Company != null)
        {
            var (companyOk, resolvedCompanyId, companyError) = await ResolveExternalContactAsync(dto.Company, EntityType.Company, "company", context, SystemUserId);
            if (!companyOk)
                return (false, false, null, companyError);
            companyId = resolvedCompanyId;
        }

        var (agentOk, agentId, agentError) = await ResolveExternalAgentAsync(dto, context);
        if (!agentOk)
            return (false, false, null, agentError);

        var (maidOk, maidUserId, maidError) = await ResolveExternalMaidAsync(dto, context);
        if (!maidOk)
            return (false, false, null, maidError);

        var referenceNo = string.IsNullOrWhiteSpace(dto.ReferenceNo) ? null : dto.ReferenceNo.Trim();
        Reservation? existing = null;
        if (referenceNo != null)
        {
            var active = await _reservationRepository.GetActiveReservationsByOfficeIdsAsync(context.OrganizationId, context.OfficeId.ToString());
            existing = active.FirstOrDefault(reservation =>
                reservation.PropertyId == property.PropertyId
                && string.Equals((reservation.ReferenceNo ?? string.Empty).Trim(), referenceNo, StringComparison.OrdinalIgnoreCase));
        }

        if (existing != null)
        {
            dto.ApplyToExisting(existing, property, contactIds, companyId, agentId, maidUserId);
            existing.ModifiedBy = SystemUserId;
            var updated = await _reservationRepository.UpdateByIdAsync(existing);
            return (true, true, new ReservationResponseDto(updated), null);
        }

        var createDto = dto.ToCreateReservationDto(context.OrganizationId, property.OfficeId, property.PropertyId, property, contactIds, companyId, agentId, maidUserId);
        createDto.ExtraFeeLines = [];
        var (isValid, validationError) = createDto.IsValid();
        if (!isValid)
            return (false, false, null, validationError);

        var code = await _organizationManager.GenerateEntityCodeAsync(context.OrganizationId, EntityType.Reservation);
        var model = createDto.ToModel(code, SystemUserId);
        model.ExtraFeeLines = dto.ToExtraFeeLines();
        var created = await _reservationRepository.CreateAsync(model);
        return (true, false, new ReservationResponseDto(created), null);
    }

    private async Task<(bool Success, Guid? ContactId, string? ErrorMessage)> ResolveExternalContactAsync(
        ExternalPropertyContactDto contact,
        EntityType entityType,
        string fieldLabel,
        ExternalPropertyIntakeContext context,
        Guid currentUser)
    {
        if (contact.EntityTypeId == 0)
            contact.EntityTypeId = (int)entityType;

        var (isValid, validationError) = contact.IsValid(fieldLabel);
        if (!isValid)
            return (false, null, validationError);

        var email = contact.Email.Trim();
        var existing = await _contactRepository.GetContactByEmailAsync(email, context.OrganizationId);
        if (existing != null)
        {
            if (existing.EntityType != entityType)
                return (false, null, $"{fieldLabel} email '{email}' is already used by {existing.EntityType} contact {existing.ContactCode}. Use a different email or change that contact to {entityType}.");

            contact.ApplyToExistingOwnerContact(existing, context.OfficeId, currentUser);
            var updated = await _contactRepository.UpdateByIdAsync(existing);
            return (true, updated.ContactId, null);
        }

        var code = await _contactManager.GenerateContactCodeAsync(context.OrganizationId, (int)entityType);
        var created = entityType switch
        {
            EntityType.Company => await _contactRepository.CreateAsync(contact.ToNewCompanyContactModel(context.OrganizationId, context.OfficeId, code, currentUser)),
            EntityType.Owner => await _contactRepository.CreateAsync(contact.ToNewOwnerContactModel(context.OrganizationId, context.OfficeId, code, currentUser)),
            _ => await _contactRepository.CreateAsync(contact.ToNewTenantContactModel(context.OrganizationId, context.OfficeId, code, currentUser))
        };
        return (true, created.ContactId, null);
    }

    private async Task<(bool Success, Guid? AgentId, string? ErrorMessage)> ResolveExternalAgentAsync(
        CreateExternalReservationDto dto,
        ExternalPropertyIntakeContext context)
    {
        if (dto.ReservationTypeId == (int)ReservationType.Owner)
            return (true, null, null);

        if (!string.IsNullOrWhiteSpace(dto.AgentCode))
        {
            var agent = await _organizationRepository.GetAgentByCodeAsync(dto.AgentCode.Trim(), context.OrganizationId);
            return agent == null ? (false, null, $"agentCode '{dto.AgentCode.Trim()}' was not found.") : (true, agent.AgentId, null);
        }

        return (false, null, "agentCode is required.");
    }

    private async Task<(bool Success, Guid? MaidUserId, string? ErrorMessage)> ResolveExternalMaidAsync(
        CreateExternalReservationDto dto,
        ExternalPropertyIntakeContext context)
    {
        if (!string.IsNullOrWhiteSpace(dto.MaidEmail))
        {
            var user = await _userRepository.GetUserByEmailAsync(dto.MaidEmail.Trim());
            if (user == null || user.OrganizationId != context.OrganizationId)
                return (false, null, $"maidEmail '{dto.MaidEmail.Trim()}' was not found.");
            return (true, user.UserId, null);
        }

        return (true, null, null);
    }

    private async Task<IActionResult?> ValidateExternalReservationOrganizationAccessAsync(Guid organizationId)
    {
        var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalPropertyKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        SetApplicationLogContext(organizationId, null);
        return null;
    }

    private async Task<IActionResult?> ValidateExternalReservationAccessAsync(Guid organizationId, int officeId)
    {
        var organizationError = await ValidateExternalReservationOrganizationAccessAsync(organizationId);
        if (organizationError != null)
            return organizationError;

        var office = await _organizationRepository.GetOfficeByIdAsync(officeId, organizationId);
        if (office == null)
            return BadRequest("Invalid OfficeId for OrganizationId");

        SetApplicationLogContext(organizationId, officeId);
        return null;
    }

    private async Task<IActionResult?> ValidateExternalReservationPartnerAsync(Guid organizationId, Guid partnerVendorId)
    {
        if (partnerVendorId == Guid.Empty)
            return BadRequest("VendorId is required");

        var partner = await _contactRepository.GetContactByIdsAsync(partnerVendorId, organizationId);
        if (partner == null || partner.EntityType != EntityType.Vendor)
            return BadRequest("Invalid VendorId");

        return null;
    }
}
