using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IInvoiceRepository
{
	// Creates
	Task<Invoice> CreateAsync(Invoice invoice);

	// Selects
	Task<IEnumerable<Invoice>> GetAllAsync(Guid organizationId);
	Task<IEnumerable<Invoice>> GetAllByOfficeIdsAsync(Guid organizationId, string officeAccess);
	Task<IEnumerable<Invoice>> GetAllByReservationIdAsync(Guid reservationId, Guid organizationId, string officeAccess);
	Task<IEnumerable<Invoice>> GetAllByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
	Task<IEnumerable<Invoice>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess);

	Task<Invoice?> GetByIdAsync(Guid invoiceId, Guid organizationId);
	Task<IEnumerable<Invoice>> GetByReservationIdAsync(Guid reservationId, Guid organizationId);

	// Updates
	Task<Invoice> UpdateByIdAsync(Invoice invoice);

	// Deletes
	Task DeleteByIdAsync(Guid invoiceId, Guid organizationId);
}
