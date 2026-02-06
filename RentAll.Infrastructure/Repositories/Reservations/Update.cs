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
		await db.OpenAsync();
		await using var transaction = await db.BeginTransactionAsync();

		try
		{
			// Get current reservation with ExtraFeeLines
			var currentReservationResult = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetById", new
			{
				ReservationId = r.ReservationId,
				OrganizationId = r.OrganizationId
			}, transaction: transaction);

			if (currentReservationResult == null || !currentReservationResult.Any())
				throw new Exception("Reservation not found");

			var currentReservation = ConvertEntityToModel(currentReservationResult.FirstOrDefault()!);
			var currentExtraFeeLineIds = currentReservation.ExtraFeeLines.Select(efl => efl.ExtraFeeLineId).ToHashSet();
			var incomingExtraFeeLineIds = r.ExtraFeeLines.Where(efl => efl.ExtraFeeLineId != 0).Select(efl => efl.ExtraFeeLineId).ToHashSet();

			// Update the Reservation
			var response = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_UpdateById", new
			{
				ReservationId = r.ReservationId,
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
				ModifiedBy = r.ModifiedBy
			}, transaction: transaction);

			if (response == null || !response.Any())
				throw new Exception("Reservation not updated");

			// Sync ExtraFeeLines
			if (r.ExtraFeeLines != null && r.ExtraFeeLines.Any())
			{
				foreach (var line in r.ExtraFeeLines)
				{
					if (line.ExtraFeeLineId == 0)
					{
						// Create new ExtraFeeLine
						await db.DapperProcQueryAsync<ExtraFeeLineEntity>("Property.ExtraFeeLine_Add", new
						{
							ReservationId = r.ReservationId,
							FeeDescription = line.FeeDescription,
							FeeAmount = line.FeeAmount,
							FeeFrequencyId = (int)line.FeeFrequency,
							CostCodeId = line.CostCodeId
						}, transaction: transaction);
					}
					else if (currentExtraFeeLineIds.Contains(line.ExtraFeeLineId))
					{
						// Update existing ExtraFeeLine
						await db.DapperProcQueryAsync<ExtraFeeLineEntity>("Property.ExtraFeeLine_UpdateById", new
						{
							ExtraFeeLineId = line.ExtraFeeLineId,
							ReservationId = r.ReservationId,
							FeeDescription = line.FeeDescription,
							FeeAmount = line.FeeAmount,
							FeeFrequencyId = (int)line.FeeFrequency,
							CostCodeId = line.CostCodeId
						}, transaction: transaction);
					}
				}
			}

			// Delete ExtraFeeLines that are no longer in the incoming list
			var extraFeeLinesToDelete = currentExtraFeeLineIds.Except(incomingExtraFeeLineIds).ToList();
			foreach (var extraFeeLineId in extraFeeLinesToDelete)
			{
				await db.DapperProcExecuteAsync("Property.ExtraFeeLine_DeleteById", new
				{
					ExtraFeeLineId = extraFeeLineId
				}, transaction: transaction);
			}

			// Get fully populated reservation
			var updatedReservationResult = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_GetById", new
			{
				ReservationId = r.ReservationId,
				OrganizationId = r.OrganizationId
			}, transaction: transaction);

			if (updatedReservationResult == null || !updatedReservationResult.Any())
				throw new Exception("Reservation not updated");

			await transaction.CommitAsync();
			return ConvertEntityToModel(updatedReservationResult.FirstOrDefault()!);
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}
	}

		public async Task<Reservation> IncrementCurrentInvoiceAsync(Guid reservationId, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<ReservationEntity>("Property.Reservation_IncrementInvoiceById", new
			{
				ReservationId = reservationId,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				throw new Exception("Reservation not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);

		}
	}
}
