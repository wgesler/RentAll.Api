using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Domain.Managers;

public class OrganizationManager : IOrganizationManager
{
    private const int CodeSequenceResetThreshold = 1_000_000;
    private readonly Guid systemOrganizationId = Guid.Empty;
    private readonly ICommonRepository _commonRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public OrganizationManager(ICommonRepository commonRepository, IOrganizationRepository organizationRepository)
    {
        _commonRepository = commonRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<string> GenerateEntityCodeAsync()
    {
        EntityType entityType = EntityType.Organization;
        var prefix = entityType.ToCode();
        for (var attempt = 0; attempt < CodeSequenceResetThreshold; attempt++)
        {
            int nextNumber = await GetNextEntityCodeNumberAsync(systemOrganizationId, entityType);
            var code = $"{prefix}-{nextNumber:D6}";
            if (!await _organizationRepository.ExistsByOrganizationCodeAsync(code))
                return code;
        }

        throw new InvalidOperationException("Unable to generate a unique organization code.");
    }

    public async Task<string> GenerateEntityCodeAsync(Guid organizationId, EntityType entityType)
    {
        var prefix = entityType.ToCode();
        int nextNumber = await GetNextEntityCodeNumberAsync(organizationId, entityType);
        var code = $"{prefix}-{nextNumber:D6}";

        return code;
    }

    public async Task ResetEntityCodeSequenceAsync(Guid organizationId, EntityType entityType, int nextNumber = 0)
    {
        await _commonRepository.ResetCodeSequenceAsync(organizationId, (int)entityType, entityType.ToString(), nextNumber);
    }

    async Task<int> GetNextEntityCodeNumberAsync(Guid organizationId, EntityType entityType)
    {
        var nextNumber = await _commonRepository.GetNextCodeAsync(organizationId, (int)entityType, entityType.ToString());
        if (nextNumber < CodeSequenceResetThreshold)
            return nextNumber;

        await ResetEntityCodeSequenceAsync(organizationId, entityType, 0);
        return await _commonRepository.GetNextCodeAsync(organizationId, (int)entityType, entityType.ToString());
    }
}
