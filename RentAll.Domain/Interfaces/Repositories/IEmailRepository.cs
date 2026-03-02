using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IEmailRepository
{
    #region Emails
    Task<IEnumerable<Email>> GetEmailsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Email?> GetEmailByIdAsync(Guid emailId, Guid organizationId);
    Task<Email> CreateAsync(Email email);
    Task<Email> UpdateByIdAsync(Email email);
    #endregion

    #region EmailHtml
    Task<EmailHtml?> GetEmailHtmlByOrganizationIdAsync(Guid organizationId);
    Task<EmailHtml> CreateEmailHtmlAsync(EmailHtml emailHtml);
    Task<EmailHtml> UpdateEmailHtmlByOrganizationIdAsync(EmailHtml emailHtml);
    Task DeleteEmailHtmlByOrganizationIdAsync(Guid organizationId);
    #endregion
}
