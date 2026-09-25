namespace RentAll.Infrastructure.Entities.Accounting;

public sealed class DepositSplitArRematchMatchEntity
{
    public int DepositSplitId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? ArJournalEntryLineId { get; set; }
    public bool IsConsolidatedPaymentTotal { get; set; }
    public Guid? CurrentLineId { get; set; }
    public Guid? PaymentDepositId { get; set; }
    public string? PaymentCode { get; set; }
    public string MatchOutcome { get; set; } = string.Empty;
}

public sealed class DepositSplitArRematchNoMatchEntity
{
    public int DepositSplitId { get; set; }
    public string? SplitDescription { get; set; }
    public string MatchOutcome { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public string? PaymentCode { get; set; }
    public string? InvoiceCode { get; set; }
}
