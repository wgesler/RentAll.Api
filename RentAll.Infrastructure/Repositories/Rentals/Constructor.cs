using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Rentals;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Rentals
{
    public partial class RentalRepository : IRentalRepository
    {
        private readonly string _dbConnectionString;

        public RentalRepository(IOptions<AppSettings> appSettings)
        {
            _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        }

        private Rental ConvertEntityToModel(RentalEntity e)
        {
            var response = new Rental()
            {
                RentalId = e.RentalId,
                PropertyId = e.PropertyId,
                ContactId = e.ContactId,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                DailyRate = e.DailyRate,
                IsActive = e.IsActive,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }
    }
}