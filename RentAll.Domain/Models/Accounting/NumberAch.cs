namespace RentAll.Domain.Models.Accounting;

/// <summary>ACH account types as EasyPay (Number) numbers them.</summary>
public enum NumberAchAccountType
{
    PersonalChecking = 1,
    PersonalSavings = 2,
    BusinessChecking = 3,
    BusinessSavings = 4
}

/// <summary>
/// A bank account to save as an ACH consent with EasyPay's ACH/Combo at $0 (no debit). The full numbers are passed
/// straight through to EasyPay and never stored or logged by RentAll.
/// </summary>
public class NumberAchConsentRequest
{
    public string RoutingNumber { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public NumberAchAccountType AccountType { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? ServiceDescription { get; set; }
    public string? ClientRefId { get; set; }
    public string? RpGuid { get; set; }
}

/// <summary>EasyPay's answer to ACH/Combo.</summary>
public class NumberAchConsentResult
{
    public bool FunctionOk { get; set; }
    public bool TxApproved { get; set; }
    public long ConsentId { get; set; }
    public long TxId { get; set; }
    public int ErrCode { get; set; }
    public string? ErrMsg { get; set; }
    public string? RespMsg { get; set; }

    /// <summary>Saved: EasyPay created the consent.</summary>
    public bool Saved => FunctionOk && ConsentId > 0;
}
