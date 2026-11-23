namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>A public pay-request call: the emailed link's token is the only parameter.</summary>
public class PayRequestRequestDto
{
    public string Token { get; set; } = string.Empty;

    public PayRequestRequestDto(string? token)
    {
        Token = (token ?? string.Empty).Trim();
    }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        return ValidateToken(Token);
    }

    /// <summary>Tokens are 32 URL-safe base64 characters (A-Z, a-z, 0-9, - and _), at most 64 (the column size).</summary>
    public static (bool IsValid, string? ErrorMessage) ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64 || !token.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_'))
            return (false, "This payment link is not valid.");
        return (true, null);
    }
}
