using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
    #region Reservation
    Task<IEnumerable<ReservationList>> GetReservationListByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Reservation>> GetReservationListByPropertyIdAsync(Guid propertyId, Guid organizationId);
    Task<Reservation?> GetReservationByIdAsync(Guid reservationId, Guid organizationId);

    Task<Reservation> CreateAsync(Reservation reservation);
    Task<Reservation> UpdateByIdAsync(Reservation reservation);
    Task DeleteReservationByIdAsync(Guid reservationId);
    #endregion

    #region Lease Information
    Task<LeaseInformation?> GetLeaseInformationByIdAsync(Guid leaseInformationId, Guid organizationId);
    Task<LeaseInformation?> GetLeaseInformationByPropertyIdAsync(Guid propertyId, Guid organizationId);

    Task<LeaseInformation> CreateLeaseInformationAsync(LeaseInformation leaseInformation);
    Task<LeaseInformation> UpdateLeaseInformationByIdAsync(LeaseInformation leaseInformation);
    Task DeleteLeaseInformationByIdAsync(Guid leaseInformationId);
    #endregion
}
