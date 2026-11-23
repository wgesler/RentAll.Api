using RentAll.Domain.Models.Accounting;

namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>What the public pay-response page shows after EasyPay returns: whether the card was saved, and which card.</summary>
public class PayResponseResponseDto
{
    public bool Saved { get; set; }
    public long ConsentId { get; set; }
    public string? ReservationCode { get; set; }
    public string? CardType { get; set; }
    public string? LastFour { get; set; }
    public string? ExpirationDate { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Website { get; set; }

    public PayResponseResponseDto(bool saved, long consentId, string? reservationCode, NumberConsent? consent, string? website)
    {
        Saved = saved;
        ConsentId = consentId;
        ReservationCode = reservationCode;
        CardType = CardTypeLabel(consent?.CardType);
        LastFour = consent?.LastFour;
        ExpirationDate = consent?.ExpDate;
        Message = saved ? "Your card was saved." : "Your card could not be confirmed. Please contact us.";
        Website = website;
    }

    /// <summary>EasyPay's card code (VI, MC, DS, AX) as a name the payer recognizes.</summary>
    private static string? CardTypeLabel(string? numberCode)
    {
        var value = new string((numberCode ?? string.Empty).Where(char.IsLetter).ToArray()).ToUpperInvariant();
        return value switch
        {
            "" => null,
            "VI" or "VISA" => "Visa",
            "MC" or "MASTERCARD" => "Mastercard",
            "DS" or "DI" or "DISC" or "DISCOVER" => "Discover",
            "AX" or "AMEX" or "AMERICANEXPRESS" => "American Express",
            _ => numberCode
        };
    }
}
