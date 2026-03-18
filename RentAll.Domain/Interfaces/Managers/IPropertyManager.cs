using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IPropertyManager
{
    Task UpdatePropertyOfficeAsync(Property property, Guid currentUser);
}
