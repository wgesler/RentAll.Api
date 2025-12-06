using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Common;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Common
{
	public partial class CommonRepository : ICommonRepository
	{
		private readonly string _dbConnectionString;

		public CommonRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private State ConvertDtoToModel(StateEntity dto)
		{
			return new State
			{
				Code = dto.Code,
				Name = dto.Name
			};
		}

		private DailyQuote ConvertDtoToModel(DailyQuoteEntity dto)
		{
			return new DailyQuote
			{
				q = dto.Quote,
				a = dto.Author,
				h = dto.HTML
			};
		}
	}
}

