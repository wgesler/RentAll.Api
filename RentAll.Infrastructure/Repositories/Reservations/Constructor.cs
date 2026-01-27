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
		private readonly string _dbConnectionString;

		public ReservationRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private Reservation ConvertEntityToModel(ReservationEntity e)
		{
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
				ExtraFee = e.ExtraFee,
				ExtraFeeName = e.ExtraFeeName,
				ExtraFee2 = e.ExtraFee2,
				ExtraFee2Name = e.ExtraFee2Name,
				Notes = e.Notes,
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
				CreditDue = e.CreditDue,
				IsActive = e.IsActive,
				CreatedOn = e.CreatedOn
			};
		}
	}
}
