using RentAll.Domain;
using System.Globalization;
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
    public static (bool Success, UpdatePropertyDto? UpdateDto, string? ErrorMessage) TryMerge(Property existing, JsonElement body, ExternalPropertyKeyDto keys)
    {
        if (keys.OrganizationId != existing.OrganizationId)
            return (false, null, "Property not found");

        var presentFields = GetPresentFields(body);
        var errors = new List<string>();
        var updateDto = UpdatePropertyDto.FromProperty(existing);
        updateDto.OfficeId = keys.OfficeId;

        if (presentFields.Contains("address1"))
        {
            if (!TryGetTrimmedString(body, "address1", out var address1) || string.IsNullOrWhiteSpace(address1))
                errors.Add($"address1 cannot be empty when provided. Received {ExternalPropertyIntakeErrors.DescribeField(body, "address1")}.");

            updateDto.Address1 = address1;
        }

        if (presentFields.Contains("address2"))
            updateDto.Address2 = TryGetTrimmedString(body, "address2", out var address2) ? TrimOrNull(address2) : null;

        if (presentFields.Contains("suite"))
            updateDto.Suite = TryGetTrimmedString(body, "suite", out var suite) ? TrimOrNull(suite) : null;

        if (presentFields.Contains("city"))
        {
            if (!TryGetTrimmedString(body, "city", out var city) || string.IsNullOrWhiteSpace(city))
                errors.Add($"city cannot be empty when provided. Received {ExternalPropertyIntakeErrors.DescribeField(body, "city")}.");

            updateDto.City = city;
        }

        if (presentFields.Contains("state"))
        {
            if (!TryGetTrimmedString(body, "state", out var state) || string.IsNullOrWhiteSpace(state))
                errors.Add($"state cannot be empty when provided. Received {ExternalPropertyIntakeErrors.DescribeField(body, "state")}.");
            else if (!UsStateCode.IsRecognized(state))
                errors.Add($"state must be a 2-letter US code or full state name. Received \"{state}\".");
            else
                updateDto.State = UsStateCode.Normalize(state) ?? state;
        }

        if (presentFields.Contains("zip"))
        {
            if (!TryGetTrimmedString(body, "zip", out var zip) || string.IsNullOrWhiteSpace(zip))
                errors.Add($"zip cannot be empty when provided. Received {ExternalPropertyIntakeErrors.DescribeField(body, "zip")}.");
            else if (zip.Length > 10)
                errors.Add($"zip is {zip.Length} characters; max is 10. Received \"{zip}\".");

            updateDto.Zip = zip;
        }

        if (presentFields.Contains("bedrooms"))
        {
            if (!TryGetInt(body, "bedrooms", out var bedrooms) || bedrooms < 0)
                errors.Add($"bedrooms must be an integer >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "bedrooms")}.");

            updateDto.Bedrooms = bedrooms;
        }

        if (presentFields.Contains("bathrooms"))
        {
            if (!TryGetDecimal(body, "bathrooms", out var bathrooms) || bathrooms < 0)
                errors.Add($"bathrooms must be a number >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "bathrooms")}.");

            updateDto.Bathrooms = bathrooms;
        }

        if (presentFields.Contains("accommodates"))
        {
            if (!TryGetInt(body, "accommodates", out var accommodates) || accommodates < 0)
                errors.Add($"accommodates must be an integer >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "accommodates")}.");

            updateDto.Accommodates = accommodates;
        }

        if (presentFields.Contains("squareFeet"))
        {
            if (!TryGetInt(body, "squareFeet", out var squareFeet) || squareFeet < 0)
                errors.Add($"squareFeet must be an integer >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "squareFeet")}.");

            updateDto.SquareFeet = squareFeet;
        }

        if (presentFields.Contains("propertyLeaseTypeId"))
        {
            if (!TryGetInt(body, "propertyLeaseTypeId", out var propertyLeaseTypeId) || !Enum.IsDefined(typeof(PropertyLeaseType), propertyLeaseTypeId))
                errors.Add($"propertyLeaseTypeId must be 0=PropertyManagement, 1=Direct, or 2=ThirdParty. Received {ExternalPropertyIntakeErrors.DescribeField(body, "propertyLeaseTypeId")}.");

            updateDto.PropertyLeaseTypeId = propertyLeaseTypeId;
        }

        if (presentFields.Contains("propertyStyleId"))
        {
            if (!TryGetInt(body, "propertyStyleId", out var propertyStyleId) || !Enum.IsDefined(typeof(PropertyStyle), propertyStyleId))
                errors.Add($"propertyStyleId must be 0=Standard, 1=Corporate, or 2=Vacation. Received {ExternalPropertyIntakeErrors.DescribeField(body, "propertyStyleId")}.");

            updateDto.PropertyStyleId = propertyStyleId;
        }

        if (presentFields.Contains("propertyTypeId"))
        {
            if (!TryGetInt(body, "propertyTypeId", out var propertyTypeId) || !Enum.IsDefined(typeof(PropertyType), propertyTypeId) || propertyTypeId == (int)PropertyType.Unspecified)
                errors.Add($"propertyTypeId must be 1-17 (1=Apartment, 8=House, 5=Condo). 0=Unspecified is not allowed. Received {ExternalPropertyIntakeErrors.DescribeField(body, "propertyTypeId")}.");

            updateDto.PropertyTypeId = propertyTypeId;
        }

        if (presentFields.Contains("monthlyRate"))
        {
            if (!TryGetDecimal(body, "monthlyRate", out var monthlyRate) || monthlyRate < 0)
                errors.Add($"monthlyRate must be a number >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "monthlyRate")}.");

            updateDto.MonthlyRate = monthlyRate;
        }

        if (presentFields.Contains("dailyRate"))
        {
            if (!TryGetDecimal(body, "dailyRate", out var dailyRate) || dailyRate < 0)
                errors.Add($"dailyRate must be a number >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "dailyRate")}.");

            updateDto.DailyRate = dailyRate;
        }

        if (presentFields.Contains("departureFee"))
        {
            if (!TryGetDecimal(body, "departureFee", out var departureFee) || departureFee < 0)
                errors.Add($"departureFee must be a number >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "departureFee")}.");

            updateDto.DepartureFee = departureFee;
        }

        if (presentFields.Contains("maidServiceFee"))
        {
            if (!TryGetDecimal(body, "maidServiceFee", out var maidServiceFee) || maidServiceFee < 0)
                errors.Add($"maidServiceFee must be a number >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "maidServiceFee")}.");

            updateDto.MaidServiceFee = maidServiceFee;
        }

        if (presentFields.Contains("petFee"))
        {
            if (!TryGetDecimal(body, "petFee", out var petFee) || petFee < 0)
                errors.Add($"petFee must be a number >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "petFee")}.");

            updateDto.PetFee = petFee;
        }

        if (presentFields.Contains("externalCalendars") || presentFields.Contains("externalCalendar"))
        {
            if (!TryGetExternalCalendars(body, out var calendars, out var calendarError))
                errors.Add(calendarError ?? "externalCalendars must be an array of iCal URLs.");
            else
                updateDto.ExternalCalendars = calendars;
        }

        if (presentFields.Contains("description"))
            updateDto.Description = TryGetTrimmedString(body, "description", out var description) ? description : null;

        if (presentFields.Contains("isActive"))
        {
            if (!TryGetBool(body, "isActive", out var isActive))
                errors.Add($"isActive must be true/false or 0/1. Received {ExternalPropertyIntakeErrors.DescribeField(body, "isActive")}.");

            updateDto.IsActive = isActive;
        }

        if (presentFields.Contains("minStay"))
        {
            if (!TryGetInt(body, "minStay", out var minStay) || minStay < 0)
                errors.Add($"minStay must be an integer >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "minStay")}.");

            updateDto.MinStay = minStay;
        }

        if (presentFields.Contains("maxStay"))
        {
            if (!TryGetInt(body, "maxStay", out var maxStay) || maxStay < 0)
                errors.Add($"maxStay must be an integer >= 0. Received {ExternalPropertyIntakeErrors.DescribeField(body, "maxStay")}.");

            updateDto.MaxStay = maxStay;
        }

        if (presentFields.Contains("checkInTimeId"))
        {
            if (!TryGetInt(body, "checkInTimeId", out var checkInTimeId) || !Enum.IsDefined(typeof(CheckInTime), checkInTimeId))
                errors.Add($"checkInTimeId must be 0=11AM through 6=5PM. Received {ExternalPropertyIntakeErrors.DescribeField(body, "checkInTimeId")}.");

            updateDto.CheckInTimeId = checkInTimeId;
        }

        if (presentFields.Contains("checkOutTimeId"))
        {
            if (!TryGetInt(body, "checkOutTimeId", out var checkOutTimeId) || !Enum.IsDefined(typeof(CheckOutTime), checkOutTimeId))
                errors.Add($"checkOutTimeId must be 1=8AM through 6=1PM. Received {ExternalPropertyIntakeErrors.DescribeField(body, "checkOutTimeId")}.");

            updateDto.CheckOutTimeId = checkOutTimeId;
        }

        AddBedroomIdPatch(body, presentFields, "bedroomId1", value => updateDto.BedroomId1 = value, errors);
        AddBedroomIdPatch(body, presentFields, "bedroomId2", value => updateDto.BedroomId2 = value, errors);
        AddBedroomIdPatch(body, presentFields, "bedroomId3", value => updateDto.BedroomId3 = value, errors);
        AddBedroomIdPatch(body, presentFields, "bedroomId4", value => updateDto.BedroomId4 = value, errors);

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

        ApplyBoolPatch(body, presentFields, "unfurnished", value => updateDto.Unfurnished = value, errors);
        ApplyBoolPatch(body, presentFields, "heating", value => updateDto.Heating = value, errors);
        ApplyBoolPatch(body, presentFields, "ac", value => updateDto.Ac = value, errors);
        ApplyBoolPatch(body, presentFields, "elevator", value => updateDto.Elevator = value, errors);
        ApplyBoolPatch(body, presentFields, "security", value => updateDto.Security = value, errors);
        ApplyBoolPatch(body, presentFields, "gated", value => updateDto.Gated = value, errors);
        ApplyBoolPatch(body, presentFields, "petsAllowed", value => updateDto.PetsAllowed = value, errors);
        ApplyBoolPatch(body, presentFields, "dogsOkay", value => updateDto.DogsOkay = value, errors);
        ApplyBoolPatch(body, presentFields, "catsOkay", value => updateDto.CatsOkay = value, errors);
        ApplyBoolPatch(body, presentFields, "smoking", value => updateDto.Smoking = value, errors);
        ApplyBoolPatch(body, presentFields, "parking", value => updateDto.Parking = value, errors);
        ApplyBoolPatch(body, presentFields, "kitchen", value => updateDto.Kitchen = value, errors);
        ApplyBoolPatch(body, presentFields, "oven", value => updateDto.Oven = value, errors);
        ApplyBoolPatch(body, presentFields, "refrigerator", value => updateDto.Refrigerator = value, errors);
        ApplyBoolPatch(body, presentFields, "microwave", value => updateDto.Microwave = value, errors);
        ApplyBoolPatch(body, presentFields, "dishwasher", value => updateDto.Dishwasher = value, errors);
        ApplyBoolPatch(body, presentFields, "bathtub", value => updateDto.Bathtub = value, errors);
        ApplyBoolPatch(body, presentFields, "washerDryerInUnit", value => updateDto.WasherDryerInUnit = value, errors);
        ApplyBoolPatch(body, presentFields, "washerDryerInBldg", value => updateDto.WasherDryerInBldg = value, errors);
        ApplyBoolPatch(body, presentFields, "tv", value => updateDto.Tv = value, errors);
        ApplyBoolPatch(body, presentFields, "cable", value => updateDto.Cable = value, errors);
        ApplyBoolPatch(body, presentFields, "dvd", value => updateDto.Dvd = value, errors);
        ApplyBoolPatch(body, presentFields, "streaming", value => updateDto.Streaming = value, errors);
        ApplyBoolPatch(body, presentFields, "fastInternet", value => updateDto.FastInternet = value, errors);
        ApplyBoolPatch(body, presentFields, "deck", value => updateDto.Deck = value, errors);
        ApplyBoolPatch(body, presentFields, "patio", value => updateDto.Patio = value, errors);
        ApplyBoolPatch(body, presentFields, "yard", value => updateDto.Yard = value, errors);
        ApplyBoolPatch(body, presentFields, "garden", value => updateDto.Garden = value, errors);
        ApplyBoolPatch(body, presentFields, "commonPool", value => updateDto.CommonPool = value, errors);
        ApplyBoolPatch(body, presentFields, "privatePool", value => updateDto.PrivatePool = value, errors);
        ApplyBoolPatch(body, presentFields, "jacuzzi", value => updateDto.Jacuzzi = value, errors);
        ApplyBoolPatch(body, presentFields, "sauna", value => updateDto.Sauna = value, errors);
        ApplyBoolPatch(body, presentFields, "gym", value => updateDto.Gym = value, errors);

        if (errors.Count > 0)
            return (false, null, ExternalPropertyIntakeErrors.Join(errors));

        return (true, updateDto, null);
    }

    private static void AddBedroomIdPatch(JsonElement body, HashSet<string> presentFields, string fieldName, Action<int> apply, List<string> errors)
    {
        if (!presentFields.Contains(fieldName))
            return;

        if (!TryGetProperty(body, fieldName, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            apply(0);
            return;
        }

        if (!TryCoerceInt32(element, out var bedroomId) || !Enum.IsDefined(typeof(BedSizeType), bedroomId))
        {
            errors.Add($"{fieldName} must be an integer 0-7 (0=Unknown, 1=King, 2=Queen, 3=Double, 4=Twin, 5=TwoTwins, 6=DayBed, 7=SofaBed). Received {ExternalPropertyIntakeErrors.Describe(element)}.");
            return;
        }

        apply(bedroomId);
    }

    private static void ApplyBoolPatch(JsonElement body, HashSet<string> presentFields, string fieldName, Action<bool> apply, List<string> errors)
    {
        if (!presentFields.Contains(fieldName))
            return;

        if (TryGetBool(body, fieldName, out var value))
        {
            apply(value);
            return;
        }

        errors.Add($"{fieldName} must be true/false or 0/1. Received {ExternalPropertyIntakeErrors.DescribeField(body, fieldName)}.");
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

    private static bool TryGetExternalCalendars(JsonElement body, out List<string> calendars, out string? error)
    {
        calendars = [];
        error = null;
        var legacyUrl = TryGetTrimmedString(body, "externalCalendar", out var externalCalendar) ? TrimOrNull(externalCalendar) : null;
        if (!TryGetProperty(body, "externalCalendars", out var element) || element.ValueKind == JsonValueKind.Null)
        {
            calendars = PropertyICalDto.MergeCalendarInputs(null, legacyUrl);
            return true;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            error = $"externalCalendars must be an array. Received {ExternalPropertyIntakeErrors.Describe(element)}.";
            return false;
        }

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var url = item.GetString()?.Trim() ?? string.Empty;
                if (url.Length > 0)
                    calendars.Add(url);
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                error = $"externalCalendars items must be iCal URL strings. Received {ExternalPropertyIntakeErrors.Describe(item)}.";
                return false;
            }

            if (!TryGetProperty(item, "iCalUrl", out var urlElement) && !TryGetProperty(item, "ICalUrl", out urlElement))
                continue;

            var parsedUrl = urlElement.ValueKind == JsonValueKind.String ? urlElement.GetString()?.Trim() ?? string.Empty : string.Empty;
            if (parsedUrl.Length > 0)
                calendars.Add(parsedUrl);
        }

        calendars = PropertyICalDto.MergeCalendarInputs(calendars, legacyUrl);
        return true;
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
        return TryGetProperty(body, name, out var element) && TryCoerceInt32(element, out value);
    }

    private static bool TryGetDecimal(JsonElement body, string name, out decimal value)
    {
        value = 0;
        return TryGetProperty(body, name, out var element) && TryCoerceDecimal(element, out value);
    }

    private static bool TryGetBool(JsonElement body, string name, out bool value)
    {
        value = false;
        return TryGetProperty(body, name, out var element) && TryCoerceBool(element, out value);
    }

    private static bool TryCoerceInt32(JsonElement element, out int value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        return element.ValueKind == JsonValueKind.String
            && int.TryParse(element.GetString()?.Trim(), out value);
    }

    private static bool TryCoerceDecimal(JsonElement element, out decimal value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetDecimal(out value);

        return element.ValueKind == JsonValueKind.String
            && decimal.TryParse(element.GetString()?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryCoerceBool(JsonElement element, out bool value)
    {
        value = false;
        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
            return true;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            if (number == 1)
            {
                value = true;
                return true;
            }

            return number == 0;
        }

        if (element.ValueKind != JsonValueKind.String)
            return false;

        var raw = element.GetString()?.Trim();
        if (bool.TryParse(raw, out value))
            return true;

        if (string.Equals(raw, "1", StringComparison.Ordinal))
        {
            value = true;
            return true;
        }

        return string.Equals(raw, "0", StringComparison.Ordinal);
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
