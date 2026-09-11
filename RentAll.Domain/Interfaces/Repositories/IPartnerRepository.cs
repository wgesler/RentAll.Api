using RentAll.Domain.Models;
using RentAll.Domain.Models.Partners;
using RentAll.Domain.Models.Properties;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPartnerRepository
{
    Task<IEnumerable<PropertyList>> GetAllPropertiesAsync();
    Task<IEnumerable<ExternalExportPropertyList>> GetExternalExportListAsync();
    Task<IEnumerable<PropertyList>> GetActivePropertyListBySelectionCriteriaAsync(Guid userId);
    Task<IEnumerable<PartnerCityState>> GetListOfCitiesAsync();
    Task<PartnerContact?> GetPartnerContactAsync(Guid propertyId);
}
