using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Managers;

public class OrganizationManager : IOrganizationManager
{
    private const int UniqueCodeAttemptLimit = 1_000_000;
    private const int CodeSequenceResetThreshold = 1_000_000_000;
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
        for (var attempt = 0; attempt < UniqueCodeAttemptLimit; attempt++)
        {
            int nextNumber = await GetNextEntityCodeNumberAsync(systemOrganizationId, entityType);
            var code = EntityCodeFormatting.Format(prefix, nextNumber);
            if (!await _organizationRepository.ExistsByOrganizationCodeAsync(code))
                return code;
        }

        throw new InvalidOperationException("Unable to generate a unique organization code.");
    }

    public async Task<string> GenerateEntityCodeAsync(Guid organizationId, EntityType entityType)
    {
        var prefix = entityType.ToCode();
        int nextNumber = await GetNextEntityCodeNumberAsync(organizationId, entityType);
        var code = EntityCodeFormatting.Format(prefix, nextNumber);

        return code;
    }

    public async Task<IReadOnlyList<CodeSequence>> GetCodeSequencesAsync(Guid organizationId)
    {
        var sequences = await _commonRepository.GetCodeSequencesAsync(organizationId);
        return (sequences ?? []).ToList();
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
