using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Services;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    private const int MaxExternalOwnersPerRequest = 50;

    [AllowAnonymous]
    [HttpPost("owners/external")]
    public async Task<IActionResult> UpsertExternalOwners([FromBody] JsonElement body)
    {
        var (ownersParsed, context, owners, ownersError) = TryParseExternalOwners(body);
        if (!ownersParsed || context == null || owners == null)
            return await RejectExternalPropertyRequestAsync(body, PropertyUploadLogOperations.UpsertOwner, PropertyUploadLogEvents.OwnerUpsert, ownersError ?? "Invalid request data", context);

        var organizationAccessError = await ValidateExternalPropertyOrganizationAccessAsync(context.OrganizationId);
        if (organizationAccessError != null)
            return organizationAccessError;

        var accessError = await ValidateExternalPropertyAccessAsync(context.OrganizationId, context.OfficeId);
        if (accessError != null)
            return accessError;

        var partnerError = await ValidateExternalPropertyPartnerAsync(context.OrganizationId, context.PartnerVendorId);
        if (partnerError != null)
            return partnerError;

        var response = new ExternalOwnerBatchResponseDto();
        for (var index = 0; index < owners.Count; index++)
        {
            var owner = owners[index];
            var item = new ExternalOwnerBatchItemResultDto
            {
                Index = index,
                Email = owner.Email.Trim()
            };

            try
            {
                var result = await _externalPropertyOwnerContactResolver.UpsertOwnerAsync(
                    owner,
                    context.OrganizationId,
                    context.OfficeId,
                    SystemUserId,
                    $"Owners[{index}]");

                item.Success = result.Success;
                item.Updated = result.Updated;
                item.ContactId = result.Contact?.ContactId;
                item.ContactCode = result.Contact?.ContactCode;
                item.ErrorMessage = result.ErrorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExternalOwnerTrace] Error upserting owner {Email}", item.Email);
                item.Success = false;
                item.ErrorMessage = "An error occurred while saving the owner";
            }

            await _externalPropertyUploadLogService.LogExternalApiAttemptAsync(
                new ExternalPropertyApiAttemptLog
                {
                    OrganizationId = context.OrganizationId,
                    OfficeId = context.OfficeId,
                    VendorId = context.PartnerVendorId,
                    EventType = PropertyUploadLogEvents.OwnerUpsert,
                    Operation = PropertyUploadLogOperations.UpsertOwner,
                    Detail = item.Success
                        ? $"{(item.Updated ? "Updated" : "Created")} owner {item.ContactCode} {item.Email}."
                        : item.ErrorMessage
                },
                item.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
                item.ErrorMessage);

            response.Results.Add(item);
        }

        response.SuccessCount = response.Results.Count(result => result.Success);
        response.FailureCount = response.Results.Count - response.SuccessCount;
        return Ok(response);
    }

    private static (bool Success, ExternalPropertyIntakeContext? Context, List<ExternalPropertyContactDto>? Owners, string? ErrorMessage) TryParseExternalOwners(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, null, "Owner data is required");

        var errors = new List<string>();
        ExternalPropertyIntakeJson.CollectIntakeContextErrors(body, errors, out var context);

        var owners = new List<ExternalPropertyContactDto>();
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "owners", out var ownersElement))
            errors.Add("Owners must contain at least one item");
        else if (ownersElement.ValueKind != JsonValueKind.Array)
            errors.Add(ownersElement.ValueKind == JsonValueKind.Object
                ? "Owners must be a JSON array. Received a single object; wrap the owner in an array."
                : $"Owners must be a JSON array. Received {ExternalPropertyIntakeErrors.Describe(ownersElement)}.");
        else
        {
            if (ownersElement.GetArrayLength() == 0)
                errors.Add("Owners must contain at least one item");
            if (ownersElement.GetArrayLength() > MaxExternalOwnersPerRequest)
                errors.Add($"Owners cannot exceed {MaxExternalOwnersPerRequest} items per request");

            var index = 0;
            foreach (var ownerElement in ownersElement.EnumerateArray())
            {
                var prefix = $"Owners[{index}]";
                var itemErrors = new List<string>();
                ExternalPropertyIntakeErrors.CollectContactElement(ownerElement, prefix, itemErrors);
                if (itemErrors.Count == 0)
                {
                    try
                    {
                        var owner = ExternalPropertyIntakeJson.DeserializeContact(ownerElement);
                        itemErrors.AddRange(owner.CollectErrors(prefix));
                        if (itemErrors.Count == 0)
                            owners.Add(owner);
                    }
                    catch (JsonException ex)
                    {
                        itemErrors.Add(ExternalPropertyIntakeErrors.FromJsonException(ex, ownerElement, prefix));
                    }
                }

                errors.AddRange(itemErrors);
                index++;
            }
        }

        if (errors.Count > 0 || context == null)
            return (false, context, null, errors.Count > 0 ? ExternalPropertyIntakeErrors.Join(errors) : "Invalid request data");

        return (true, context, owners, null);
    }
}
