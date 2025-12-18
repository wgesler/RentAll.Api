using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Reservations
{
	public partial class ReservationRepository : IReservationRepository
	{
		public async Task<Reservation> CreateAsync(Reservation r)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ReservationEntity>("dbo.Reservation_Add", new
			{
				OrganizationId = r.OrganizationId,
				AgentId = r.AgentId,
				PropertyId = r.PropertyId,
				TenantName = r.TenantName,
				ReservationTypeId = (int)r.ReservationType,
				ContactId = r.ContactId,
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
				CreatedBy = r.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Reservation not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}