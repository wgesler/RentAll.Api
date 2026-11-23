using RentAll.Domain.Models.Accounting;

namespace RentAll.Api.Dtos.Reservations.Reservations;

/// <summary>
/// Everything Number returned for a saved-card charge, plus the payment recorded for it, so staff can see exactly
/// what happened (shown in the charge result dialog).
/// </summary>
public class ChargeCardOnFileResponseDto
{
    /// <summary>Approved, PartiallyApproved, Declined or Error (Number couldn't process the request).</summary>
    public string Status { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public long TransactionId { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public bool IsPartialApproval { get; set; }
    public Guid? PaymentId { get; set; }
    public string? PaymentCode { get; set; }
    /// <summary>The card charged, e.g. "Visa ending 0939".</summary>
    public string? CardLabel { get; set; }

    /// <summary>Number's ErrCode (0 when none).</summary>
    public int NumberCode { get; set; }
    /// <summary>Number's error text, without the "ERROR:6126:HIGH:" prefix.</summary>
    public string? NumberError { get; set; }
    /// <summary>Number's response text (RespMsg), e.g. "APPROVED OK1400".</summary>
    public string? NumberResponse { get; set; }
    public string? TxnCode { get; set; }
    public string? AvsResult { get; set; }
    public string? CvvResult { get; set; }
    /// <summary>What the code means in plain words, when known.</summary>
    public string? CodeMeaning { get; set; }
    /// <summary>What staff should do next, when known.</summary>
    public string? SuggestedAction { get; set; }
    /// <summary>A RentAll note, e.g. that the payment couldn't be recorded and must be entered by hand.</summary>
    public string? Message { get; set; }

    /// <param name="result">Number's answer to the charge.</param>
    /// <param name="requestedAmount">What staff asked to charge.</param>
    /// <param name="approvedAmount">What Number approved (0 when declined or unclear).</param>
    /// <param name="cardLabel">The card charged, e.g. "Visa ending 0939".</param>
    /// <param name="payment">The payment recorded for it, if one was.</param>
    /// <param name="message">A RentAll note to show with Number's response.</param>
    public ChargeCardOnFileResponseDto(NumberStoredCardChargeResult result, decimal requestedAmount, decimal approvedAmount, string? cardLabel, Payment? payment = null, string? message = null)
    {
        Approved = result.Approved;
        TransactionId = result.TxId;
        RequestedAmount = requestedAmount;
        ApprovedAmount = approvedAmount;
        IsPartialApproval = result.Approved && (result.IsPartialApproval || (approvedAmount > 0 && approvedAmount < requestedAmount));
        Status = !result.FunctionOk && !result.Approved ? "Error"
            : !result.Approved ? "Declined"
            : IsPartialApproval ? "PartiallyApproved"
            : "Approved";
        PaymentId = payment?.PaymentId;
        PaymentCode = payment?.PaymentCode;
        CardLabel = cardLabel;

        NumberCode = result.ErrCode;
        NumberError = NumberResponseCodes.Readable(result.ErrMsg);
        NumberResponse = NumberResponseCodes.Readable(result.RespMsg);
        TxnCode = result.TxnCode;
        AvsResult = result.AvsResult;
        CvvResult = result.CvvResult;
        var described = NumberResponseCodes.Describe(result.ErrCode);
        if (described != null && (result.ErrCode != 0 || !result.Approved))
        {
            CodeMeaning = described.Value.Meaning;
            SuggestedAction = described.Value.Action;
        }
        Message = message;
    }
}
