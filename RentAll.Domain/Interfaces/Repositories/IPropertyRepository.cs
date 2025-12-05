using RentAll.Domain.Models.Properties;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertyRepository
{
    // Creates
    Task<Property> CreateAsync(Property property);

    // Selects
    Task<Property?> GetByIdAsync(Guid propertyId);
    Task<Property?> GetByPropertyCodeAsync(string propertyCode);
    Task<IEnumerable<Property>> GetAllAsync();
    Task<IEnumerable<Property>> GetByStateAsync(string state);
    Task<bool> ExistsByPropertyCodeAsync(string propertyCode);

    // Updates
    Task<Property> UpdateByIdAsync(Property property);

    // Deletes
    Task DeleteByIdAsync(Guid propertyId);
}