using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories
{
	public interface IUserRepository
	{
		// Creates
		Task<User> CreateAsync(User user);

		// Selects
		Task<User?> GetByIdAsync(Guid userId);
		Task<User?> GetByUsernameAsync(string username);
		Task<User?> GetByEmailAsync(string email);
		Task<bool> ExistsByUsernameAsync(string username);
		Task<bool> ExistsByEmailAsync(string email);

		// Updates
		Task<User> UpdateByIdAsync(User user);

		// Deletes
		Task DeleteByIdAsync(Guid userId);
	}
}
