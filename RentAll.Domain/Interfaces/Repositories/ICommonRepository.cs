using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICommonRepository
{
    #region States
    Task<IEnumerable<State>> GetAllStatesAsync();
    #endregion

    #region Code Sequence
    Task<int> GetNextCodeAsync(Guid organizationId, int entityTypeId, string entityType);
    #endregion
}
