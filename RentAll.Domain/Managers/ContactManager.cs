using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Domain.Managers;

public class ContactManager : IContactManager
{
    private readonly IContactRepository _contactRepository;
    private readonly ICodeSequenceRepository _codeSequenceRepository;

    public ContactManager(
        IContactRepository contactRepository,
        ICodeSequenceRepository codeSequenceRepository)
    {
        _contactRepository = contactRepository;
        _codeSequenceRepository = codeSequenceRepository;
    }

    public async Task<string> GenerateContactCodeAsync(Guid organizationId, int entityTypeId)
    {
        var entityType = (EntityType)entityTypeId;
        var prefix = entityType.ToCode();
        int nextNumber = await _codeSequenceRepository.GetNextAsync(organizationId, entityTypeId, entityType.ToString());
        var code = $"{prefix}-{nextNumber:D6}";

        return code;
    }
}

