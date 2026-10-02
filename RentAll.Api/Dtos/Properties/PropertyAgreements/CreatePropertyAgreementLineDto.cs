namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class CreatePropertyAgreementLineDto
{
    public Guid? PropertyId { get; set; }
    public string? Title { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public int? BankCardId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? Deposit { get; set; }
    public decimal? OneTime { get; set; }
    public decimal? Monthly { get; set; }
    public decimal? Daily { get; set; }
    public int? ChartOfAccountId { get; set; }
    public bool? IsRent { get; set; }
    public string? Notes { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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

    public AgreementLine ToModel(Guid? defaultPropertyId, Guid organizationId)
    {
        return new AgreementLine
        {
            AgreementId = NormalizePropertyId(PropertyId ?? defaultPropertyId),
            OrganizationId = organizationId,
            Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim(),
            VendorId = NormalizeBankCardId(BankCardId).HasValue ? null : VendorId,
            VendorName = NormalizeBankCardId(BankCardId).HasValue ? NormalizeVendorName(VendorName) : null,
            BankCardId = NormalizeBankCardId(BankCardId),
            StartDate = StartDate!.Value,
            EndDate = EndDate,
            Deposit = Deposit!.Value,
            OneTime = OneTime!.Value,
            Monthly = Monthly!.Value,
            Daily = Daily!.Value,
            ChartOfAccountId = ChartOfAccountId,
            IsRent = IsRent ?? false,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };
    }

    private static int? NormalizeBankCardId(int? bankCardId)
    {
        return bankCardId is > 0 ? bankCardId : null;
    }

    private static string? NormalizeVendorName(string? vendorName)
    {
        return string.IsNullOrWhiteSpace(vendorName) ? null : vendorName.Trim();
    }

    private static Guid? NormalizePropertyId(Guid? propertyId)
    {
        if (!propertyId.HasValue || propertyId.Value == Guid.Empty)
            return null;

        return propertyId.Value;
    }
}
