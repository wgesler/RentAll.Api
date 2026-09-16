using RentAll.Api.Dtos.Properties.Properties;
using RentAll.Api.Services;
using System.Text.Json;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
    private async Task<IActionResult> RejectExternalPropertyRequestAsync(
        JsonElement body,
        string operation,
        string eventType,
        string errorMessage,
        ExternalPropertyIntakeContext? context = null)
    {
        await LogExternalPropertyParseFailureAsync(body, operation, eventType, errorMessage, context);
        return BadRequest(errorMessage);
    }

    private async Task LogExternalPropertyParseFailureAsync(
        JsonElement body,
        string operation,
        string eventType,
        string errorMessage,
        ExternalPropertyIntakeContext? context = null)
    {
        var attempt = BuildExternalPropertyParseFailureAttempt(body, operation, eventType, errorMessage, context);
        if (attempt.OrganizationId == Guid.Empty)
            return;

        await _externalPropertyUploadLogService.LogExternalApiAttemptAsync(
            attempt,
            StatusCodes.Status400BadRequest,
            errorMessage);
    }

    private static ExternalPropertyApiAttemptLog BuildExternalPropertyParseFailureAttempt(
        JsonElement body,
        string operation,
        string eventType,
        string errorMessage,
        ExternalPropertyIntakeContext? context)
    {
        var organizationId = context?.OrganizationId ?? Guid.Empty;
        int? officeId = context?.OfficeId;
        Guid? vendorId = context?.PartnerVendorId;

        if (context == null)
        {
            ExternalPropertyIntakeJson.TryParseRequiredOrganizationId(body, out organizationId, out _);
            if (ExternalPropertyIntakeJson.TryGetProperty(body, "officeId", out var officeIdElement)
                && ExternalPropertyIntakeJson.TryCoerceOfficeId(officeIdElement, out var coercedOfficeId))
                officeId = coercedOfficeId;

            if (ExternalPropertyIntakeJson.TryParseRequiredVendorId(body, out var parsedVendorId, out _))
                vendorId = parsedVendorId;
        }

        return new ExternalPropertyApiAttemptLog
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            VendorId = vendorId,
            Operation = operation,
            EventType = eventType,
            Detail = errorMessage,
            PropertyCode = ExternalPropertyIntakeJson.TryGetPropertyCodeForLogging(body)
        };
    }

    private async Task<IActionResult> CompleteExternalPropertyAttemptAsync(IActionResult result, ExternalPropertyApiAttemptLog attempt, string? detailOverride = null, Guid? propertyId = null)
    {
        if (attempt.OrganizationId == Guid.Empty)
            return result;

        var statusCode = ResolveExternalPropertyAttemptStatusCode(result);
        var detail = detailOverride ?? attempt.Detail ?? ExtractExternalPropertyAttemptDetail(result);
        await _externalPropertyUploadLogService.LogExternalApiAttemptAsync(new ExternalPropertyApiAttemptLog
        {
            OrganizationId = attempt.OrganizationId,
            OfficeId = attempt.OfficeId,
            VendorId = attempt.VendorId,
            PropertyId = propertyId ?? attempt.PropertyId,
            PropertyCode = attempt.PropertyCode,
            ImportId = attempt.ImportId,
            EventType = attempt.EventType,
            Operation = attempt.Operation,
            Detail = detail
        }, statusCode, detail);
        return result;
    }

    private static int ResolveExternalPropertyAttemptStatusCode(IActionResult result)
    {
        return result switch
        {
            ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            _ => StatusCodes.Status200OK
        };
    }

    private static string? ExtractExternalPropertyAttemptDetail(IActionResult result)
    {
        if (result is ObjectResult { Value: string message })
            return message;

        return null;
    }
}
