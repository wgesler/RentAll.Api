using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IEmailRepository
{
    // Email Creates
    Task<Email> CreateAsync(Email email);

    // Email Selects
    Task<IEnumerable<Email>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<Email?> GetByIdAsync(Guid emailId, Guid organizationId);

    // Email Updates
    Task<Email> UpdateByIdAsync(Email email);

    // EmailHtml Creates
    Task<EmailHtml> CreateEmailHtmlAsync(EmailHtml emailHtml);

    // EmailHtml Selects
    Task<EmailHtml?> GetEmailHtmlByOrganizationIdAsync(Guid organizationId);

    // EmailHtml Updates
    Task<EmailHtml> UpdateEmailHtmlByOrganizationIdAsync(EmailHtml emailHtml);

    // EmailHtml Deletes
    Task DeleteEmailHtmlByOrganizationIdAsync(Guid organizationId);
}
