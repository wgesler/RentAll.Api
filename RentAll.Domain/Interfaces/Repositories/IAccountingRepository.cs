using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IAccountingRepository
{
    #region Invoices
    Task<IEnumerable<Invoice>> GetInvoicesByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Invoice>> GetInvoicesByReservationIdAsync(Guid reservationId, Guid organizationId, string officeAccess);
    Task<IEnumerable<Invoice>> GetInvoicesByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId, Guid organizationId);
    Task<Invoice?> GetInvoiceByCodeAsync(string invoiceCode, Guid organizationId);

    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice> UpdateByIdAsync(Invoice invoice);
    Task DeleteInvoiceByIdAsync(Guid invoiceId, Guid organizationId);
    #endregion

    #region CostCodes
    Task<List<CostCode>> GetCostCodesByOfficeIdsAsync( Guid organizationId, string officeIds);
    Task<List<CostCode>> GetCostCodesByOfficeIdAsync(Guid organizationId, int officeId);
    Task<CostCode?> GetCostCodeByIdAsync(int costCodeId, Guid organizationId, int officeId);
    Task<CostCode?> GetByCostCodeAsync(string costCode, Guid organizationId, int officeId);
    Task<bool> ExistsByCostCodeAsync(string costCode, Guid organizationI, int officeId);

    Task<CostCode> CreateAsync(CostCode costCode);
    Task<CostCode> UpdateByIdAsync(CostCode costCode);
    Task DeleteCostCodeByIdAsync(int costCodeId, Guid organizationId, int officeId);
    #endregion
}
