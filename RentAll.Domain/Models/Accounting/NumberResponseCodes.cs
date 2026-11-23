namespace RentAll.Domain.Models.Accounting;

/// <summary>
/// Plain-language meanings for Number response codes, so staff understand a decline or error.
/// Card decline codes (ErrCode under 1000) come from Number's processor tables:
/// https://docs.number.tech/documentation/testing/first-data-testing.md (Response codes).
/// API errors (4-5 digits, e.g. 6126) are not in Number's docs; these are the ones we have seen and what they meant.
/// </summary>
public static class NumberResponseCodes
{
    private static readonly Dictionary<int, (string Meaning, string? Action)> Codes = new()
    {
        // Card responses (First Data / Number processor codes).
        [0] = ("Approved.", null),
        [2] = ("Approved for a partial amount.", "Collect the rest another way."),
        [100] = ("Do not honor: the card issuer declined the charge.", "Ask the tenant to contact their bank or use another card."),
        [101] = ("The card has expired.", "Ask the tenant for a new card."),
        [102] = ("Suspected fraud.", "Do not retry. Ask the tenant to contact their bank."),
        [104] = ("Restricted card.", "Ask the tenant to use another card."),
        [107] = ("Call the card issuer for authorization.", "Ask the tenant to contact their bank."),
        [116] = ("Insufficient funds.", "Try again later or ask the tenant for another card."),
        [129] = ("Suspected counterfeit card.", "Do not retry."),
        [134] = ("Insufficient funds.", "Try again later or ask the tenant for another card."),
        [209] = ("The card was reported stolen.", "Do not retry."),
        [500] = ("Declined.", "Ask the tenant to contact their bank or use another card."),
        [504] = ("Declined: do not try again.", "Do not retry this card."),
        [505] = ("Please retry.", "Try the charge again."),
        [508] = ("Try again later (the issuer cancelled recurring charges).", "Ask the tenant to contact their bank."),
        [509] = ("Do not try again: the card has expired.", "Ask the tenant for a new card."),
        [511] = ("Try again later: a new expiration date is needed.", "Ask the tenant to update their card."),
        [514] = ("Do not try again: security violation.", "Do not retry this card."),
        [516] = ("The card network was busy or unavailable.", "Try the charge again."),
        [517] = ("The CVV was declined.", "Ask the tenant to save the card again."),
        [524] = ("The debit network was unavailable or timed out.", "Try the charge again."),
        [785] = ("No reason to decline (card verified).", null),
        [790] = ("Visa: do not resubmit this same charge.", "Do not retry."),
        [791] = ("Visa: the cardholder stopped recurring charges.", "Ask the tenant for another payment method."),
        [902] = ("Invalid transaction for this card.", "Ask the tenant to use another card."),
        [906] = ("Card network system error.", "Try again later."),
        [907] = ("The card issuer is unavailable.", "Try again later."),
        [909] = ("Card network malfunction or timeout.", "Try again later."),
        [911] = ("The card issuer timed out.", "Try again later."),
        [913] = ("Duplicate transaction.", "Check whether the charge already went through before retrying."),
        [963] = ("The payment network is unavailable.", "Try again later."),

        // Number API errors seen in testing.
        [5030] = ("The EasyPay session expired.", "Try again; RentAll signs in again automatically."),
        [5050] = ("The EasyPay session was rejected (the server's address changed).", "Try again; RentAll signs in again automatically."),
        [6046] = ("EasyPay could not find this saved card as an annual consent.", "This card was saved as a subscription consent; see below."),
        [6126] = ("This saved card can't be charged this way: EasyPay only charges annual consents here, and this card was saved as a subscription consent.", "Contact EasyPay: subscription consents need a different charge method."),
        [7123] = ("EasyPay rejected a date in the request.", "Contact support."),
    };

    /// <summary>The meaning and suggested action for an Number ErrCode, or null when we don't know it.</summary>
    public static (string Meaning, string? Action)? Describe(int errCode)
    {
        return Codes.TryGetValue(errCode, out var entry) ? entry : null;
    }

    /// <summary>"ERROR:6126:HIGH:Cannot get consent ..." -> "Cannot get consent ..."; other text unchanged.</summary>
    public static string? Readable(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;
        var text = message.Trim();
        if (text.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = text.Split(':', 4);
            if (parts.Length == 4 && !string.IsNullOrWhiteSpace(parts[3]))
                return parts[3].Trim();
        }
        return text;
    }
}
