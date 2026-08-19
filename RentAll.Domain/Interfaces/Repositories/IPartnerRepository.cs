using RentAll.Domain.Models;
using RentAll.Domain.Models.Partners;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPartnerRepository
{
    Task<IEnumerable<PropertyList>> GetAllPropertiesAsync();
    Task<IEnumerable<PartnerCityState>> GetListOfCitiesAsync();
}
