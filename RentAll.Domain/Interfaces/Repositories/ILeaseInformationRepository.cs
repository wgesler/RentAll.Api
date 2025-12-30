using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ILeaseInformationRepository
{
	// Creates
	Task<LeaseInformation> CreateAsync(LeaseInformation leaseInformation);

	// Selects
	Task<LeaseInformation?> GetByIdAsync(Guid leaseInformationId, Guid organizationId);
	Task<LeaseInformation?> GetByPropertyIdAsync(Guid propertyId, Guid organizationId);

	// Updates
	Task<LeaseInformation> UpdateByIdAsync(LeaseInformation leaseInformation);

	// Deletes
	Task DeleteByIdAsync(Guid leaseInformationId);
}

