using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.ExtraFeeLines;

public class UpdateExtraFeeLineDto
{
    public int ExtraFeeLineId { get; set; }
    public Guid ReservationId { get; set; }
    public string FeeDescription { get; set; } = string.Empty;
    public decimal FeeAmount { get; set; }
    public int FeeFrequencyId { get; set; }
    public int CostCodeId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ExtraFeeLineId <= 0)
            return (false, "ExtraFeeLineId is required");

        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");

        if (string.IsNullOrWhiteSpace(FeeDescription))
            return (false, "FeeDescription is required");

        if (FeeAmount < 0)
            return (false, "FeeAmount must be zero or greater");

        if (!Enum.IsDefined(typeof(FrequencyType), FeeFrequencyId))
            return (false, $"Invalid FeeFrequencyId value: {FeeFrequencyId}");

        if (CostCodeId <= 0)
            return (false, "CostCodeId is required");

        return (true, null);
    }

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
