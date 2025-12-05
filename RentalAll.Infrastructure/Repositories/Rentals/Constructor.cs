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

        private Rental ConvertDtoToModel(RentalEntity dto)
        {
            var response = new Rental()
            {
                RentalId = dto.RentalId,
                PropertyId = dto.PropertyId,
                ContactId = dto.ContactId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DailyRate = dto.DailyRate,
                IsActive = dto.IsActive,
                CreatedOn = dto.CreatedOn,
                CreatedBy = dto.CreatedBy,
                ModifiedOn = dto.ModifiedOn,
                ModifiedBy = dto.ModifiedBy
            };

            return response;
        }
    }
}

