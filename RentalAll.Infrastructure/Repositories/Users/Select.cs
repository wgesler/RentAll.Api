using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Users
{
	public partial class UserRepository : IUserRepository
	{
		public async Task<bool> ExistsByUsernameAsync(string username)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_GetByName", new
			{
				username = username
			});

			if (res == null || !res.Any())
				return false;
			return true;
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

		public async Task<User?> GetByIdAsync(Guid userId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_GetById", new
			{
				UserId = userId
			});

			if (res == null || !res.Any())
				throw new Exception("User not found");

			return ConvertDtoToModel(res.FirstOrDefault()!);
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

			return ConvertDtoToModel(res.FirstOrDefault()!);
		}

		public async Task<User?> GetByUsernameAsync(string username)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_GetByName", new
			{
				username = username
			});

			if (res == null || !res.Any())
				throw new Exception("User not found");

			return ConvertDtoToModel(res.FirstOrDefault()!);
		}
	}
}
