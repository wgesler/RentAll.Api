namespace RentAll.Api.Dtos.Auth;

public class JwtUserResponseDto
{
    public Guid UserGuid { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string> UserGroups { get; set; } = new List<string>();
    public int StartupPage { get; set; }

    public JwtUserResponseDto(User user)
    {
        UserGuid = user.UserId;
        FirstName = user.FirstName;
        LastName = user.LastName;
        Email = user.Email;
        Phone = user.Phone;
        UserGroups = user.UserGroups;
        StartupPage = (int)user.StartupPage;
    }
}
