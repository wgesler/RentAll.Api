using RentAll.Api.Dtos.External;
using System.Text.Json;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class CreateExternalReservationRequestDto
{
    public const int MaxReservationsPerRequest = 50;

    public static (bool Success, ExternalPropertyIntakeContext? Context, List<CreateExternalReservationDto>? Reservations, string? ErrorMessage) TryParseFromBody(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, null, "Reservation data is required");

        var errors = new List<string>();
        var organizationOk = ExternalPropertyIntakeJson.TryParseRequiredOrganizationId(body, out var organizationId, out var organizationError);
        if (!organizationOk && !string.IsNullOrWhiteSpace(organizationError))
            errors.Add(organizationError);
        var officeOk = ExternalPropertyIntakeJson.TryParseRequiredOfficeId(body, out var officeId, out var officeError);
        if (!officeOk && !string.IsNullOrWhiteSpace(officeError))
            errors.Add(officeError);
        var vendorOk = ExternalPropertyIntakeJson.TryParseRequiredVendorId(body, out var vendorId, out var vendorError);
        if (!vendorOk && !string.IsNullOrWhiteSpace(vendorError))
            errors.Add(vendorError);

        ExternalPropertyIntakeContext? context = organizationOk && officeOk && vendorOk
            ? new ExternalPropertyIntakeContext
            {
                OrganizationId = organizationId,
                OfficeId = officeId,
                PartnerVendorId = vendorId
            }
            : null;

        var reservations = new List<CreateExternalReservationDto>();
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "reservations", out var reservationsElement))
            errors.Add("Reservations must contain at least one item");
        else if (reservationsElement.ValueKind != JsonValueKind.Array)
            errors.Add(reservationsElement.ValueKind == JsonValueKind.Object
                ? "Reservations must be a JSON array. Received a single object; wrap the reservation in an array."
                : $"Reservations must be a JSON array. Received {ExternalIntakeErrors.Describe(reservationsElement)}.");
        else
        {
            if (reservationsElement.GetArrayLength() == 0)
                errors.Add("Reservations must contain at least one item");
            if (reservationsElement.GetArrayLength() > MaxReservationsPerRequest)
                errors.Add($"Reservations cannot exceed {MaxReservationsPerRequest} items per request");

            var index = 0;
            foreach (var reservationElement in reservationsElement.EnumerateArray())
            {
                var prefix = $"Reservations[{index}]";
                var itemErrors = CreateExternalReservationDto.CollectFromJson(reservationElement, prefix);
                try
                {
                    var reservationDto = ExternalIntakeErrors.Deserialize<CreateExternalReservationDto>(reservationElement);
                    if (reservationDto == null)
                        itemErrors.Add($"{prefix}: Reservation data is required");
                    else
                    {
                        foreach (var error in reservationDto.CollectErrors())
                            itemErrors.Add(error.StartsWith(prefix, StringComparison.Ordinal) ? error : $"{prefix}.{error}");

                        if (itemErrors.Count == 0)
                            reservations.Add(reservationDto);
                    }
                }
                catch (JsonException ex)
                {
                    itemErrors.Add($"{prefix}: {ExternalIntakeErrors.FromJsonException(ex, reservationElement)}");
                }

                errors.AddRange(itemErrors);
                index++;
            }
        }

        if (errors.Count > 0)
            return (false, null, null, ExternalIntakeErrors.Join(errors));

        return (true, context, reservations, null);
    }
}
