using RentAll.Domain.Models.Users;

namespace RentAll.Domain.Interfaces.Auth;

public interface IAuthTokenService
{
    string GenerateToken(User user);
}


