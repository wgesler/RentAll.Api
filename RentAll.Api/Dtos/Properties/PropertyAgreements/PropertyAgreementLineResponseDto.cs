namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class PropertyAgreementLineResponseDto
{
    public int AgreementLineId { get; set; }
    public Guid AgreementId { get; set; }
    public string? Title { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public int TermsId { get; set; }
    public string Terms { get; set; } = "Due on receipt";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Deposit { get; set; }
    public decimal OneTime { get; set; }
    public decimal Monthly { get; set; }
    public decimal Daily { get; set; }
    public int? ChartOfAccountId { get; set; }

    public PropertyAgreementLineResponseDto(AgreementLine model)
    {
        AgreementLineId = model.AgreementLineId;
        AgreementId = model.AgreementId;
        Title = model.Title;
        VendorId = model.VendorId;
        VendorName = model.VendorName;
        TermsId = model.TermsId;
        Terms = string.IsNullOrWhiteSpace(model.Terms) ? "Due on receipt" : model.Terms;
        StartDate = model.StartDate;
        EndDate = model.EndDate;
        Deposit = model.Deposit;
        OneTime = model.OneTime;
        Monthly = model.Monthly;
        Daily = model.Daily;
        ChartOfAccountId = model.ChartOfAccountId;
    }
}
