using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Common;

namespace RentAll.Infrastructure.Repositories.Common
{
    public partial class CommonRepository : ICommonRepository
    {
        private readonly string _dbConnectionString;

        public CommonRepository(IOptions<AppSettings> appSettings)
        {
            _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        }

        private State ConvertEntityToModel(StateEntity e)
        {
            return new State
            {
                Code = e.Code,
                Name = e.Name
            };
        }

        private DailyQuote ConvertEntityToModel(DailyQuoteEntity e)
        {
            return new DailyQuote
            {
                q = e.Quote,
                a = e.Author,
                h = e.HTML
            };
        }
    }
}

