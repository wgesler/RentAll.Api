using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Rentals;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Rentals
{
    public partial class ReservationRepository : IReservationRepository
    {
        public async Task<Reservation> UpdateByIdAsync(Reservation r)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_UpdateById", new
            {
				ReservationId = r.ReservationId,
                AgentId = r.AgentId,
                PropertyId = r.PropertyId,
                ContactId = r.ContactId,
                ClientTypeId = (int)r.ClientType,
                ReservationStatusId = (int)r.ReservationStatus,
                IsActive = r.IsActive,
                ArrivalDate = r.ArrivalDate,
                DepartureDate = r.DepartureDate,
                CheckInTimeId = (int)r.CheckInTime,
                CheckOutTimeId = (int)r.CheckOutTime,
                MonthlyRate = r.MonthlyRate,
                DailyRate = r.DailyRate,
                NumberOfPeople = r.NumberOfPeople,
                Deposit = r.Deposit,
                DepartureFee = r.DepartureFee,
                Taxes = r.Taxes,
				ModifiedBy = r.ModifiedBy
			});

            if (res == null || !res.Any())
                throw new Exception("Reservation not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}