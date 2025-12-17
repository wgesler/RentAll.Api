using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Reservations;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Reservations
{
	public partial class ReservationRepository : IReservationRepository
	{
		public async Task<Reservation> UpdateByIdAsync(Reservation r)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_UpdateById", new
			{
				OrganizationId = r.OrganizationId,
				ReservationId = r.ReservationId,
				AgentId = r.AgentId,
				PropertyId = r.PropertyId,
				TenantName = r.TenantName,
				ClientId = r.ClientId,
				ClientTypeId = (int)r.ClientType,
				ReservationStatusId = (int)r.ReservationStatus,
				IsActive = r.IsActive,
				ArrivalDate = r.ArrivalDate,
				DepartureDate = r.DepartureDate,
				CheckInTimeId = (int)r.CheckInTime,
				CheckOutTimeId = (int)r.CheckOutTime,
				BillingTypeId = (int)r.BillingType,
				BillingRate = r.BillingRate,
				NumberOfPeople = r.NumberOfPeople,
				Deposit = r.Deposit,
				CheckoutFee = r.CheckoutFee,
				MaidServiceFee = r.MaidServiceFee,
				FrequencyId = r.FrequencyId,
				PetFee = r.PetFee,
				ExtraFee = r.ExtraFee,
				ExtraFeeName = r.ExtraFeeName,
				Taxes = r.Taxes,
				ModifiedBy = r.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Reservation not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}