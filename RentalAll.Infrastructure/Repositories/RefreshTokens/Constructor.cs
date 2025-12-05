using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Auth;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.RefreshTokens
{
	public partial class RefreshTokenRepository : IRefreshTokenRepository
	{
		private readonly string _dbConnectionString;

		public RefreshTokenRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private RefreshToken ConvertDtoToModel(RefreshTokenEntity dto)
		{
			var response = new RefreshToken()
			{
				RefreshTokenId = dto.RefreshTokenId,
				UserId = dto.UserId,
				TokenHash = dto.TokenHash,
				ExpiresOn = dto.ExpiresOn,
				CreatedOn = dto.CreatedOn
			};

			return response;
		}
	}
}

