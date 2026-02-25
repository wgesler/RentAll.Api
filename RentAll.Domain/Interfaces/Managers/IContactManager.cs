using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers
{
    public interface IContactManager
    {
        Task<string> GenerateContactCodeAsync(Guid organizationId, int contactTypeId);

        Task GenerateLoginForOwnerContact(Contact contact, Guid createdBy);
    }
}
