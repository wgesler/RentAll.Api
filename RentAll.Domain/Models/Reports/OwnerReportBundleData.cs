namespace RentAll.Domain.Models;

public class OwnerReportBundleData
{
    public List<JournalEntryRecapLine> RecapLines { get; set; } = [];
    public List<JournalEntryLineSearchResult> OwnerApLines { get; set; } = [];
    public List<EscrowOfficeBalance> EscrowOfficeBalances { get; set; } = [];
}
