using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class UpdatePropertyAgreementDto
{
    public Guid PropertyId { get; set; }
    public int? ManagementFeeTypeId { get; set; }
    public decimal? FlatRateAmount { get; set; }
    public FileDetails? W9FileDetails { get; set; }
    public FileDetails? InsuranceFileDetails { get; set; }
    public DateTimeOffset? InsuranceExpiration { get; set; }
    public string? W9Path { get; set; }
    public string? InsurancePath { get; set; }
    public string? AgreementPath { get; set; }
    public FileDetails? AgreementFileDetails { get; set; }
    public int? Markup { get; set; }
    public int? RevenueSplitOwner { get; set; }
    public int? RevenueSplitOffice { get; set; }
    public decimal? WorkingCapitalBalance { get; set; }
    public decimal? LinenAndTowelFee { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? Notes { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (ManagementFeeTypeId.HasValue && !Enum.IsDefined(typeof(ManagementFeeType), ManagementFeeTypeId.Value))
            return (false, $"Invalid ManagementFeeType value: {ManagementFeeTypeId.Value}");

        if (FlatRateAmount.HasValue && FlatRateAmount.Value < 0)
            return (false, "FlatRateAmount cannot be negative");

        if (Markup.HasValue && Markup.Value < 0)
            return (false, "Markup cannot be negative");

        if (RevenueSplitOwner.HasValue && (RevenueSplitOwner.Value < 0 || RevenueSplitOwner.Value > 100))
            return (false, "RevenueSplitOwner must be between 0 and 100");

        if (RevenueSplitOffice.HasValue && (RevenueSplitOffice.Value < 0 || RevenueSplitOffice.Value > 100))
            return (false, "RevenueSplitOffice must be between 0 and 100");

        if (RevenueSplitOwner.HasValue && RevenueSplitOffice.HasValue
            && RevenueSplitOwner.Value + RevenueSplitOffice.Value != 100)
            return (false, "When both splits are provided, RevenueSplitOwner and RevenueSplitOffice must sum to 100");

        return (true, null);
    }

    public PropertyAgreement ToModel(PropertyAgreement existing)
    {
        return new PropertyAgreement
        {
            PropertyId = PropertyId,
            OfficeId = existing.OfficeId,
            ManagementFeeType = ManagementFeeTypeId.HasValue
                ? (ManagementFeeType)ManagementFeeTypeId.Value
                : existing.ManagementFeeType,
            FlatRateAmount = FlatRateAmount ?? existing.FlatRateAmount,
            W9Path = W9Path,
            InsurancePath = InsurancePath,
            InsuranceExpiration = InsuranceExpiration,
            AgreementPath = AgreementPath,
            Markup = Markup ?? existing.Markup,
            RevenueSplitOwner = RevenueSplitOwner ?? existing.RevenueSplitOwner,
            RevenueSplitOffice = RevenueSplitOffice ?? existing.RevenueSplitOffice,
            WorkingCapitalBalance = WorkingCapitalBalance ?? existing.WorkingCapitalBalance,
            LinenAndTowelFee = LinenAndTowelFee ?? existing.LinenAndTowelFee,
            BankName = BankName,
            RoutingNumber = RoutingNumber,
            AccountNumber = AccountNumber,
            Notes = Notes
        };
    }
}
