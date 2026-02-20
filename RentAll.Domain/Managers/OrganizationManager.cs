using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Domain.Managers;

public class OrganizationManager : IOrganizationManager
{
    private readonly Guid systemOrganizationId = Guid.Empty;
    private readonly ICommonRepository _commonRepository;

    public OrganizationManager(
        ICommonRepository commonRepository)
    {
        _commonRepository = commonRepository;
    }

    public async Task<string> GenerateEntityCodeAsync()
    {
        EntityType entityType = EntityType.Organization;
        int entityTypeId = (int)entityType;

        var prefix = entityType.ToCode();
        int nextNumber = await _commonRepository.GetNextAsync(systemOrganizationId, entityTypeId, entityType.ToString());
        var code = $"{prefix}-{nextNumber:D6}";

        return code;
    }
    public async Task<string> GenerateEntityCodeAsync(Guid organizationId, EntityType entityType)
    {
        var prefix = entityType.ToCode();
        int nextNumber = await _commonRepository.GetNextAsync(organizationId, (int)entityType, entityType.ToString());
        var code = $"{prefix}-{nextNumber:D6}";

        return code;
    }
}

