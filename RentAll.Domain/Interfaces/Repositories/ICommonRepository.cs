using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICommonRepository
{
    Task<IEnumerable<State>> GetAllStatesAsync();
}

