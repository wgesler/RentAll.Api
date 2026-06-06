namespace RentAll.Domain.Models.Common;

public class EmailAddress
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }

    public static string? NormalizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    public static EmailAddress Create(string email, string? name)
    {
        return new EmailAddress
        {
            Email = email,
            Name = NormalizeName(name)
        };
    }

    public EmailAddress WithNormalizedName()
    {
        return new EmailAddress
        {
            Email = Email,
            Name = NormalizeName(Name)
        };
    }
}
