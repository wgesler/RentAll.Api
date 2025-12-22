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
				AgentId = e.AgentId,
				PropertyId = e.PropertyId,
				ContactId = e.ContactId,
				ReservationType = (ReservationType)e.ReservationTypeId,
				ReservationStatus = (ReservationStatus)e.ReservationStatusId,
				ReservationNotice = (ReservationNotice)e.ReservationNoticeId,
				NumberOfPeople = e.NumberOfPeople,
				HasPets = e.HasPets,
				TenantName = e.TenantName,
				PropertyCode = e.PropertyCode,
				PropertyAddress = e.PropertyAddress,
				PropertyStatus = (PropertyStatus)e.PropertyStatusId,
				ContactName = e.ContactName,
				ContactPhone = e.ContactPhone,
				ContactEmail = e.ContactEmail,
				ArrivalDate = e.ArrivalDate,
				DepartureDate = e.DepartureDate,
				CheckInTime = (CheckInTime)e.CheckInTimeId,
				CheckOutTime = (CheckOutTime)e.CheckOutTimeId,
				BillingType = (BillingType)e.BillingTypeId,
				BillingRate = e.BillingRate,
				Deposit = e.Deposit,
				DepartureFee = e.DepartureFee,
				MaidServiceFee = e.MaidServiceFee,
				FrequencyId = e.FrequencyId,
				PetFee = e.PetFee,
				ExtraFee = e.ExtraFee,
				ExtraFeeName = e.ExtraFeeName,
				Taxes = e.Taxes,
				Notes = e.Notes,
				IsActive = e.IsActive,
				CreatedBy = e.CreatedBy,
				CreatedOn = e.CreatedOn,
				ModifiedBy = e.ModifiedBy,
				ModifiedOn = e.ModifiedOn
			};
		}
	}
}