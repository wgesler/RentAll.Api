using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentAll.Api.Dtos.Properties.Properties;

public sealed record ExternalPropertyAgreementValues(int? AgreementType, decimal? AgreementOwnerFlatRate, decimal? AgreementOwnerSplit, decimal? AgreementOfficeSplit)
{
    public bool HasAny => AgreementType.HasValue || AgreementOwnerFlatRate.HasValue || AgreementOwnerSplit.HasValue || AgreementOfficeSplit.HasValue;
}

public static class ExternalPropertyAgreementIntake
{
    public const string AgreementTypeAllowed = "0=FlatRate, 1=Percentage, or 2=Minimum";

    public static void AddRangeErrors(List<string> errors, int? agreementType, decimal? agreementOwnerFlatRate, decimal? agreementOwnerSplit, decimal? agreementOfficeSplit)
    {
        if (agreementType.HasValue && !Enum.IsDefined(typeof(ManagementFeeType), agreementType.Value))
            errors.Add($"agreementType must be {AgreementTypeAllowed}. Received {agreementType.Value}.");

        if (agreementOwnerFlatRate is < 0)
            errors.Add($"agreementOwnerFlatRate must be >= 0. Received {agreementOwnerFlatRate}.");

        if (agreementOwnerSplit is < 0 or > 100)
            errors.Add($"agreementOwnerSplit must be between 0 and 100. Received {agreementOwnerSplit}.");

        if (agreementOfficeSplit is < 0 or > 100)
            errors.Add($"agreementOfficeSplit must be between 0 and 100. Received {agreementOfficeSplit}.");

        if (agreementOwnerSplit.HasValue && agreementOfficeSplit.HasValue && Math.Abs(agreementOwnerSplit.Value + agreementOfficeSplit.Value - 100m) > 0.01m)
            errors.Add("agreementOwnerSplit and agreementOfficeSplit must sum to 100 when both are provided.");
    }

    public static void CollectAgreementType(JsonElement body, string prefix, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "agreementType", out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryReadElement(element, out _))
            errors.Add($"{prefix}.agreementType must be {AgreementTypeAllowed}. Received {ExternalPropertyIntakeErrors.Describe(element)}.");
    }

    public static (bool Success, ExternalPropertyAgreementValues Values, string? ErrorMessage) TryRead(JsonElement body)
    {
        var errors = new List<string>();
        int? agreementType = null;
        decimal? agreementOwnerFlatRate = null;
        decimal? agreementOwnerSplit = null;
        decimal? agreementOfficeSplit = null;

        if (ExternalPropertyIntakeJson.TryGetProperty(body, "agreementType", out var typeElement) && typeElement.ValueKind != JsonValueKind.Null)
        {
            if (!TryReadElement(typeElement, out var typeValue))
                errors.Add($"agreementType must be {AgreementTypeAllowed}. Received {ExternalPropertyIntakeErrors.Describe(typeElement)}.");
            else
                agreementType = typeValue;
        }

        if (!TryReadOptionalDecimal(body, "agreementOwnerFlatRate", errors, out agreementOwnerFlatRate))
            agreementOwnerFlatRate = null;

        if (!TryReadOptionalDecimal(body, "agreementOwnerSplit", errors, out agreementOwnerSplit))
            agreementOwnerSplit = null;

        if (!TryReadOptionalDecimal(body, "agreementOfficeSplit", errors, out agreementOfficeSplit))
            agreementOfficeSplit = null;

        AddRangeErrors(errors, agreementType, agreementOwnerFlatRate, agreementOwnerSplit, agreementOfficeSplit);
        if (errors.Count > 0)
            return (false, new ExternalPropertyAgreementValues(null, null, null, null), ExternalPropertyIntakeErrors.Join(errors));

        return (true, new ExternalPropertyAgreementValues(agreementType, agreementOwnerFlatRate, agreementOwnerSplit, agreementOfficeSplit), null);
    }

    public static bool TryReadToken(ref Utf8JsonReader reader, out int value)
    {
        value = 0;
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out value))
            return Enum.IsDefined(typeof(ManagementFeeType), value);

        if (reader.TokenType != JsonTokenType.String)
            return false;

        return TryParseAgreementTypeText(reader.GetString(), out value);
    }

    private static bool TryReadElement(JsonElement element, out int value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
            return Enum.IsDefined(typeof(ManagementFeeType), value);

        if (element.ValueKind != JsonValueKind.String)
            return false;

        return TryParseAgreementTypeText(element.GetString(), out value);
    }

    private static bool TryParseAgreementTypeText(string? raw, out int value)
    {
        value = 0;
        var text = raw?.Trim() ?? string.Empty;
        if (text.Length == 0)
            return false;

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return Enum.IsDefined(typeof(ManagementFeeType), value);

        if (!Enum.TryParse<ManagementFeeType>(text, true, out var parsed) || !Enum.IsDefined(parsed))
            return false;

        value = (int)parsed;
        return true;
    }

    private static bool TryReadOptionalDecimal(JsonElement body, string field, List<string> errors, out decimal? value)
    {
        value = null;
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return true;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            value = number;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString()?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        errors.Add($"{field} must be a number. Received {ExternalPropertyIntakeErrors.Describe(element)}.");
        return false;
    }
}

public sealed class FlexibleManagementFeeTypeJsonConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (ExternalPropertyAgreementIntake.TryReadToken(ref reader, out var value))
            return value;

        throw new JsonException($"agreementType must be {ExternalPropertyAgreementIntake.AgreementTypeAllowed}.");
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
