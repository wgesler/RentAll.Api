using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Auth;

public interface IAuthTokenService
{
    string GenerateToken(User user);
}


