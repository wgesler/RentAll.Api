namespace RentAll.Api.Dtos.Accounting.Transfers;

public class TransferDepositAllocationItemDto
{
    public Guid DepositId { get; set; }
    public decimal EscrowAmount { get; set; }
    public Guid? JournalEntryLineId { get; set; }
}

public class GetTransferDepositAllocationsDto
{
    public int OfficeId { get; set; }
    public List<TransferDepositAllocationItemDto> Items { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (Items == null || Items.Count == 0)
            return (false, "At least one deposit allocation item is required");

        foreach (var item in Items)
        {
            if (item.DepositId == Guid.Empty)
                return (false, "DepositId is required for each allocation item");
        }

        return (true, null);
    }

    public IReadOnlyList<TransferDepositAllocationRequestItem> ToRequestItems()
        => (Items ?? [])
            .Select(item => new TransferDepositAllocationRequestItem
            {
                DepositId = item.DepositId,
                EscrowAmount = item.EscrowAmount,
                JournalEntryLineId = item.JournalEntryLineId is { } lineId && lineId != Guid.Empty ? lineId : null
            })
            .ToList();
}

public class TransferDepositAllocationResponseDto
{
    public TransferDepositAllocationResponseDto()
    {
    }

    public TransferDepositAllocationResponseDto(TransferDepositAllocationResult result)
    {
        DepositId = result.DepositId;
        JournalEntryLineId = result.JournalEntryLineId;
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

    public Guid DepositId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
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
