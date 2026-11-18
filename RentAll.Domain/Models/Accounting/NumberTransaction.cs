namespace RentAll.Domain.Models.Accounting;

/// <summary>
/// A transaction as Number's Query/Transaction_FullDetail reports it.
/// </summary>
public class NumberTransaction
{
    public long Id { get; set; }
    public string? CreatedOn { get; set; }
    public string? Status { get; set; }
    public string? TxType { get; set; }
    public decimal Amount { get; set; }
    public long ConsentId { get; set; }
    public string? RefId { get; set; }
    public string? RpGuid { get; set; }
    public string? TxnCode { get; set; }
}
