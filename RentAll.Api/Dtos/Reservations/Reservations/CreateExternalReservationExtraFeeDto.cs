namespace RentAll.Api.Dtos.Reservations.Reservations;

public class CreateExternalReservationExtraFeeDto
{
    public string FeeDescription { get; set; } = string.Empty;
    public decimal FeeAmount { get; set; }
    public int FeeFrequencyId { get; set; }
    public int CostCodeId { get; set; }

    public List<string> CollectErrors(string prefix)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(FeeDescription))
            errors.Add($"{prefix}.feeDescription is required.");
        if (!Enum.IsDefined(typeof(FrequencyType), FeeFrequencyId))
            errors.Add($"{prefix}.feeFrequencyId must be 0=NA through 8=Daily. Received {FeeFrequencyId}.");
        if (CostCodeId <= 0)
            errors.Add($"{prefix}.costCodeId is required.");
        return errors;
    }

    public ExtraFeeLine ToModel()
    {
        return new ExtraFeeLine
        {
            FeeDescription = FeeDescription.Trim(),
            FeeAmount = FeeAmount,
            FeeFrequency = (FrequencyType)FeeFrequencyId,
            CostCodeId = CostCodeId
        };
    }
}
