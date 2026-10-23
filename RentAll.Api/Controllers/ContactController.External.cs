using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Contacts;
using RentAll.Api.Services;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class ContactController
{
    private const int MaxExternalContactsPerRequest = 50;

    [AllowAnonymous]
    [HttpPost("external")]
    public async Task<IActionResult> UpsertExternalContacts([FromBody] JsonElement body)
    {
        var (parsed, context, contacts, parseError) = TryParseExternalContacts(body);
        if (!parsed || context == null || contacts == null)
            return BadRequest(parseError ?? "Invalid request data");

        var organizationAccessError = await ValidateExternalContactOrganizationAccessAsync(context.OrganizationId);
        if (organizationAccessError != null)
            return organizationAccessError;

        var accessError = await ValidateExternalContactAccessAsync(context.OrganizationId, context.OfficeId);
        if (accessError != null)
            return accessError;

        var partnerError = await ValidateExternalContactPartnerAsync(context.OrganizationId, context.PartnerVendorId);
        if (partnerError != null)
            return partnerError;

        var response = new ExternalContactBatchResponseDto();
        var hadException = false;
        for (var index = 0; index < contacts.Count; index++)
        {
            var contact = contacts[index];
            var email = contact.Email.Trim();
            var item = new ExternalContactBatchItemResultDto();

            var itemFailedWithException = false;
            try
            {
                var result = await _externalPropertyOwnerContactResolver.UpsertOwnerAsync(contact, context.OrganizationId, context.OfficeId, SystemUserId, $"Contacts[{index}]");
                item.Success = result.Success;
                item.Updated = result.Updated;
                item.ErrorMessage = result.ErrorMessage;
                item.Contact = result.Contact == null ? null : await ToExternalContactAsync(result.Contact);
            }
            catch (Exception ex)
            {
                hadException = true;
                itemFailedWithException = true;
                _logger.LogError(ex, "[ExternalContactTrace] Error upserting contact {Email}", email);
                item.Success = false;
                item.ErrorMessage = "An error occurred while saving the contact";
            }

            await _externalPropertyUploadLogService.LogExternalApiAttemptAsync(
                new ExternalPropertyApiAttemptLog
                {
                    OrganizationId = context.OrganizationId,
                    OfficeId = context.OfficeId,
                    VendorId = context.PartnerVendorId,
                    EventType = PropertyUploadLogEvents.ContactUpsert,
                    Operation = PropertyUploadLogOperations.UpsertContact,
                    Detail = item.Success
                        ? $"{(item.Updated ? "Updated" : "Created")} contact {item.Contact?.ContactCode} {email}."
                        : item.ErrorMessage
                },
                item.Success ? StatusCodes.Status200OK : itemFailedWithException ? StatusCodes.Status500InternalServerError : StatusCodes.Status400BadRequest,
                item.ErrorMessage);

            response.Results.Add(item);
        }

        response.SuccessCount = response.Results.Count(result => result.Success);
        response.FailureCount = response.Results.Count - response.SuccessCount;
        if (response.FailureCount == 0)
            return Ok(response);

        return StatusCode(hadException ? StatusCodes.Status500InternalServerError : StatusCodes.Status400BadRequest, response);
    }

    [AllowAnonymous]
    [HttpGet("external")]
    public async Task<IActionResult> GetExternalContactsAsync([FromQuery] ExternalContactExportQueryDto query)
    {
        if (query == null)
            return BadRequest("Query parameters are required");

        var (isValid, errorMessage) = query.Validate();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var accessError = await ValidateExternalContactAccessAsync(query.OrganizationId, query.OfficeId);
        if (accessError != null)
            return accessError;

        try
        {
            var contacts = await LoadExternalContactsAsync(query.OrganizationId, query.OfficeId);
            return Ok(await ToExternalContactBatchResponse(contacts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external contacts. OrganizationId={OrganizationId}, OfficeId={OfficeId}", query.OrganizationId, query.OfficeId);
            return ServerError("An error occurred while retrieving contacts");
        }
    }

    [AllowAnonymous]
    [HttpGet("external/{externalRefNo}")]
    public async Task<IActionResult> GetExternalContactByRefNoAsync(string externalRefNo, [FromQuery] ExternalContactExportQueryDto query)
    {
        if (query == null)
            return BadRequest("Query parameters are required");

        var (isValid, errorMessage) = query.Validate();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (string.IsNullOrWhiteSpace(externalRefNo))
            return BadRequest("ExternalRefNo is required");

        var accessError = await ValidateExternalContactAccessAsync(query.OrganizationId, query.OfficeId);
        if (accessError != null)
            return accessError;

        try
        {
            var externalRef = externalRefNo.Trim();
            var contacts = await LoadExternalContactsAsync(query.OrganizationId, query.OfficeId);
            var matches = contacts.Where(contact =>
                string.Equals(contact.ExternalRefNo?.Trim(), externalRef, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0)
                return NotFound("Contact not found");

            return Ok(await ToExternalContactBatchResponse(matches));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting external contact. OrganizationId={OrganizationId}, OfficeId={OfficeId}, ExternalRefNo={ExternalRefNo}", query.OrganizationId, query.OfficeId, externalRefNo);
            return ServerError("An error occurred while retrieving the contact");
        }
    }

    private async Task<List<Contact>> LoadExternalContactsAsync(Guid organizationId, int officeId)
    {
        var contacts = await _contactRepository.GetExternalContactsByOfficeAsync(organizationId, officeId);
        return contacts.Where(ExternalContactResponseDto.IsExportable).ToList();
    }

    private async Task<ExternalContactResponseDto> ToExternalContactAsync(Contact contact)
    {
        var response = ExternalContactResponseDto.FromContact(contact);
        if (contact.ContactCardId is not > 0)
            return response;

        var card = await _contactRepository.GetContactCardByIdAsync(contact.ContactCardId.Value, contact.OrganizationId, contact.OfficeId);
        if (card != null)
            response.ApplyCard(card.CardTypeId, card.CardName, card.LastFour);

        return response;
    }

    private async Task<ExternalContactBatchResponseDto> ToExternalContactBatchResponse(IEnumerable<Contact> contacts)
    {
        var results = new List<ExternalContactBatchItemResultDto>();
        foreach (var contact in contacts)
        {
            results.Add(new ExternalContactBatchItemResultDto
            {
                Success = true,
                Updated = false,
                Contact = await ToExternalContactAsync(contact)
            });
        }

        return new ExternalContactBatchResponseDto
        {
            SuccessCount = results.Count,
            FailureCount = 0,
            Results = results
        };
    }

    private static (bool Success, ExternalPropertyIntakeContext? Context, List<ExternalPropertyContactDto>? Contacts, string? ErrorMessage) TryParseExternalContacts(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, null, "Contact data is required");

        var errors = new List<string>();
        ExternalPropertyIntakeJson.CollectIntakeContextErrors(body, errors, out var context);

        var contacts = new List<ExternalPropertyContactDto>();
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "contacts", out var contactsElement))
            errors.Add("Contacts must contain at least one item");
        else if (contactsElement.ValueKind != JsonValueKind.Array)
            errors.Add(contactsElement.ValueKind == JsonValueKind.Object
                ? "Contacts must be a JSON array. Received a single object; wrap the contact in an array."
                : $"Contacts must be a JSON array. Received {ExternalPropertyIntakeErrors.Describe(contactsElement)}.");
        else
        {
            if (contactsElement.GetArrayLength() == 0)
                errors.Add("Contacts must contain at least one item");
            if (contactsElement.GetArrayLength() > MaxExternalContactsPerRequest)
                errors.Add($"Contacts cannot exceed {MaxExternalContactsPerRequest} items per request");

            var index = 0;
            foreach (var contactElement in contactsElement.EnumerateArray())
            {
                var prefix = $"Contacts[{index}]";
                var itemErrors = new List<string>();
                ExternalPropertyIntakeErrors.CollectContactElement(contactElement, prefix, itemErrors, ownerOnly: true);
                if (itemErrors.Count == 0)
                {
                    try
                    {
                        var contact = ExternalPropertyIntakeJson.DeserializeContact(contactElement);
                        contact.EntityTypeId = (int)EntityType.Owner;
                        contact.VendorTypeId = null;
                        itemErrors.AddRange(contact.CollectErrors(prefix));
                        if (itemErrors.Count == 0)
                            contacts.Add(contact);
                    }
                    catch (JsonException ex)
                    {
                        itemErrors.Add(ExternalPropertyIntakeErrors.FromJsonException(ex, contactElement, prefix));
                    }
                }

                errors.AddRange(itemErrors);
                index++;
            }
        }

        if (errors.Count > 0 || context == null)
            return (false, context, null, errors.Count > 0 ? ExternalPropertyIntakeErrors.Join(errors) : "Invalid request data");

        return (true, context, contacts, null);
    }

    private async Task<IActionResult?> ValidateExternalContactOrganizationAccessAsync(Guid organizationId)
    {
        var organization = await _organizationRepository.GetOrganizationByIdAsync(organizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalPropertyKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        SetApplicationLogContext(organizationId, null);
        return null;
    }

    private async Task<IActionResult?> ValidateExternalContactAccessAsync(Guid organizationId, int officeId)
    {
        var organizationError = await ValidateExternalContactOrganizationAccessAsync(organizationId);
        if (organizationError != null)
            return organizationError;

        var office = await _organizationRepository.GetOfficeByIdAsync(officeId, organizationId);
        if (office == null)
            return BadRequest("Invalid OfficeId for OrganizationId");

        SetApplicationLogContext(organizationId, officeId);
        return null;
    }

    private async Task<IActionResult?> ValidateExternalContactPartnerAsync(Guid organizationId, Guid partnerVendorId)
    {
        if (partnerVendorId == Guid.Empty)
            return BadRequest("VendorId is required");

        var partner = await _contactRepository.GetContactByIdsAsync(partnerVendorId, organizationId);
        if (partner == null || partner.EntityType != EntityType.Vendor)
            return BadRequest("Invalid VendorId");

        return null;
    }
}
