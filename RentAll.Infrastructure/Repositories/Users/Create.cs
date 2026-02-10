using System.Data.SqlClient;
using System.Text.Json;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Users
{
	public partial class UserRepository : IUserRepository
	{
		public async Task<User> CreateAsync(User user)
		{
			var userGroupsJson = user.UserGroups != null && user.UserGroups.Any()
				? JsonSerializer.Serialize(user.UserGroups)
				: "[]";

			var officeAccessJson = user.OfficeAccess != null && user.OfficeAccess.Any()
				? JsonSerializer.Serialize(user.OfficeAccess)
				: "[]";

			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<UserEntity>("User.User_Add", new
			{
				OrganizationId = user.OrganizationId,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Email = user.Email,
				PasswordHash = user.PasswordHash,
				UserGroups = userGroupsJson,
				OfficeAccess = officeAccessJson,
				ProfilePath = user.ProfilePath,
				CreatedBy = user.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("User not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
