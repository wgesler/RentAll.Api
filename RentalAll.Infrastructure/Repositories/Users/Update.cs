using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Users;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Users
{
	public partial class UserRepository : IUserRepository
	{
		public async Task<User> UpdateByIdAsync(User user)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_UpdateById", new
			{
				Username = user.Username,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Email = user.Email,
				PasswordHash = user.PasswordHash,
				IsActive = user.IsActive
			});

			if (res == null || !res.Any())
				throw new Exception("User not found");

			return ConvertDtoToModel(res.FirstOrDefault()!);
		}
	}
}
