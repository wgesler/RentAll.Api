namespace RentAll.Api.Dtos.Common;

public class FeatureFlagsResponseDto
{
    public Dictionary<string, bool> FeatureFlags { get; set; } = new Dictionary<string, bool>();

    public FeatureFlagsResponseDto()
    {
    }

    public FeatureFlagsResponseDto(IReadOnlyDictionary<string, bool> featureFlags)
    {
        FeatureFlags = featureFlags.ToDictionary(
            entry => ToCamelCase(entry.Key),
            entry => entry.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
            return value;

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
