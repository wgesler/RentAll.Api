using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Users;

public partial class UserRepository
{
    public async Task UpdateAuthActivityByIdAsync(Guid userId, DateTimeOffset? lastLoginOn = null, DateTimeOffset? lastSeenOn = null, DateTimeOffset? lastLogoutOn = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("User.User_UpdateAuthActivityById", new
        {
            UserId = userId,
            LastLoginOn = lastLoginOn,
            LastSeenOn = lastSeenOn,
            LastLogoutOn = lastLogoutOn
        });
    }
}
