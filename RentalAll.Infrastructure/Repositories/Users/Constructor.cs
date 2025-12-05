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
			_dbConnectionString = appSettings.Value.DBConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private User ConvertDtoToModel(UserEntity dto)
		{
			var response = new User()
			{
				UserId = dto.UserId,
				Username = dto.Username,
				FirstName = dto.FirstName,
				LastName = dto.LastName,
				FullName = dto.FullName,
				Email = dto.Email,
				PasswordHash = dto.PasswordHash,
				IsActive = dto.IsActive,
				CreatedAt = dto.CreatedAt,
				UpdatedAt = dto.UpdatedAt
			};

			return response;
		}
	}

}
