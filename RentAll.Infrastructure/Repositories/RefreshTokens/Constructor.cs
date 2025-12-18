using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
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

		private RefreshToken ConvertEntityToModel(RefreshTokenEntity e)
		{
			var response = new RefreshToken()
			{
				RefreshTokenId = e.RefreshTokenId,
				UserId = e.UserId,
				TokenHash = e.TokenHash,
				ExpiresOn = e.ExpiresOn,
				CreatedOn = e.CreatedOn
			};

			return response;
		}
	}
}