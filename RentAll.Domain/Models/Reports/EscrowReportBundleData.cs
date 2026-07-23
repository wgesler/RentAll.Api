namespace RentAll.Domain.Models;

public class EscrowReportBundleData
{
    public List<JournalEntryRecapLine> RecapLines { get; set; } = [];
    public List<EscrowOfficeBalance> EscrowOfficeBalances { get; set; } = [];
    public List<EscrowPrepaidPropertyBalance> EscrowPrepaidPropertyBalances { get; set; } = [];
}
