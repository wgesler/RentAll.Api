using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
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
				ReservationId = r.ReservationId,
				OrganizationId = r.OrganizationId,
				AgentId = r.AgentId,
				PropertyId = r.PropertyId,
				ContactId = r.ContactId,
				ReservationTypeId = (int)r.ReservationType,
				ReservationStatusId = (int)r.ReservationStatus,
				ReservationNoticeId = (int)r.ReservationNotice,
				NumberOfPeople = r.NumberOfPeople,
				HasPets = r.HasPets,
				TenantName = r.TenantName,
				ArrivalDate = r.ArrivalDate,
				DepartureDate = r.DepartureDate,
				CheckInTimeId = (int)r.CheckInTime,
				CheckOutTimeId = (int)r.CheckOutTime,
				BillingTypeId = (int)r.BillingType,
				BillingRate = r.BillingRate,
				Deposit = r.Deposit,
				DepartureFee = r.DepartureFee,
				MaidServiceFee = r.MaidServiceFee,
				FrequencyId = r.FrequencyId,
				PetFee = r.PetFee,
				ExtraFee = r.ExtraFee,
				ExtraFeeName = r.ExtraFeeName,
				Taxes = r.Taxes,
				Notes = r.Notes,
				IsActive = r.IsActive,
				ModifiedBy = r.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Reservation not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}