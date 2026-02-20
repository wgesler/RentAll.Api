using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IEmailManager
{
    Task<Email> SendEmail(Email email);
}
