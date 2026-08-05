namespace RentAll.Domain.Models;

public class TransferReportLineAllocationResult
{
    public Guid JournalEntryLineId { get; set; }
    public Guid DepositId { get; set; }
    public decimal EscrowAmount { get; set; }
    public decimal OwnerEscrow { get; set; }
    public decimal SecDep { get; set; }
    public decimal Sdw { get; set; }
    public decimal Business { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? ContactId { get; set; }
    public string Description { get; set; } = string.Empty;
}
