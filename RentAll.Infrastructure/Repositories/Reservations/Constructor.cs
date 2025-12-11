using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Rentals;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Rentals
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
            var response = new Reservation()
            {
                ReservationId = e.ReservationId,
                AgentId = e.AgentId,
				PropertyCode = e.PropertyCode,
				PropertyAddress = e.PropertyAddress,
				PropertyStatus = (PropertyStatus)e.PropertyStatusId,
				PropertyId = e.PropertyId,
                ContactId = e.ContactId,
                ClientType = (ClientType)e.ClientTypeId,
                ReservationStatus = (ReservationStatus)e.ReservationStatusId,
                IsActive = e.IsActive,
                ArrivalDate = e.ArrivalDate,
                DepartureDate = e.DepartureDate,
                CheckInTime = (CheckInTime)e.CheckInTimeId,
                CheckOutTime = (CheckOutTime)e.CheckOutTimeId,
                MonthlyRate = e.MonthlyRate,
                DailyRate = e.DailyRate,
				Bedrooms = e.Bedrooms,
				Bathrooms = e.Bathrooms,
                NumberOfPeople = e.NumberOfPeople,
                Deposit = e.Deposit,
                DepartureFee = e.DepartureFee,
                Taxes = e.Taxes,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }
    }
}