namespace RentAll.Domain.Models;

internal sealed class InvoiceOwnerIncomeTotals
{
    public decimal OwnerRentValue { get; set; }
    public decimal OwnerRentActualValue { get; set; }
    public decimal ExpectedIncomeValue { get; set; }
    public string OwnerRentMemo { get; set; } = string.Empty;
    public string OwnerRentJournalEntryCode { get; set; } = string.Empty;
    public string OwnerRentAccountingPeriod { get; set; } = string.Empty;
    public Guid? OwnerRentJournalEntryLineId { get; set; }
    public Guid? OwnerRentSourceId { get; set; }
    public int? OwnerRentSourceTypeId { get; set; }
}
