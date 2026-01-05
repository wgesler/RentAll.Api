using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Reservations
{
    public partial class ReservationRepository : IReservationRepository
    {
		public async Task<IEnumerable<Reservation>> GetAllAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetAll", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<Reservation>();

			return res.Select(ConvertEntityToModel);
		}
		
        public async Task<Reservation?> GetByIdAsync(Guid reservationId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetById", new
            {
				ReservationId = reservationId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<IEnumerable<Reservation>> GetByPropertyIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetByPropertyId", new
            {
                PropertyId = propertyId,
				OrganizationId = organizationId
			});

            if (res == null || !res.Any())
                return Enumerable.Empty<Reservation>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<Reservation>> GetByContactIdAsync(Guid contactId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetByContactId", new
            {
                ContactId = contactId,
				OrganizationId = organizationId
			});

            if (res == null || !res.Any())
                return Enumerable.Empty<Reservation>();

            return res.Select(ConvertEntityToModel);
        }
		public async Task<IEnumerable<Reservation>> GetActiveReservationsAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_GetAllActive", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<Reservation>();

			return res.Select(ConvertEntityToModel);
		}
	}
}
