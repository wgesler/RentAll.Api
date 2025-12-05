using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Rentals;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Rentals
{
    public partial class RentalRepository : IRentalRepository
    {
        public async Task<Rental> CreateAsync(Rental rental)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<RentalEntity>("dbo.Rental_Add", new
            {
                PropertyId = rental.PropertyId,
                ContactId = rental.ContactId,
                StartDate = rental.StartDate,
                EndDate = rental.EndDate,
                DailyRate = rental.DailyRate,
                CreatedBy = rental.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Rental not created");

            return ConvertDtoToModel(res.FirstOrDefault()!);
        }
    }
}