namespace RentAll.Api.Dtos.Auth;

public class RegisterDto
{
    public Guid OrganizationId { get; set; } 
	public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


