using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Managers
{
    public interface IOrganizationManager
    {
        Task<string> GenerateEntityCodeAsync();
        Task<string> GenerateEntityCodeAsync(Guid organizationId, EntityType entityType);
        Task<IReadOnlyList<CodeSequence>> GetCodeSequencesAsync(Guid organizationId);
        Task ResetEntityCodeSequenceAsync(Guid organizationId, EntityType entityType, int nextNumber = 0);
    }
}
