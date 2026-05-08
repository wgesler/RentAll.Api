namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class PropertyAgreementLineResponseDto
{
    public int AgreementLineId { get; set; }
    public Guid AgreementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Deposit { get; set; }
    public decimal OneTime { get; set; }
    public decimal Monthly { get; set; }

    public PropertyAgreementLineResponseDto(AgreementLine model)
    {
        AgreementLineId = model.AgreementLineId;
        AgreementId = model.AgreementId;
        Title = model.Title;
        StartDate = model.StartDate;
        EndDate = model.EndDate;
        Deposit = model.Deposit;
        OneTime = model.OneTime;
        Monthly = model.Monthly;
    }
}
