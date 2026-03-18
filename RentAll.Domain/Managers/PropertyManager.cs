using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public class PropertyManager : IPropertyManager
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IContactRepository _contactRepository;

    public PropertyManager(
        IPropertyRepository propertyRepository,
        IContactRepository contactRepository)
    {
        _propertyRepository = propertyRepository;
        _contactRepository = contactRepository;
    }

    public async Task UpdatePropertyOfficeAsync(Property p, Guid currentUser)
    {
        if (p.Owner1Id != Guid.Empty)
        {
            var contact = await _contactRepository.GetContactByIdAsync(p.Owner1Id, p.OrganizationId);
            if (contact == null)
                return;

            contact?.OfficeId = p.OfficeId;
            contact?.ModifiedBy = currentUser;
            _ = await _contactRepository.UpdateByIdAsync(contact!);
        }

        if (p.Owner2Id is { } owner2Id && owner2Id != Guid.Empty)
        {
            var contact = await _contactRepository.GetContactByIdAsync(owner2Id, p.OrganizationId);
            if (contact == null)
                return;

            contact?.OfficeId = p.OfficeId;
            contact?.ModifiedBy = currentUser;
            _ = await _contactRepository.UpdateByIdAsync(contact!);
        }

        if (p.Owner3Id != null && p.Owner3Id != Guid.Empty)
        {
            var contact = await _contactRepository.GetContactByIdAsync(p.Owner3Id.Value, p.OrganizationId);
            if (contact == null)
                return;

            contact?.OfficeId = p.OfficeId;
            contact?.ModifiedBy = currentUser;
            _ = await _contactRepository.UpdateByIdAsync(contact!);
        }
    }
}
