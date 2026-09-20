namespace RentAll.Domain.Models.Common;

public sealed class SidebarAttentionCounts
{
    public long AssignedTicketCount { get; set; }
    public long NewLeadCount { get; set; }
    public long PendingReceiptDraftCount { get; set; }
}
