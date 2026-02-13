using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IEmailRepository
{
	Task<Email> CreateAsync(Email email);
	Task<IEnumerable<Email>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess);
	Task<Email?> GetByIdAsync(Guid emailId, Guid organizationId);
	Task<Email> UpdateByIdAsync(Email email);
}
