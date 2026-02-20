namespace RentAll.Infrastructure.Entities.Reservations;

public class ExtraFeeLineEntity
{
    public int ExtraFeeLineId { get; set; }
    public Guid? ReservationId { get; set; }
    public string FeeDescription { get; set; } = string.Empty;
    public decimal FeeAmount { get; set; }
    public int FeeFrequencyId { get; set; }
    public int CostCodeId { get; set; }
}
