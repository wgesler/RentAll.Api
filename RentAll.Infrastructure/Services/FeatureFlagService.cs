using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Infrastructure.Services;

public class FeatureFlagService : IFeatureFlagService
{
    private static readonly string[] KnownFlags = [FeatureFlagKeys.Accounting];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, bool> _overrides = new(StringComparer.OrdinalIgnoreCase);

    public FeatureFlagService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public IReadOnlyDictionary<string, bool> GetAll()
    {
        return KnownFlags.ToDictionary(flag => flag, IsEnabled, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsEnabled(string featureName)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            return false;

        return _overrides.TryGetValue(featureName, out var overridden) && overridden;
    }

    public async Task<bool> IsEnabledAsync(string featureName, Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            return false;

        if (_overrides.TryGetValue(featureName, out var overridden) && !overridden)
            return false;

        if (organizationId != Guid.Empty && TryMapToFeatureType(featureName, out var featureType))
            return await OrganizationHasFeatureAccessAsync(organizationId, featureType, cancellationToken);

        return _overrides.TryGetValue(featureName, out overridden) && overridden;
    }

    public void Set(string featureName, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(featureName))
            throw new ArgumentException("Feature name is required.", nameof(featureName));

        if (!KnownFlags.Contains(featureName, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Unknown feature flag '{featureName}'.", nameof(featureName));

        _overrides[featureName] = enabled;
    }

    private async Task<bool> OrganizationHasFeatureAccessAsync(Guid organizationId, FeatureType featureType, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var organizationRepository = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var features = await organizationRepository.GetFeaturesByOrganizationIdAsync(organizationId);

        return features.Any(feature =>
            feature.FeatureTypeId == featureType
            && feature.HasAccess);
    }

    private static bool TryMapToFeatureType(string featureName, out FeatureType featureType)
    {
        switch (featureName)
        {
            case FeatureFlagKeys.Accounting:
                featureType = FeatureType.Accounting;
                return true;
            default:
                featureType = default;
                return false;
        }
    }
}
