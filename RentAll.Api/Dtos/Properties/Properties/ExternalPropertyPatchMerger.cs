using System.Text.Json;

namespace RentAll.Api.Dtos.Properties.Properties;

public sealed class ExternalPropertyKeyDto
{
    public Guid OrganizationId { get; init; }
    public int OfficeId { get; init; }
    public Guid VendorId { get; init; }
    public string PropertyCode { get; init; } = string.Empty;
}

public static class ExternalPropertyPatchMerger
{
    public static (bool Success, ExternalPropertyKeyDto? Keys, string? ErrorMessage) TryParseRequiredKeys(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "Property data is required");

        if (!TryGetProperty(body, "organizationId", out var organizationIdElement))
            return (false, null, "OrganizationId is required");

        if (!organizationIdElement.TryGetGuid(out var organizationId) || organizationId == Guid.Empty)
            return (false, null, "OrganizationId is required");

        if (!TryGetProperty(body, "officeId", out var officeIdElement) || officeIdElement.ValueKind != JsonValueKind.Number || !officeIdElement.TryGetInt32(out var officeId))
            return (false, null, "OfficeId is required");

        if (officeId <= 0)
            return (false, null, "OfficeId is required");

        if (!TryGetProperty(body, "vendorId", out var vendorIdElement))
            return (false, null, "VendorId is required");

        if (!vendorIdElement.TryGetGuid(out var vendorId) || vendorId == Guid.Empty)
            return (false, null, "VendorId is required");

        if (!TryGetProperty(body, "propertyCode", out var propertyCodeElement) || propertyCodeElement.ValueKind != JsonValueKind.String)
            return (false, null, "PropertyCode is required");

        var propertyCode = propertyCodeElement.GetString()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(propertyCode))
            return (false, null, "PropertyCode is required");

        return (true, new ExternalPropertyKeyDto
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            VendorId = vendorId,
            PropertyCode = propertyCode
        }, null);
    }

    public static bool TryGetOrganizationContextForLogging(JsonElement body, out Guid organizationId, out int officeId, out Guid vendorId, out string propertyCode)
    {
        organizationId = Guid.Empty;
        officeId = 0;
        vendorId = Guid.Empty;
        propertyCode = string.Empty;
        if (body.ValueKind != JsonValueKind.Object)
            return false;

        if (TryGetProperty(body, "organizationId", out var organizationIdElement)
            && organizationIdElement.TryGetGuid(out organizationId)
            && organizationId != Guid.Empty
            && TryGetProperty(body, "officeId", out var officeIdElement)
            && officeIdElement.ValueKind == JsonValueKind.Number
            && officeIdElement.TryGetInt32(out officeId)
            && TryGetProperty(body, "vendorId", out var vendorIdElement)
            && vendorIdElement.TryGetGuid(out vendorId)
            && TryGetProperty(body, "propertyCode", out var propertyCodeElement)
            && propertyCodeElement.ValueKind == JsonValueKind.String)
        {
            propertyCode = propertyCodeElement.GetString()?.Trim() ?? string.Empty;
        }

        return organizationId != Guid.Empty;
    }

    public static (bool Success, UpdatePropertyDto? UpdateDto, string? ErrorMessage) TryMerge(Property existing, JsonElement body, ExternalPropertyKeyDto keys)
    {
        if (keys.OrganizationId != existing.OrganizationId)
            return (false, null, "Property not found");

        var presentFields = GetPresentFields(body);
        var updateDto = UpdatePropertyDto.FromProperty(existing);
        updateDto.OfficeId = keys.OfficeId;
        updateDto.VendorId = keys.VendorId;

        if (presentFields.Contains("address1"))
        {
            if (!TryGetTrimmedString(body, "address1", out var address1) || string.IsNullOrWhiteSpace(address1))
                return (false, null, "Address1 cannot be empty when provided");

            updateDto.Address1 = address1;
        }

        if (presentFields.Contains("address2"))
            updateDto.Address2 = TryGetTrimmedString(body, "address2", out var address2) ? TrimOrNull(address2) : null;

        if (presentFields.Contains("suite"))
            updateDto.Suite = TryGetTrimmedString(body, "suite", out var suite) ? TrimOrNull(suite) : null;

        if (presentFields.Contains("city"))
        {
            if (!TryGetTrimmedString(body, "city", out var city) || string.IsNullOrWhiteSpace(city))
                return (false, null, "City cannot be empty when provided");

            updateDto.City = city;
        }

        if (presentFields.Contains("state"))
        {
            if (!TryGetTrimmedString(body, "state", out var state) || string.IsNullOrWhiteSpace(state))
                return (false, null, "State cannot be empty when provided");

            updateDto.State = state;
        }

        if (presentFields.Contains("zip"))
        {
            if (!TryGetTrimmedString(body, "zip", out var zip) || string.IsNullOrWhiteSpace(zip))
                return (false, null, "Zip cannot be empty when provided");

            updateDto.Zip = zip;
        }

        if (presentFields.Contains("bedrooms"))
        {
            if (!TryGetInt(body, "bedrooms", out var bedrooms) || bedrooms < 0)
                return (false, null, "Bedrooms must be >= 0");

            updateDto.Bedrooms = bedrooms;
        }

        if (presentFields.Contains("bathrooms"))
        {
            if (!TryGetDecimal(body, "bathrooms", out var bathrooms) || bathrooms < 0)
                return (false, null, "Bathrooms must be >= 0");

            updateDto.Bathrooms = bathrooms;
        }

        if (presentFields.Contains("accommodates"))
        {
            if (!TryGetInt(body, "accommodates", out var accommodates) || accommodates < 0)
                return (false, null, "Accommodates must be >= 0");

            updateDto.Accommodates = accommodates;
        }

        if (presentFields.Contains("squareFeet"))
        {
            if (!TryGetInt(body, "squareFeet", out var squareFeet) || squareFeet < 0)
                return (false, null, "SquareFeet must be >= 0");

            updateDto.SquareFeet = squareFeet;
        }

        if (presentFields.Contains("propertyStyleId"))
        {
            if (!TryGetInt(body, "propertyStyleId", out var propertyStyleId) || !Enum.IsDefined(typeof(PropertyStyle), propertyStyleId))
                return (false, null, $"Invalid PropertyStyleId value: {propertyStyleId}");

            updateDto.PropertyStyleId = propertyStyleId;
        }

        if (presentFields.Contains("propertyTypeId"))
        {
            if (!TryGetInt(body, "propertyTypeId", out var propertyTypeId) || !Enum.IsDefined(typeof(PropertyType), propertyTypeId))
                return (false, null, $"Invalid PropertyTypeId value: {propertyTypeId}");

            if (propertyTypeId == (int)PropertyType.Unspecified)
                return (false, null, "PropertyTypeId cannot be Unspecified when provided");

            updateDto.PropertyTypeId = propertyTypeId;
        }

        if (presentFields.Contains("monthlyRate"))
        {
            if (!TryGetDecimal(body, "monthlyRate", out var monthlyRate) || monthlyRate < 0)
                return (false, null, "MonthlyRate must be >= 0");

            updateDto.MonthlyRate = monthlyRate;
        }

        if (presentFields.Contains("dailyRate"))
        {
            if (!TryGetDecimal(body, "dailyRate", out var dailyRate) || dailyRate < 0)
                return (false, null, "DailyRate must be >= 0");

            updateDto.DailyRate = dailyRate;
        }

        if (presentFields.Contains("departureFee"))
        {
            if (!TryGetDecimal(body, "departureFee", out var departureFee) || departureFee < 0)
                return (false, null, "DepartureFee must be >= 0");

            updateDto.DepartureFee = departureFee;
        }

        if (presentFields.Contains("maidServiceFee"))
        {
            if (!TryGetDecimal(body, "maidServiceFee", out var maidServiceFee) || maidServiceFee < 0)
                return (false, null, "MaidServiceFee must be >= 0");

            updateDto.MaidServiceFee = maidServiceFee;
        }

        if (presentFields.Contains("petFee"))
        {
            if (!TryGetDecimal(body, "petFee", out var petFee) || petFee < 0)
                return (false, null, "PetFee must be >= 0");

            updateDto.PetFee = petFee;
        }

        if (presentFields.Contains("externalCalendar"))
            updateDto.ExternalCalendar = TryGetTrimmedString(body, "externalCalendar", out var externalCalendar) ? TrimOrNull(externalCalendar) : null;

        if (presentFields.Contains("description"))
            updateDto.Description = TryGetTrimmedString(body, "description", out var description) ? description : null;

        if (presentFields.Contains("isActive"))
        {
            if (!TryGetBool(body, "isActive", out var isActive))
                return (false, null, "IsActive must be a boolean when provided");

            updateDto.IsActive = isActive;
        }

        if (presentFields.Contains("minStay"))
        {
            if (!TryGetInt(body, "minStay", out var minStay) || minStay < 0)
                return (false, null, "MinStay must be >= 0");

            updateDto.MinStay = minStay;
        }

        if (presentFields.Contains("maxStay"))
        {
            if (!TryGetInt(body, "maxStay", out var maxStay) || maxStay < 0)
                return (false, null, "MaxStay must be >= 0");

            updateDto.MaxStay = maxStay;
        }

        if (presentFields.Contains("checkInTimeId"))
        {
            if (!TryGetInt(body, "checkInTimeId", out var checkInTimeId) || !Enum.IsDefined(typeof(CheckInTime), checkInTimeId))
                return (false, null, $"Invalid CheckInTimeId value: {checkInTimeId}");

            updateDto.CheckInTimeId = checkInTimeId;
        }

        if (presentFields.Contains("checkOutTimeId"))
        {
            if (!TryGetInt(body, "checkOutTimeId", out var checkOutTimeId) || !Enum.IsDefined(typeof(CheckOutTime), checkOutTimeId))
                return (false, null, $"Invalid CheckOutTimeId value: {checkOutTimeId}");

            updateDto.CheckOutTimeId = checkOutTimeId;
        }

        var bedroomValidation = ApplyBedroomIdPatch(body, presentFields, "bedroomId1", 1, value => updateDto.BedroomId1 = value)
            ?? ApplyBedroomIdPatch(body, presentFields, "bedroomId2", 2, value => updateDto.BedroomId2 = value)
            ?? ApplyBedroomIdPatch(body, presentFields, "bedroomId3", 3, value => updateDto.BedroomId3 = value)
            ?? ApplyBedroomIdPatch(body, presentFields, "bedroomId4", 4, value => updateDto.BedroomId4 = value);
        if (bedroomValidation != null)
            return (false, null, bedroomValidation);

        if (presentFields.Contains("neighborhood"))
            updateDto.Neighborhood = TryGetTrimmedString(body, "neighborhood", out var neighborhood) ? TrimOrNull(neighborhood) : null;

        if (presentFields.Contains("crossStreet"))
            updateDto.CrossStreet = TryGetTrimmedString(body, "crossStreet", out var crossStreet) ? TrimOrNull(crossStreet) : null;

        if (presentFields.Contains("view"))
            updateDto.View = TryGetTrimmedString(body, "view", out var view) ? TrimOrNull(view) : null;

        if (presentFields.Contains("mailbox"))
            updateDto.Mailbox = TryGetTrimmedString(body, "mailbox", out var mailbox) ? TrimOrNull(mailbox) : null;

        if (presentFields.Contains("poundLimit"))
            updateDto.PoundLimit = TryGetTrimmedString(body, "poundLimit", out var poundLimit) ? poundLimit.Trim() : string.Empty;

        if (presentFields.Contains("parkingNotes"))
            updateDto.ParkingNotes = TryGetTrimmedString(body, "parkingNotes", out var parkingNotes) ? TrimOrNull(parkingNotes) : null;

        if (presentFields.Contains("amenities"))
            updateDto.Amenities = TryGetTrimmedString(body, "amenities", out var amenities) ? TrimOrNull(amenities) : null;

        ApplyBoolPatch(body, presentFields, "unfurnished", value => updateDto.Unfurnished = value);
        ApplyBoolPatch(body, presentFields, "heating", value => updateDto.Heating = value);
        ApplyBoolPatch(body, presentFields, "ac", value => updateDto.Ac = value);
        ApplyBoolPatch(body, presentFields, "elevator", value => updateDto.Elevator = value);
        ApplyBoolPatch(body, presentFields, "security", value => updateDto.Security = value);
        ApplyBoolPatch(body, presentFields, "gated", value => updateDto.Gated = value);
        ApplyBoolPatch(body, presentFields, "petsAllowed", value => updateDto.PetsAllowed = value);
        ApplyBoolPatch(body, presentFields, "dogsOkay", value => updateDto.DogsOkay = value);
        ApplyBoolPatch(body, presentFields, "catsOkay", value => updateDto.CatsOkay = value);
        ApplyBoolPatch(body, presentFields, "smoking", value => updateDto.Smoking = value);
        ApplyBoolPatch(body, presentFields, "parking", value => updateDto.Parking = value);
        ApplyBoolPatch(body, presentFields, "kitchen", value => updateDto.Kitchen = value);
        ApplyBoolPatch(body, presentFields, "oven", value => updateDto.Oven = value);
        ApplyBoolPatch(body, presentFields, "refrigerator", value => updateDto.Refrigerator = value);
        ApplyBoolPatch(body, presentFields, "microwave", value => updateDto.Microwave = value);
        ApplyBoolPatch(body, presentFields, "dishwasher", value => updateDto.Dishwasher = value);
        ApplyBoolPatch(body, presentFields, "bathtub", value => updateDto.Bathtub = value);
        ApplyBoolPatch(body, presentFields, "washerDryerInUnit", value => updateDto.WasherDryerInUnit = value);
        ApplyBoolPatch(body, presentFields, "washerDryerInBldg", value => updateDto.WasherDryerInBldg = value);
        ApplyBoolPatch(body, presentFields, "tv", value => updateDto.Tv = value);
        ApplyBoolPatch(body, presentFields, "cable", value => updateDto.Cable = value);
        ApplyBoolPatch(body, presentFields, "dvd", value => updateDto.Dvd = value);
        ApplyBoolPatch(body, presentFields, "streaming", value => updateDto.Streaming = value);
        ApplyBoolPatch(body, presentFields, "fastInternet", value => updateDto.FastInternet = value);
        ApplyBoolPatch(body, presentFields, "deck", value => updateDto.Deck = value);
        ApplyBoolPatch(body, presentFields, "patio", value => updateDto.Patio = value);
        ApplyBoolPatch(body, presentFields, "yard", value => updateDto.Yard = value);
        ApplyBoolPatch(body, presentFields, "garden", value => updateDto.Garden = value);
        ApplyBoolPatch(body, presentFields, "commonPool", value => updateDto.CommonPool = value);
        ApplyBoolPatch(body, presentFields, "privatePool", value => updateDto.PrivatePool = value);
        ApplyBoolPatch(body, presentFields, "jacuzzi", value => updateDto.Jacuzzi = value);
        ApplyBoolPatch(body, presentFields, "sauna", value => updateDto.Sauna = value);
        ApplyBoolPatch(body, presentFields, "gym", value => updateDto.Gym = value);

        return (true, updateDto, null);
    }

    private static string? ApplyBedroomIdPatch(JsonElement body, HashSet<string> presentFields, string fieldName, int bedroomNumber, Action<int> apply)
    {
        if (!presentFields.Contains(fieldName))
            return null;

        if (!TryGetProperty(body, fieldName, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            apply(0);
            return null;
        }

        if (!element.TryGetInt32(out var bedroomId))
            return $"Invalid BedroomId{bedroomNumber} value";

        if (!Enum.IsDefined(typeof(BedSizeType), bedroomId))
            return $"Invalid BedroomId{bedroomNumber} value: {bedroomId}";

        apply(bedroomId);
        return null;
    }

    private static void ApplyBoolPatch(JsonElement body, HashSet<string> presentFields, string fieldName, Action<bool> apply)
    {
        if (!presentFields.Contains(fieldName))
            return;

        if (TryGetBool(body, fieldName, out var value))
            apply(value);
    }

    private static HashSet<string> GetPresentFields(JsonElement body)
    {
        return body.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryGetProperty(JsonElement body, string name, out JsonElement value)
    {
        foreach (var property in body.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetTrimmedString(JsonElement body, string name, out string value)
    {
        value = string.Empty;
        if (!TryGetProperty(body, name, out var element) || element.ValueKind != JsonValueKind.String)
            return false;

        value = element.GetString()?.Trim() ?? string.Empty;
        return true;
    }

    private static bool TryGetInt(JsonElement body, string name, out int value)
    {
        value = 0;
        if (!TryGetProperty(body, name, out var element) || element.ValueKind != JsonValueKind.Number)
            return false;

        return element.TryGetInt32(out value);
    }

    private static bool TryGetDecimal(JsonElement body, string name, out decimal value)
    {
        value = 0;
        if (!TryGetProperty(body, name, out var element) || element.ValueKind != JsonValueKind.Number)
            return false;

        return element.TryGetDecimal(out value);
    }

    private static bool TryGetBool(JsonElement body, string name, out bool value)
    {
        value = false;
        if (!TryGetProperty(body, name, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        return false;
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
