using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Rentals;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Rentals
{
    public partial class RentalRepository : IRentalRepository
    {
        public async Task<Rental?> GetByIdAsync(Guid rentalId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RentalEntity>("dbo.Rental_GetById", new
            {
                RentalId = rentalId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertDtoToModel(res.FirstOrDefault()!);
        }

        public async Task<IEnumerable<Rental>> GetActiveRentalsAsync()
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RentalEntity>("dbo.Rental_GetActiveRentals", null);

            if (res == null || !res.Any())
                return Enumerable.Empty<Rental>();

            return res.Select(ConvertDtoToModel);
        }

        public async Task<IEnumerable<Rental>> GetByPropertyIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RentalEntity>("dbo.Rental_GetByPropertyId", new
            {
                PropertyId = propertyId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Rental>();

            return res.Select(ConvertDtoToModel);
        }

        public async Task<IEnumerable<Rental>> GetByContactIdAsync(Guid contactId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RentalEntity>("dbo.Rental_GetByContactId", new
            {
                ContactId = contactId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Rental>();

            return res.Select(ConvertDtoToModel);
        }
    }
}

