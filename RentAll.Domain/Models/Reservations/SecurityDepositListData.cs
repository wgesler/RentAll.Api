namespace RentAll.Domain.Models;

public class SecurityDepositListData
{
    public List<ReservationDeparture> Rows { get; set; } = [];
    public List<Invoice> Invoices { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<JournalEntry> JournalEntries { get; set; } = [];
}
