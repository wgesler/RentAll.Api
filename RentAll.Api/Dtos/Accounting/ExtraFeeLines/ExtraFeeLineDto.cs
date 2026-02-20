using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.ExtraFeeLines;

public class ExtraFeeLineDto
{
    public int ExtraFeeLineId { get; set; }
    public Guid? ReservationId { get; set; }
    public string FeeDescription { get; set; } = string.Empty;
    public decimal FeeAmount { get; set; }
    public int FeeFrequencyId { get; set; }
    public int CostCodeId { get; set; }

    public ExtraFeeLine ToModel()
    {
        return new ExtraFeeLine
        {
            ExtraFeeLineId = ExtraFeeLineId,
            ReservationId = ReservationId,
            FeeDescription = FeeDescription,
            FeeAmount = FeeAmount,
            FeeFrequency = (FrequencyType)FeeFrequencyId,
            CostCodeId = CostCodeId
        };
    }
}
