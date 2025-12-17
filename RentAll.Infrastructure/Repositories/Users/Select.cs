using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Users;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Users
{
	public partial class UserRepository : IUserRepository
	{
		public async Task<IEnumerable<User>> GetAllAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_GetAll", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<User>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<User?> GetByIdAsync(Guid userId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_GetById", new
			{
				UserId = userId
			});

			if (res == null || !res.Any())
				throw new Exception("User not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<User?> GetByEmailAsync(string email)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_GetByEmail", new
			{
				Email = email
			});

			if (res == null || !res.Any())
				throw new Exception("User not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<bool> ExistsByEmailAsync(string email)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_GetByEmail", new
			{
				Email = email
			});

			if (res == null || !res.Any())
				return false;
			return true;
		}


	}
}