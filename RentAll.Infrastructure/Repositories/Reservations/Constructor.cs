using System.Text.Json;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Reservations
{
	public partial class ReservationRepository : IReservationRepository
	{
		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		private readonly string _dbConnectionString;

		public ReservationRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private Reservation ConvertEntityToModel(ReservationEntity e)
		{
			List<ExtraFeeLine> extraFeeLines = new List<ExtraFeeLine>();
			if (!string.IsNullOrWhiteSpace(e.ExtraFeeLines))
			{
				try
				{
					var entityLines = JsonSerializer.Deserialize<List<ExtraFeeLineEntity>>(e.ExtraFeeLines, JsonOptions) ?? new List<ExtraFeeLineEntity>();
					extraFeeLines = entityLines.Select(ConvertExtraFeeLineEntityToModel).ToList();
				}
				catch
				{
					extraFeeLines = new List<ExtraFeeLine>();
				}
			}

			return new Reservation
			{
				ReservationId = e.ReservationId,
				OrganizationId = e.OrganizationId,
				OfficeId = e.OfficeId,
				OfficeName = e.OfficeName,
				ReservationCode = e.ReservationCode,
				AgentId = e.AgentId,
				PropertyId = e.PropertyId,
				ContactId = e.ContactId,
				ContactName = e.ContactName,
				ReservationType = (ReservationType)e.ReservationTypeId,
				ReservationStatus = (ReservationStatus)e.ReservationStatusId,
				ReservationNotice = (ReservationNotice)e.ReservationNoticeId,
				NumberOfPeople = e.NumberOfPeople,
				TenantName = e.TenantName,
				ArrivalDate = e.ArrivalDate,
				DepartureDate = e.DepartureDate,
				CheckInTime = (CheckInTime)e.CheckInTimeId,
				CheckOutTime = (CheckOutTime)e.CheckOutTimeId,
				BillingMethod = (BillingMethod)e.BillingMethodId,
				ProrateType = (ProrateType)e.ProrateTypeId,
				BillingType = (BillingType)e.BillingTypeId,
				BillingRate = e.BillingRate,
				Deposit = e.Deposit,
				DepositType = (DepositType)e.DepositTypeId,
				DepartureFee = e.DepartureFee,
				HasPets = e.HasPets,
				PetFee = e.PetFee,
				NumberOfPets = e.NumberOfPets,
				PetDescription = e.PetDescription,
				MaidService = e.MaidService,
				MaidServiceFee = e.MaidServiceFee,
				Frequency = (FrequencyType)e.FrequencyId,
				MaidStartDate = e.MaidStartDate,
				Taxes = e.Taxes,
				Notes = e.Notes,
				ExtraFeeLines = extraFeeLines,
				AllowExtensions = e.AllowExtensions,
				CurrentInvoiceNumber = e.CurrentInvoiceNumber,
				CreditDue = e.CreditDue,
				IsActive = e.IsActive,
				CreatedBy = e.CreatedBy,
				CreatedOn = e.CreatedOn,
				ModifiedBy = e.ModifiedBy,
				ModifiedOn = e.ModifiedOn
			};
		}

	private ExtraFeeLine ConvertExtraFeeLineEntityToModel(ExtraFeeLineEntity e)
	{
		return new ExtraFeeLine
		{
			ExtraFeeLineId = e.ExtraFeeLineId,
			ReservationId = e.ReservationId,
			FeeDescription = e.FeeDescription,
			FeeAmount = e.FeeAmount,
			FeeFrequency = (FrequencyType)e.FeeFrequencyId,
			CostCodeId = e.CostCodeId
		};
	}

		private ReservationList ConvertEntityToModel(ReservationListEntity e)
		{
			return new ReservationList
			{
				ReservationId = e.ReservationId,
				ReservationCode = e.ReservationCode,
				PropertyId = e.PropertyId,
				PropertyCode = e.PropertyCode,
				OfficeId = e.OfficeId,
				OfficeName = e.OfficeName,
				ContactId = e.ContactId,
				ContactName = e.ContactName,
				TenantName = e.TenantName,
				CompanyName = e.CompanyName,
				AgentCode = e.AgentCode,
				MonthlyRate = e.MonthlyRate,
				ArrivalDate = e.ArrivalDate,
				DepartureDate = e.DepartureDate,
				ReservationStatus = (ReservationStatus)e.ReservationStatusId,
				CurrentInvoiceNumber = e.CurrentInvoiceNumber,
				CreditDue = e.CreditDue,
				IsActive = e.IsActive,
				CreatedOn = e.CreatedOn
			};
		}
	}
}
