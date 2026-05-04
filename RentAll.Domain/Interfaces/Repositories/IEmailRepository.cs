using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IEmailRepository
{
    #region Emails
    Task<IEnumerable<Email>> GetEmailsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Email?> GetEmailByIdAsync(Guid emailId, Guid organizationId);
    Task<Email> CreateEmailAsync(Email email);
    Task<Email> UpdateEmailByIdAsync(Email email);
    Task DeleteEmailByIdAsync(Guid emailId, Guid organizationId);
    #endregion

    #region EmailHtml
    Task<EmailHtml?> GetEmailHtmlByOrganizationIdAsync(Guid organizationId);
    Task<EmailHtml> CreateEmailHtmlAsync(EmailHtml emailHtml);
    Task<EmailHtml> UpdateEmailHtmlByOrganizationIdAsync(EmailHtml emailHtml);
    Task DeleteEmailHtmlByOrganizationIdAsync(Guid organizationId);
    #endregion

    #region Alerts
    Task<IEnumerable<Alert>> GetAlertsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Alert>> GetActiveAlertsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Alert?> GetAlertByIdAsync(Guid alertId, Guid organizationId);
    Task<IEnumerable<Alert>> GetAlertsByTicketIdAsync(Guid ticketId, Guid organizationId);
    Task<Alert> CreateAlertAsync(Alert alert);
    Task<Alert> UpdateAlertByIdAsync(Alert alert);
    Task<Alert> UpdateAlertEmailStatusAsync(Alert alert);
    Task DeleteAlertByIdAsync(Guid alertId, Guid organizationId);
    #endregion
}
