using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Users;

public class UserActivityResponseDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLoggedIn { get; set; }
    public DateTimeOffset? LastLoginOn { get; set; }
    public DateTimeOffset? LastSeenOn { get; set; }
    public DateTimeOffset? LastLogoutOn { get; set; }

    public UserActivityResponseDto(User user)
    {
        UserId = user.UserId;
        FullName = $"{user.FirstName} {user.LastName}".Trim();
        Email = user.Email;
        IsActive = user.IsActive;
        IsLoggedIn = user.IsLoggedIn;
        LastLoginOn = user.LastLoginOn;
        LastSeenOn = user.LastSeenOn;
        LastLogoutOn = user.LastLogoutOn;
    }
}
