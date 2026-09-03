using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Services;

namespace RentAll.Api.Controllers;

public partial class PropertyController
{
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
