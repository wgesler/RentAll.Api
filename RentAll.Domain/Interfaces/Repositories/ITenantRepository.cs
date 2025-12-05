using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(int tenantId);
    Task<IEnumerable<Tenant>> GetByCompanyIdAsync(int companyId);
    Task<Tenant> CreateAsync(Tenant tenant);
    Task<Tenant> UpdateAsync(Tenant tenant);
    Task<bool> DeleteAsync(int tenantId);
    Task<bool> ExistsAsync(int tenantId);
}


