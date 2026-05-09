using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
    #region Reservation
    Task<IEnumerable<ReservationList>> GetReservationListByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<ReservationList>> GetReservationActiveListByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Reservation>> GetReservationListByPropertyIdAsync(Guid propertyId, Guid organizationId);
    Task<IEnumerable<Reservation>> GetReservationActiveListByPropertyIdAsync(Guid propertyId, Guid organizationId);
    Task<Reservation?> GetReservationByIdAsync(Guid reservationId, Guid organizationId);

    Task<Reservation> CreateAsync(Reservation reservation);
    Task<Reservation> UpdateByIdAsync(Reservation reservation);
    Task DeleteReservationByIdAsync(Guid reservationId, Guid organizationId);
    #endregion

    #region Lease Information
    Task<LeaseInformation?> GetLeaseInformationByIdAsync(Guid leaseInformationId, Guid organizationId);
    Task<LeaseInformation?> GetLeaseInformationByPropertyIdAsync(Guid propertyId, Guid organizationId, int? officeId = null);
    Task<LeaseInformation?> GetLeaseInformationByScopeAsync(Guid organizationId, int? officeId, Guid? propertyId);
    Task<LeaseInformation?> GetLeaseInformationByExactScopeAsync(Guid organizationId, int? officeId, Guid? propertyId);

    Task<LeaseInformation> CreateLeaseInformationAsync(LeaseInformation leaseInformation);
    Task<LeaseInformation> UpdateLeaseInformationByIdAsync(LeaseInformation leaseInformation);
    Task DeleteLeaseInformationByIdAsync(Guid leaseInformationId, Guid organizationId, Guid modifiedBy);
    #endregion

    #region Tracker Responses
    Task<IEnumerable<TrackerResponse>> GetTrackerResponsesByReservationIdAsync(Guid reservationId);
    Task<IEnumerable<TrackerResponseOption>> GetTrackerResponseOptionsByReservationIdAsync(Guid reservationId);
    Task<TrackerResponse?> GetTrackerResponseByIdAsync(Guid trackerResponseId);
    Task<TrackerResponse> CreateTrackerResponseAsync(TrackerResponse trackerResponse);
    Task<TrackerResponse> UpdateTrackerResponseByIdAsync(TrackerResponse trackerResponse);
    Task DeleteTrackerResponseByIdAsync(Guid trackerResponseId);

    Task<TrackerResponseOption?> GetTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId);
    Task<TrackerResponseOption> CreateTrackerResponseOptionAsync(TrackerResponseOption trackerResponseOption);
    Task<TrackerResponseOption> UpdateTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId, Guid newTrackerDefinitionOptionId);
    Task DeleteTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId);
    #endregion
}
