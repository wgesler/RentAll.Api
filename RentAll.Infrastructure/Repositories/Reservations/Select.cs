using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Rentals;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Rentals
{
    public partial class ReservationRepository : IReservationRepository
    {
		public async Task<IEnumerable<Reservation>> GetAllAsync()
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetAll", null);

			if (res == null || !res.Any())
				return Enumerable.Empty<Reservation>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<IEnumerable<Reservation>> GetActiveReservationsAsync()
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetAllActive", null);

			if (res == null || !res.Any())
				return Enumerable.Empty<Reservation>();

			return res.Select(ConvertEntityToModel);
		}
		
        public async Task<Reservation?> GetByIdAsync(Guid reservationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetById", new
            {
				ReservationId = reservationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<IEnumerable<Reservation>> GetByPropertyIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetByPropertyId", new
            {
                PropertyId = propertyId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Reservation>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<Reservation>> GetByContactIdAsync(Guid contactId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetByContactId", new
            {
                ContactId = contactId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Reservation>();

            return res.Select(ConvertEntityToModel);
        }
    }
}