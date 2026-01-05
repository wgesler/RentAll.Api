using System.Text.Json;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Users
{
	public partial class UserRepository : IUserRepository
	{
		private readonly string _dbConnectionString;

		public UserRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private User ConvertEntityToModel(UserEntity e)
		{
			List<string> userGroups = new List<string>();
			if (!string.IsNullOrWhiteSpace(e.UserGroups))
			{
				try
				{
					userGroups = JsonSerializer.Deserialize<List<string>>(e.UserGroups) ?? new List<string>();
				}
				catch
				{
					userGroups = new List<string>();
				}
			}

			var response = new User()
			{
				UserId = e.UserId,
				OrganizationId = e.OrganizationId,
				OrganizationName = e.OrganizationName,
				FirstName = e.FirstName,
				LastName = e.LastName,
				Email = e.Email,
				PasswordHash = e.PasswordHash,
				IsActive = e.IsActive,
				UserGroups = userGroups,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};

			return response;
		}
	}

}
