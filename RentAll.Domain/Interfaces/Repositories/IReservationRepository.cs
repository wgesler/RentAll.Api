using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
    // Reservation Creates
    Task<Reservation> CreateAsync(Reservation reservation);

    // Reservation Selects
    Task<IEnumerable<ReservationList>> GetListByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Reservation?> GetByIdAsync(Guid reservationId, Guid organizationId);
    Task<IEnumerable<Reservation>> GetByPropertyIdAsync(Guid propertyId, Guid organizationId);

    // Reservation Updates
    Task<Reservation> UpdateByIdAsync(Reservation reservation);

    // Reservation Deletes
    Task DeleteByIdAsync(Guid reservationId);

    // LeaseInformation Creates
    Task<LeaseInformation> CreateLeaseInformationAsync(LeaseInformation leaseInformation);

    // LeaseInformation Selects
    Task<LeaseInformation?> GetLeaseInformationByIdAsync(Guid leaseInformationId, Guid organizationId);
    Task<LeaseInformation?> GetLeaseInformationByPropertyIdAsync(Guid propertyId, Guid organizationId);

    // LeaseInformation Updates
    Task<LeaseInformation> UpdateLeaseInformationByIdAsync(LeaseInformation leaseInformation);

    // LeaseInformation Deletes
    Task DeleteLeaseInformationByIdAsync(Guid leaseInformationId);
}


