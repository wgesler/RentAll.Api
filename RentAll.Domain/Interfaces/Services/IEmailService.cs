using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(Guid? organizationId, EmailMessage message, CancellationToken cancellationToken = default);
}
