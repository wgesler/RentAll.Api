namespace RentAll.Api.Dtos.Accounting.Transfers;

public class TransferReportLineAllocationResponseDto
{
    public TransferReportLineAllocationResponseDto()
    {
    }

    public TransferReportLineAllocationResponseDto(TransferReportLineAllocationResult result)
    {
        JournalEntryLineId = result.JournalEntryLineId;
        DepositId = result.DepositId;
        EscrowAmount = result.EscrowAmount;
        OwnerEscrow = result.OwnerEscrow;
        SecDep = result.SecDep;
        Sdw = result.Sdw;
        Business = result.Business;
        PropertyId = result.PropertyId;
        ReservationId = result.ReservationId;
        ContactId = result.ContactId;
        Description = result.Description;
    }

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
