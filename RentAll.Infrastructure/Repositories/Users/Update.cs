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
		public async Task<User> UpdateByIdAsync(User user)
		{
			await using var db = new SqlConnection(_dbConnectionString);

			var userGroupsJson = user.UserGroups != null && user.UserGroups.Any()
				? JsonSerializer.Serialize(user.UserGroups)
				: "[]";

			var officeAccessJson = user.OfficeAccess != null && user.OfficeAccess.Any()
				? JsonSerializer.Serialize(user.OfficeAccess)
				: "[]";

			var res = await db.DapperProcQueryAsync<UserEntity>("dbo.User_UpdateById", new
			{
				OrganizationId = user.OrganizationId,
				UserId = user.UserId,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Email = user.Email,
				PasswordHash = user.PasswordHash,
				UserGroups = userGroupsJson,
				OfficeAccess = officeAccessJson,
				IsActive = user.IsActive,
				ModifiedBy = user.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("User not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
