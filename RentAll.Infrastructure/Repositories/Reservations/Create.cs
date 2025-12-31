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
				ReservationCode = r.ReservationCode,
				AgentId = r.AgentId,
				PropertyId = r.PropertyId,
				ContactId = r.ContactId,
				ReservationTypeId = (int)r.ReservationType,
				ReservationStatusId = (int)r.ReservationStatus,
				ReservationNoticeId = (int)r.ReservationNotice,
				NumberOfPeople = r.NumberOfPeople,
				TenantName = r.TenantName,
				ArrivalDate = r.ArrivalDate,
				DepartureDate = r.DepartureDate,
				CheckInTimeId = (int)r.CheckInTime,
				CheckOutTimeId = (int)r.CheckOutTime,
				BillingTypeId = (int)r.BillingType,
				BillingRate = r.BillingRate,
				Deposit = r.Deposit,
				DepositTypeId = (int)r.DepositType,
				DepartureFee = r.DepartureFee,
				HasPets = r.HasPets,
				PetFee = r.PetFee,
				NumberOfPets = r.NumberOfPets,
				PetDescription = r.PetDescription,
				MaidService = r.MaidService,
				MaidServiceFee = r.MaidServiceFee,
				FrequencyId = (int)r.Frequency,
				Taxes = r.Taxes,
				ExtraFee = r.ExtraFee,
				ExtraFeeName = r.ExtraFeeName,
				ExtraFee2 = r.ExtraFee2,
				ExtraFee2Name = r.ExtraFee2Name,
				Notes = r.Notes,
				AllowExtensions = r.AllowExtensions,
				IsActive = r.IsActive,
				CreatedBy = r.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Reservation not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}