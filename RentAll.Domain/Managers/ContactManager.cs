using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Domain.Managers;

public class ContactManager : IContactManager
{
    private readonly IContactRepository _contactRepository;
    private readonly ICommonRepository _commonRepository;

    public ContactManager(
        IContactRepository contactRepository,
        ICommonRepository commonRepository)
    {
        _contactRepository = contactRepository;
        _commonRepository = commonRepository;
    }

    public async Task<string> GenerateContactCodeAsync(Guid organizationId, int entityTypeId)
    {
        var entityType = (EntityType)entityTypeId;
        var prefix = entityType.ToCode();
        int nextNumber = await _commonRepository.GetNextAsync(organizationId, entityTypeId, entityType.ToString());
        var code = $"C{prefix}-{nextNumber:D6}";

        return code;
    }
}

