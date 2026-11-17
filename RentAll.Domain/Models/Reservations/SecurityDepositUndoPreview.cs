namespace RentAll.Domain.Models;

public class SecurityDepositUndoPreview
{
    public string ReservationCode { get; set; } = string.Empty;
    public List<SecurityDepositUndoDocument> Transfers { get; set; } = [];
    public List<SecurityDepositUndoDocument> Deposits { get; set; } = [];
    public List<SecurityDepositUndoDocument> Payments { get; set; } = [];
    public List<SecurityDepositUndoDocument> Invoices { get; set; } = [];
}

public class SecurityDepositUndoDocument
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IncludesOtherReservations { get; set; }
}
