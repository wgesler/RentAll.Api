namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class CreatePropertyAgreementLineDto
{
    public string Title { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? Deposit { get; set; }
    public decimal? OneTime { get; set; }
    public decimal? Monthly { get; set; }
    public decimal? Daily { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return (false, "Title is required");

        if (!StartDate.HasValue)
            return (false, "StartDate is required");

        if (EndDate.HasValue && EndDate.Value < StartDate.Value)
            return (false, "EndDate must be on or after StartDate");

        if (!Deposit.HasValue)
            return (false, "Deposit is required");

        if (!OneTime.HasValue)
            return (false, "OneTime is required");

        if (!Monthly.HasValue)
            return (false, "Monthly is required");

        if (!Daily.HasValue)
            return (false, "Daily is required");

        if (Deposit.Value < 0 || OneTime.Value < 0 || Monthly.Value < 0 || Daily.Value < 0)
            return (false, "Deposit, OneTime, Monthly, and Daily must be zero or greater");

        return (true, null);
    }

    public AgreementLine ToModel(Guid agreementId)
    {
        return new AgreementLine
        {
            AgreementId = agreementId,
            Title = Title.Trim(),
            StartDate = StartDate!.Value,
            EndDate = EndDate,
            Deposit = Deposit!.Value,
            OneTime = OneTime!.Value,
            Monthly = Monthly!.Value,
            Daily = Daily!.Value
        };
    }
}
