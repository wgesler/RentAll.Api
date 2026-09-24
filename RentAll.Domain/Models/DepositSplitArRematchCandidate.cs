namespace RentAll.Domain.Models;

public sealed class DepositSplitArRematchMatch
{
    public int DepositSplitId { get; init; }
    public Guid PaymentId { get; init; }
    public Guid? ArJournalEntryLineId { get; init; }
    public bool IsConsolidatedPaymentTotal { get; init; }
    public Guid? CurrentLineId { get; init; }
    public Guid? PaymentDepositId { get; init; }
    public string? PaymentCode { get; init; }
    public string MatchOutcome { get; init; } = string.Empty;
}

public sealed class DepositSplitArRematchNoMatch
{
    public int DepositSplitId { get; init; }
    public string? SplitDescription { get; init; }
    public string MatchOutcome { get; init; } = string.Empty;
    public string? PaymentCode { get; init; }
    public string? InvoiceCode { get; init; }
}

public sealed class DepositSplitArRematchCandidates
{
    public IReadOnlyList<DepositSplitArRematchMatch> ExactMatches { get; init; } = [];
    public IReadOnlyList<DepositSplitArRematchNoMatch> NoMatches { get; init; } = [];
}
