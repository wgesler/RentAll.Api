using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Rentals;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Rentals
{
    public partial class ReservationRepository : IReservationRepository
    {
        public async Task<Reservation> UpdateByIdAsync(Reservation rental)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_UpdateById", new
            {
				ReservationId = rental.ReservationId,
                AgentId = rental.AgentId,
                PropertyId = rental.PropertyId,
                ContactId = rental.ContactId,
                ClientType = (int)rental.ClientType,
                ReservationStatus = (int)rental.ReservationStatus,
                IsActive = rental.IsActive,
                StartDate = rental.ArrivalDate,
                EndDate = rental.DepartureDate,
                CheckInTimeId = (int)rental.CheckInTime,
                CheckOutTimeId = (int)rental.CheckOutTime,
                MonthlyRate = rental.MonthlyRate,
                DailyRate = rental.DailyRate,
                NumberOfPeople = rental.NumberOfPeople,
                Deposit = rental.Deposit,
                DepartureFee = rental.DepartureFee,
                Taxes = rental.Taxes
            });

            if (res == null || !res.Any())
                throw new Exception("Reservation not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}