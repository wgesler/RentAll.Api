using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class ExtraFeeLine
{
    public int ExtraFeeLineId { get; set; }
    public Guid? ReservationId { get; set; }
    public string FeeDescription { get; set; } = string.Empty;
    public decimal FeeAmount { get; set; }
    public FrequencyType FeeFrequency { get; set; }
    public int CostCodeId { get; set; }
}
