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
			var res = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_Add", new
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
				BillingMethodId = (int)r.BillingMethod,
				ProrateTypeId = (int)r.ProrateType,
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
				MaidStartDate = r.MaidStartDate,
				Taxes = r.Taxes,
				Notes = r.Notes,
				AllowExtensions = r.AllowExtensions,
				CurrentInvoiceNumber = r.CurrentInvoiceNumber,
				CreditDue = r.CreditDue,
				IsActive = r.IsActive,
				CreatedBy = r.CreatedBy
			});

		if (res == null || !res.Any())
			throw new Exception("Reservation not created");

		var reservation = ConvertEntityToModel(res.FirstOrDefault()!);
		
		// Create ExtraFeeLines
		if (r.ExtraFeeLines != null && r.ExtraFeeLines.Any())
		{
			foreach (var line in r.ExtraFeeLines)
			{
				await db.DapperProcQueryAsync<ExtraFeeLineEntity>("Property.ExtraFeeLine_Add", new
				{
					ReservationId = reservation.ReservationId,
					FeeDescription = line.FeeDescription,
					FeeAmount = line.FeeAmount,
					FeeFrequencyId = (int)line.FeeFrequency
				});
			}
		}

		// Get fully populated reservation
		var populatedRes = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetById", new
		{
			ReservationId = reservation.ReservationId,
			OrganizationId = reservation.OrganizationId
		});

		if (populatedRes == null || !populatedRes.Any())
			throw new Exception("Reservation not found");

		return ConvertEntityToModel(populatedRes.FirstOrDefault()!);
	}
	}
}
