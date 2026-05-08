namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class UpdatePropertyAgreementLineDto
{
    public int? AgreementLineId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? Deposit { get; set; }
    public decimal? OneTime { get; set; }
    public decimal? Monthly { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return (false, "Title is required");

        if (!StartDate.HasValue)
            return (false, "StartDate is required");

        if (!EndDate.HasValue)
            return (false, "EndDate is required");

        if (EndDate.Value < StartDate.Value)
            return (false, "EndDate must be on or after StartDate");

        if (!Deposit.HasValue)
            return (false, "Deposit is required");

        if (!OneTime.HasValue)
            return (false, "OneTime is required");

        if (!Monthly.HasValue)
            return (false, "Monthly is required");

        if (Deposit.Value < 0 || OneTime.Value < 0 || Monthly.Value < 0)
            return (false, "Deposit, OneTime, and Monthly must be zero or greater");

        return (true, null);
    }

    public AgreementLine ToModel(Guid agreementId)
    {
        return new AgreementLine
        {
            AgreementLineId = AgreementLineId ?? 0,
            AgreementId = agreementId,
            Title = Title.Trim(),
            StartDate = StartDate!.Value,
            EndDate = EndDate!.Value,
            Deposit = Deposit!.Value,
            OneTime = OneTime!.Value,
            Monthly = Monthly!.Value
        };
    }
}
