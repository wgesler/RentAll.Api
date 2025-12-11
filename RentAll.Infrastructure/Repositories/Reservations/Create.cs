using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Rentals;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Rentals
{
    public partial class ReservationRepository : IReservationRepository
    {
        public async Task<Reservation> CreateAsync(Reservation rental)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_Add", new
            {
                AgentId = rental.AgentId,
                PropertyId = rental.PropertyId,
                ContactId = rental.ContactId,
                ClientTypeId = (int)rental.ClientType,
                ReservationStatusId = (int)rental.ReservationStatus,
                IsActive = rental.IsActive,
                ArrivalDate = rental.ArrivalDate,
                DepartureDate = rental.DepartureDate,
                CheckInTimeId = (int)rental.CheckInTime,
                CheckOutTimeId = (int)rental.CheckOutTime,
                MonthlyRate = rental.MonthlyRate,
                DailyRate = rental.DailyRate,
                NumberOfPeople = rental.NumberOfPeople,
                Deposit = rental.Deposit,
                DepartureFee = rental.DepartureFee,
                Taxes = rental.Taxes,
                CreatedBy = rental.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Reservation not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}