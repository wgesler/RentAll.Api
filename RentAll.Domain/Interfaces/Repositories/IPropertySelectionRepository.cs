using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertySelectionRepository
{
	Task<PropertySelection?> GetByUserIdAsync(Guid userId);
	Task<PropertySelection> UpsertAsync(PropertySelection selection);
}




