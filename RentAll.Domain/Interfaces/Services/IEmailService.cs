using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string? sendGridName, EmailMessage message, CancellationToken cancellationToken = default);
}
