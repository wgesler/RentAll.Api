namespace RentAll.Domain.Models;

public class EscrowReportBundleData
{
    public List<EscrowPropertyReportData> Properties { get; set; } = [];
    public List<EscrowPrepaidPropertyBalance> PrepaidPropertyBalances { get; set; } = [];
    public List<EscrowNotCollectedPropertyBalance> NotCollectedPropertyBalances { get; set; } = [];
    public List<EscrowOfficeBalance> EscrowOfficeBalances { get; set; } = [];
    public List<OwnerStatementJournalEntryLine> OwnerApLines { get; set; } = [];
    public List<OwnerStatementJournalEntryLine> PrepaidApplyLines { get; set; } = [];
    public List<OwnerStatementJournalEntryLine> EscrowBankLines { get; set; } = [];
}
