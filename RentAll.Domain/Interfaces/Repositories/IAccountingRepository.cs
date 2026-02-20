using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IAccountingRepository
{
    // CostCode Creates
    Task<CostCode> CreateAsync(CostCode costCode);

    // CostCode Selects
    Task<List<CostCode>> GetAllAsync(string officeIds, Guid organizationId);
    Task<List<CostCode>> GetAllByOfficeIdAsync(int officeId, Guid organizationId);
    Task<CostCode?> GetByIdAsync(int costCodeId, int officeId, Guid organizationId);
    Task<CostCode?> GetByCostCodeAsync(string costCode, int officeId, Guid organizationId);
    Task<bool> ExistsByCostCodeAsync(string costCode, int officeId, Guid organizationId);

    // CostCode Updates
    Task<CostCode> UpdateByIdAsync(CostCode costCode);

    // CostCode Deletes
    Task DeleteByIdAsync(int costCodeId, int officeId, Guid organizationId);

    // Invoice Creates
    Task<Invoice> CreateAsync(Invoice invoice);

    // Invoice Selects
    Task<IEnumerable<Invoice>> GetAllAsync(Guid organizationId);
    Task<IEnumerable<Invoice>> GetAllByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Invoice>> GetAllByReservationIdAsync(Guid reservationId, Guid organizationId, string officeAccess);
    Task<IEnumerable<Invoice>> GetAllByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<IEnumerable<Invoice>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Invoice?> GetByIdAsync(Guid invoiceId, Guid organizationId);
    Task<IEnumerable<Invoice>> GetByReservationIdAsync(Guid reservationId, Guid organizationId);

    // Invoice Updates
    Task<Invoice> UpdateByIdAsync(Invoice invoice);

    // Invoice Deletes
    Task DeleteByIdAsync(Guid invoiceId, Guid organizationId);
}
