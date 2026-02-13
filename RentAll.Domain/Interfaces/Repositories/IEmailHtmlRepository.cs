using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IEmailHtmlRepository
{
	Task<EmailHtml> CreateAsync(EmailHtml emailHtml);
	Task<EmailHtml?> GetByOrganizationIdAsync(Guid organizationId);
	Task<EmailHtml> UpdateByOrganizationIdAsync(EmailHtml emailHtml);
	Task DeleteByOrganizationIdAsync(Guid organizationId);
}
