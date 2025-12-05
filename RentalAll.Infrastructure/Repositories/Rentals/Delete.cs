using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Rentals
{
    public partial class RentalRepository : IRentalRepository
    {
        public async Task DeleteByIdAsync(Guid rentalId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("dbo.Rental_DeleteById", new
            {
                RentalId = rentalId
            });
        }
    }
}

