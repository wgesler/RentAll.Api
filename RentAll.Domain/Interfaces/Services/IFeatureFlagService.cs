namespace RentAll.Domain.Interfaces.Services;

public interface IFeatureFlagService
{
    IReadOnlyDictionary<string, bool> GetAll();
    bool IsEnabled(string featureName);
    Task<bool> IsEnabledAsync(string featureName, Guid organizationId, CancellationToken cancellationToken = default);
    void Set(string featureName, bool enabled);
}
