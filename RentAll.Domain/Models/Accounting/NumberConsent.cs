namespace RentAll.Domain.Models.Accounting;

/// <summary>
/// A saved card (annual consent) as Number's Query/ConsentAnnual_FullDetail reports it.
/// </summary>
public class NumberConsent
{
    public long Id { get; set; }
    public int MerchId { get; set; }
    public bool IsEnabled { get; set; }
    public string? RpGuid { get; set; }
    public string? CustomerRefId { get; set; }
    public string? LastFour { get; set; }
    /// <summary>Number's code, e.g. VI, MC, DS, AX.</summary>
    public string? CardType { get; set; }
    /// <summary>MMYY as Number returns it.</summary>
    public string? ExpDate { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>A charge against a saved card, sent to ConsentAnnual/ChargeStoredCard.</summary>
public class NumberStoredCardCharge
{
    public long ConsentId { get; set; }
    public decimal Amount { get; set; }
    public string? ServiceDescription { get; set; }
    public string? ClientRefId { get; set; }
    public string? RpGuid { get; set; }
    public string? User { get; set; }
}

/// <summary>Number's answer to a stored-card charge.</summary>
public class NumberStoredCardChargeResult
{
    public bool Approved { get; set; }
    /// <summary>The call itself worked (false: EasyPay could not process the request at all).</summary>
    public bool FunctionOk { get; set; }
    public long TxId { get; set; }
    public string? TxnCode { get; set; }
    public string? Message { get; set; }
    /// <summary>EasyPay's error text (ErrMsg), when there is one.</summary>
    public string? ErrMsg { get; set; }
    /// <summary>EasyPay's response text (RespMsg); their docs say to show this to the user.</summary>
    public string? RespMsg { get; set; }
    public int ErrCode { get; set; }
    public bool IsPartialApproval { get; set; }
    /// <summary>ApprovedAmounts.TotalAmt; Number says this can differ from what was requested.</summary>
    public decimal ApprovedTotal { get; set; }
    public decimal AuthorizedAmount { get; set; }
    public string? AvsResult { get; set; }
    public string? CvvResult { get; set; }
}
