namespace RentAll.Domain.Interfaces.Services;

public interface IFeatureFlagService
{
    IReadOnlyDictionary<string, bool> GetAll();
    bool IsEnabled(string featureName);
    void Set(string featureName, bool enabled);
}
