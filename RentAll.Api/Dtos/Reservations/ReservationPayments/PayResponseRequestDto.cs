namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>What the pay-response page passes on from Number's redirect: the link token, plus ?TXID=..&amp;CID=..</summary>
public class PayResponseRequestDto
{
    public string Token { get; set; } = string.Empty;
    public long TxId { get; set; }
    public long ConsentId { get; set; }

    public PayResponseRequestDto(string? token, long txId, long consentId)
    {
        Token = (token ?? string.Empty).Trim();
        TxId = txId;
        ConsentId = consentId;
    }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        var (tokenValid, tokenError) = PayRequestRequestDto.ValidateToken(Token);
        if (!tokenValid)
            return (false, tokenError);
        if (TxId < 0 || ConsentId < 0)
            return (false, "TXID and CID cannot be negative.");
        if (TxId == 0 && ConsentId == 0)
            return (false, "EasyPay sent no TXID or CID.");
        return (true, null);
    }
}
