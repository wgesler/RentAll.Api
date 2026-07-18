namespace RentAll.Api.Dtos.Reservations.Reservations;

public class SecurityDepositDetailLineResponseDto
{
    public Guid? InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid? LedgerLineId { get; set; }
    public DateOnly? LineDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;

    public SecurityDepositDetailLineResponseDto(SecurityDepositDetailLine line)
    {
        InvoiceId = line.InvoiceId;
        InvoiceCode = line.InvoiceCode;
        LedgerLineId = line.LedgerLineId;
        LineDate = line.LineDate;
        Description = line.Description;
        Amount = line.Amount;
        JournalEntryId = line.JournalEntryId;
        JournalEntryCode = line.JournalEntryCode;
    }
}

public class SecurityDepositDetailReturnLineResponseDto
{
    public Guid JournalEntryId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public string Memo { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public SecurityDepositDetailReturnLineResponseDto(SecurityDepositDetailReturnLine line)
    {
        JournalEntryId = line.JournalEntryId;
        JournalEntryCode = line.JournalEntryCode;
        TransactionDate = line.TransactionDate;
        Memo = line.Memo;
        Amount = line.Amount;
    }
}

public class SecurityDepositDetailResponseDto
{
    public ReservationDepartureResponseDto Reservation { get; set; } = new(new ReservationDeparture());
    public decimal DepositAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal OwedAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal ReturnedAmount { get; set; }
    public decimal RemainingReturnAmount { get; set; }
    public List<SecurityDepositDetailLineResponseDto> SecurityDepositCharges { get; set; } = [];
    public List<SecurityDepositDetailLineResponseDto> OutstandingCharges { get; set; } = [];
    public List<SecurityDepositDetailLineResponseDto> SecurityDepositPayments { get; set; } = [];
    public List<SecurityDepositDetailReturnLineResponseDto> ReturnPayments { get; set; } = [];

    public SecurityDepositDetailResponseDto(SecurityDepositDetailResult result)
    {
        Reservation = new ReservationDepartureResponseDto(result.Reservation);
        DepositAmount = result.DepositAmount;
        CollectedAmount = result.CollectedAmount;
        OwedAmount = result.OwedAmount;
        BalanceAmount = result.BalanceAmount;
        ReturnedAmount = result.ReturnedAmount;
        RemainingReturnAmount = result.RemainingReturnAmount;
        SecurityDepositCharges = result.SecurityDepositCharges.Select(line => new SecurityDepositDetailLineResponseDto(line)).ToList();
        OutstandingCharges = result.OutstandingCharges.Select(line => new SecurityDepositDetailLineResponseDto(line)).ToList();
        SecurityDepositPayments = result.SecurityDepositPayments.Select(line => new SecurityDepositDetailLineResponseDto(line)).ToList();
        ReturnPayments = result.ReturnPayments.Select(line => new SecurityDepositDetailReturnLineResponseDto(line)).ToList();
    }
}
