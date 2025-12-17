using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Reservations;
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
				TenantName = e.TenantName,
				ClientId = e.ClientId,
				ClientType = (ClientType)e.ClientTypeId,
				ReservationStatus = (ReservationStatus)e.ReservationStatusId,
				ArrivalDate = e.ArrivalDate,
				DepartureDate = e.DepartureDate,
				CheckInTime = (CheckInTime)e.CheckInTimeId,
				CheckOutTime = (CheckOutTime)e.CheckOutTimeId,
				BillingType = (BillingType)e.BillingTypeId,
				BillingRate = e.BillingRate,
				NumberOfPeople = e.NumberOfPeople,
				Deposit = e.Deposit,
				CheckoutFee = e.CheckoutFee,
				MaidServiceFee = e.MaidServiceFee,
				FrequencyId = e.FrequencyId,
				PetFee = e.PetFee,
				ExtraFee = e.ExtraFee,
				ExtraFeeName = e.ExtraFeeName,
				Taxes = e.Taxes,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy,
				IsActive = e.IsActive
			};
		}
	}
}