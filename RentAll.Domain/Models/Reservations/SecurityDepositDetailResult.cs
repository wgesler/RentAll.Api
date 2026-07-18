namespace RentAll.Domain.Models;

public class SecurityDepositDetailResult
{
    public ReservationDeparture Reservation { get; set; } = new();
    public decimal DepositAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal OwedAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal ReturnedAmount { get; set; }
    public decimal RemainingReturnAmount { get; set; }
    public List<SecurityDepositDetailLine> SecurityDepositCharges { get; set; } = [];
    public List<SecurityDepositDetailLine> OutstandingCharges { get; set; } = [];
    public List<SecurityDepositDetailLine> SecurityDepositPayments { get; set; } = [];
    public List<SecurityDepositDetailReturnLine> ReturnPayments { get; set; } = [];
}

public class SecurityDepositDetailLine
{
    public Guid? InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid? LedgerLineId { get; set; }
    public DateOnly? LineDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
}

public class SecurityDepositDetailReturnLine
{
    public Guid JournalEntryId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public string Memo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
