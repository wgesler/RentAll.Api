using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Infrastructure.Services;

public class FeatureFlagService : IFeatureFlagService
{
    private static readonly string[] KnownFlags = [FeatureFlagKeys.Accounting];

    private readonly IOptionsMonitor<FeatureFlags> _options;
    private readonly ConcurrentDictionary<string, bool> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public FeatureFlagService(IOptionsMonitor<FeatureFlags> options)
    {
        _options = options;
    }

    public IReadOnlyDictionary<string, bool> GetAll()
    {
        return KnownFlags.ToDictionary(flag => flag, IsEnabled, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsEnabled(string featureName)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            return false;

        if (_overrides.TryGetValue(featureName, out var overridden))
            return overridden;

        return featureName switch
        {
            FeatureFlagKeys.Accounting => _options.CurrentValue.Accounting,
            _ => false
        };
    }

    public void Set(string featureName, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            throw new ArgumentException("Feature name is required.", nameof(featureName));

        if (!KnownFlags.Contains(featureName, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Unknown feature flag '{featureName}'.", nameof(featureName));

        _overrides[featureName] = enabled;
    }
}
