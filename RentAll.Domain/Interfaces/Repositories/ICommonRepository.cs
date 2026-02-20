using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICommonRepository
{
    // Common Selects
    Task<IEnumerable<State>> GetAllStatesAsync();

    // CodeSequence Selects
    Task<int> GetNextAsync(Guid organizationId, int entityTypeId, string entityType);
}

