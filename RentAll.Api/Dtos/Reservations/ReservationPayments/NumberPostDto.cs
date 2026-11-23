using System.Text.Json.Serialization;

namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>
/// What Number posts to our PostURL webhook. Two shapes arrive: the PayForm post (flat fields) and the
/// PFS webhook (type, nested transaction/subscription); ApplyWebhookShape folds the second into the first.
/// </summary>
public class NumberPostDto
{
    public long ConsentID { get; set; }
    public long TransactionID { get; set; }
    public string? CardNumber { get; set; }
    public string? CardType { get; set; }
    public string? ExpireDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Surcharge { get; set; }
    public string? CardholderFirstName { get; set; }
    public string? CardholderLastName { get; set; }
    public string? CustomerFirstName { get; set; }
    public string? CustomerLastName { get; set; }
    public string? Email { get; set; }
    public string? REFID { get; set; }
    public string? RPGUID { get; set; }
    public string? ApprovalCode { get; set; }

    // PFS webhook shape: { type, refid, rpguid, subscrip_id, transaction{id, amount (cents), result, txncode}, subscription{id, ...} }.
    // REFID/RPGUID above already match refid/rpguid (names are case-insensitive).
    public string? Type { get; set; }
    [JsonPropertyName("subscrip_id")]
    public long SubscripId { get; set; }
    public NumberWebhookTransaction? Transaction { get; set; }
    public NumberWebhookSubscription? Subscription { get; set; }

    /// <summary>Copies the webhook's nested values onto the flat PayForm fields the rest of the flow reads.</summary>
    public void ApplyWebhookShape()
    {
        if (TransactionID <= 0 && Transaction?.Id > 0)
        {
            TransactionID = Transaction.Id;
            if (Amount == 0 && Transaction.Amount > 0)
                Amount = Transaction.Amount / 100m;
            ApprovalCode ??= Transaction.TxnCode;
        }
        if (ConsentID <= 0 && SubscripId > 0)
            ConsentID = SubscripId;
        if (ConsentID <= 0 && Subscription?.Id > 0)
            ConsentID = Subscription.Id;
    }

    /// <summary>RPGUID as a reservation id (we send the reservation id as RPGUID on InitForm).</summary>
    public Guid ReservationId => Guid.TryParse(RPGUID, out var reservationId) ? reservationId : Guid.Empty;

    /// <summary>Has a transaction or saved card to act on.</summary>
    public bool HasIds => TransactionID > 0 || ConsentID > 0;

    /// <summary>Call after ApplyWebhookShape.</summary>
    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TransactionID < 0 || ConsentID < 0)
            return (false, "TransactionID and ConsentID cannot be negative");
        if (!HasIds)
            return (false, "Unreadable Number payment post");
        if (ReservationId == Guid.Empty)
            return (false, "RPGUID is not a reservation id");
        return (true, null);
    }

    /// <summary>Reads PFS's pipe-delimited result: KEY|value|KEY|value...</summary>
    public static NumberPostDto? FromPipePost(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.Contains('|'))
            return null;

        var parts = raw.Trim().Split('|');
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i + 1 < parts.Length; i += 2)
            values[parts[i].Trim()] = parts[i + 1].Trim();

        string? Get(string key) => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
        long.TryParse(Get("TXID"), out var txId);
        long.TryParse(Get("CONSENTID"), out var consentId);
        decimal.TryParse(Get("AMOUNT"), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var amount);

        return new NumberPostDto
        {
            TransactionID = txId,
            ConsentID = consentId,
            CardNumber = Get("CARDNO"),
            CardType = Get("CARDTYPE"),
            ExpireDate = Get("EXPDATE"),
            Amount = amount,
            Email = Get("Email"),
            REFID = Get("REFID"),
            RPGUID = Get("RPGUID")
        };
    }
}
