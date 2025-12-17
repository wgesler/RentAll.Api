using RentAll.Domain.Models.Users;

namespace RentAll.Domain.Interfaces.Repositories
{
	public interface IUserRepository
	{
		// Creates
		Task<User> CreateAsync(User user);

		// Selects
		Task<IEnumerable<User>> GetAllAsync(Guid organizationId);
		Task<User?> GetByIdAsync(Guid userId);
		Task<User?> GetByEmailAsync(string email);
		Task<bool> ExistsByEmailAsync(string email);

		// Updates
		Task<User> UpdateByIdAsync(User user);

		// Deletes
		Task DeleteByIdAsync(Guid userId);
	}
}
